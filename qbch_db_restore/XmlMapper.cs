using db_lib.Entities;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace qbch_db_restore;

/// <summary>
/// По какому полю ищется существующий пользователь в td_users.
/// Повторяет логику поиска из RepositoryV3.GetOrCreateUserV3.
/// </summary>
internal enum UserMatch
{
    Ogrn,
    FullName
}

/// <summary>
/// Чтение XML из колонок te_dlrequests напрямую через LINQ to XML, без XmlSerializer.
/// Сделано осознанно: данные потеряны именно потому, что десериализация в ЗапросСведений
/// упала, и на тех же XML она упадёт снова (например, при значении вне сгенерированного enum).
/// Значения справочников в XML и так лежат в виде кодов, поэтому XmlEnumHelper не нужен.
/// </summary>
internal static class XmlMapper
{
    /// <summary>
    /// Все варианты секций блока КБКИ (выбор xsd:choice в ОтветНаЗапросСведенийСведенияКБКИ).
    /// </summary>
    private static readonly string[] KbkiSections =
    [
        "СубъектНеНайден",
        "ОбязательствНет",
        "Обязательства",
        "Ошибка",
        "УсловияЗапрета",
        "СведенийОЗапретеНет",
        "СведенияОЗапретеНеПредоставляются",
        "СведенияДляПредупреждения",
        "СведенийДляПредупрежденияНет",
        "СведенияДляПредупрежденияНеПредоставляются"
    ];

    public static XElement? Parse(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            // BOM в начале строки роняет XElement.Parse («Data at the root level is invalid»),
            // а именно на кодировках и спотыкалась исходная десериализация.
            return XElement.Parse(xml.TrimStart('﻿', '​'));
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>
    /// Атрибут по локальному имени: XML контракта идёт без namespace, но если он там окажется,
    /// восстановление не должно разваливаться — именно такой XML и мог не пройти десериализацию.
    /// </summary>
    public static string? Attr(XElement? element, string name)
        => Normalize((string?)element?.Attributes().FirstOrDefault(x => x.Name.LocalName == name));

    public static XElement? Node(XElement? element, string name)
        => element?.Elements().FirstOrDefault(x => x.Name.LocalName == name);

    public static IEnumerable<XElement> Nodes(XElement? element, string name)
        => element is null ? [] : element.Elements().Where(x => x.Name.LocalName == name);

    public static string? Elem(XElement? element, string name) => Normalize((string?)Node(element, name));

    public static int? AttrInt(XElement? element, string name)
        => int.TryParse(Attr(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>
    /// Разбор xs:date: "2026-08-06", в том числе со смещением — "2026-08-06+03:00", "2026-08-06Z".
    /// Другие формы не разбираются намеренно: разбор по текущей или инвариантной культуре
    /// принял бы "06.08.2026" за 8 июня и записал бы в БД правдоподобную неверную дату.
    /// </summary>
    public static DateOnly? Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        return trimmed.Length >= 10
            && DateOnly.TryParseExact(trimmed[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    /// <summary>
    /// Субъект запроса: повторяет RepositoryV3.AddSubject, включая нормализацию СНИЛС.
    /// Ссылка на te_requests проставляется вызывающей стороной.
    /// </summary>
    public static (TeSubject Subject, List<TeSubjectsDocument> Documents, List<TeSubjectsFullName> FullNames)? BuildSubject(XElement пакет)
    {
        var субъект = Node(пакет, "Субъект");

        if (субъект is null)
            return null;

        var инн = Node(субъект, "ИНН");
        var инНомер = Elem(субъект, "ИнНомер");
        var снилс = Elem(субъект, "СНИЛС");
        var цифрыСнилс = снилс is null ? null : new string(снилс.Where(char.IsDigit).ToArray());

        var subject = new TeSubject
        {
            BirthDay = Date(Elem(субъект, "ДатаРождения")),
            Inn = Normalize((string?)инн) ?? инНомер,
            Snils = цифрыСнилс?.Length == 11 ? цифрыСнилс : снилс,
            // В проде признак берётся из enum со значением по умолчанию "1",
            // поэтому элемент ИНН без атрибута ПризнакПроверки даёт true.
            InnChecked = инн is not null && Attr(инн, "ПризнакПроверки") != "0",
            InnForeign = !string.IsNullOrWhiteSpace(инНомер)
        };

        var documents = Nodes(субъект, "ДокументЛичности").Select(x => new TeSubjectsDocument
        {
            DocTypeId = Attr(x, "КодДУЛ") ?? string.Empty,
            DocDateIssue = Date(Elem(x, "ДатаВыдачи")) ?? default,
            DocSeries = Elem(x, "Серия"),
            DocNumber = Elem(x, "Номер") ?? string.Empty,
            CountryCode = ElemInt(x, "Гражданство"),
            Subject = subject
        }).ToList();

        var fullNames = Nodes(субъект, "ФИО").Select(x => new TeSubjectsFullName
        {
            FirstName = Elem(x, "Имя"),
            LastName = Elem(x, "Фамилия"),
            MiddleName = Elem(x, "Отчество"),
            Subject = subject
        }).ToList();

        return (subject, documents, fullNames);
    }

    /// <summary>
    /// Источник (пользователь) запроса: повторяет RepositoryV3.GetOrCreateUserV3.
    /// Возвращает поле и значение для поиска существующей записи плюс заготовку новой.
    /// </summary>
    public static (UserMatch Match, string? Value, TdUser User)? BuildUser(XElement пакет)
    {
        var источник = Node(пакет, "Источник")?.Elements().FirstOrDefault();

        if (источник is null)
            return null;

        switch (источник.Name.LocalName)
        {
            case "ЮридическоеЛицо":
                {
                    var ogrn = Elem(источник, "ОГРН");

                    return (UserMatch.Ogrn, ogrn, new TdUser
                    {
                        FullName = Elem(источник, "ПолноеНаименование"),
                        ShortName = Elem(источник, "СокращенноеНаименование"),
                        OtherName = Elem(источник, "ИноеНаименование"),
                        Inn = Elem(источник, "ИНН"),
                        Ogrn = ogrn,
                        UserType = 1,
                        IsForeign = false
                    });
                }

            case "ИндивидуальныйПредприниматель":
                {
                    var ogrn = Elem(источник, "ОГРНИП");
                    var фио = Node(источник, "ФИО");
                    var дул = Node(источник, "ДокументЛичности");

                    return (UserMatch.Ogrn, ogrn, new TdUser
                    {
                        FirstName = Elem(фио, "Имя"),
                        LastName = Elem(фио, "Фамилия"),
                        MiddleName = Elem(фио, "Отчество"),
                        BirthDate = Date(Elem(источник, "ДатаРождения")),
                        Inn = Elem(источник, "ИННИП"),
                        Ogrn = ogrn,
                        Snils = Elem(источник, "СНИЛС"),
                        DocSeria = Elem(дул, "Серия"),
                        DocNumber = Elem(дул, "Номер"),
                        DocIssueDate = Date(Elem(дул, "ДатаВыдачи")),
                        DocIssuerName = Elem(дул, "НаименованиеОргана"),
                        DocIssuerCode = Elem(дул, "КодПодразделения"),
                        // Отличие от прода: там отсутствующий атрибут КодДУЛ давал значение
                        // enum по умолчанию ("21"), здесь вместо выдуманного кода остаётся NULL.
                        DocType = Attr(дул, "КодДУЛ"),
                        DocOtherName = Attr(дул, "НаименованиеДУЛ"),
                        UserType = 2,
                        IsForeign = false
                    });
                }

            case "ИностранноеЮЛ":
                {
                    var fullName = Elem(источник, "ПолноеНаименование");

                    return (UserMatch.FullName, fullName, new TdUser
                    {
                        FullName = fullName,
                        ShortName = Elem(источник, "СокращенноеНаименование"),
                        OtherName = Elem(источник, "ИноеНаименование"),
                        Inn = Elem(источник, "НомерНП"),
                        Ogrn = Elem(источник, "РегНомер"),
                        UserType = 1,
                        IsForeign = true
                    });
                }

            case "ИностранныйПредприниматель":
                {
                    var фио = Node(источник, "ФИО");
                    var дул = Node(источник, "ДокументЛичности");
                    var fullName = Elem(фио, "Фамилия") + Elem(фио, "Имя") + Elem(дул, "Серия") + Elem(дул, "Номер");

                    return (UserMatch.FullName, fullName, new TdUser
                    {
                        FirstName = Elem(фио, "Имя"),
                        LastName = Elem(фио, "Фамилия"),
                        MiddleName = Elem(фио, "Отчество"),
                        BirthDate = Date(Elem(источник, "ДатаРождения")),
                        Inn = Elem(источник, "НомерНП"),
                        Ogrn = Elem(источник, "РегНомер"),
                        DocSeria = Elem(дул, "Серия"),
                        DocNumber = Elem(дул, "Номер"),
                        DocIssueDate = Date(Elem(дул, "ДатаВыдачи")),
                        DocIssuerName = Elem(дул, "НаименованиеОргана"),
                        DocIssuerCode = Elem(дул, "КодПодразделения"),
                        DocType = Attr(дул, "КодДУЛ"),
                        DocOtherName = Attr(дул, "НаименованиеДУЛ"),
                        FullName = fullName,
                        UserType = 5,
                        IsForeign = true
                    });
                }

            default:
                return null;
        }
    }

    /// <summary>
    /// Повторяет RepositoryV3.MapAmpResponseTypeV3 на элементах XML.
    /// </summary>
    public static int? MapAmpResponseType(XElement кбки, string? ourOgrn)
    {
        if (!HasAnySection(кбки))
            return null;

        if (Node(кбки, "СубъектНеНайден") is not null)
            return 1;

        if (Node(кбки, "ОбязательствНет") is not null)
            return 2;

        var обязательства = Node(кбки, "Обязательства");

        if (обязательства is not null)
        {
            var бки = Nodes(обязательства, "БКИ").ToList();
            var ourData = бки.Any(x => Attr(x, "ОГРН") == ourOgrn);
            var notOurData = бки.Any(x => Attr(x, "ОГРН") != ourOgrn);

            if (ourData && notOurData) return 4;
            if (!ourData && notOurData) return 5;
            if (ourData) return 3;

            return 5;
        }

        var ошибка = Node(кбки, "Ошибка");

        if (ошибка is not null)
            return Attr(ошибка, "Код") == "18" ? 7 : 6;

        return null;
    }

    /// <summary>
    /// Повторяет RepositoryV3.MapSpResponseTypeV3 на элементах XML.
    /// </summary>
    public static int? MapSpResponseType(XElement кбки)
    {
        if (!HasAnySection(кбки))
            return null;

        if (Node(кбки, "СубъектНеНайден") is not null) return 1;
        if (Node(кбки, "СведенияОЗапретеНеПредоставляются") is not null) return 2;
        if (Node(кбки, "УсловияЗапрета") is not null) return 3;
        if (Node(кбки, "СведенийОЗапретеНет") is not null) return 4;

        return null;
    }

    /// <summary>
    /// Ответ одного бюро из общего агрегата: тот же документ, в котором оставлены только блоки КБКИ
    /// этого ОГРН, а в корне проставлены его ОГРН и ИдентификаторОтвета.
    /// Содержимое блоков — настоящий ответ бюро, пересобирается только конверт.
    /// ИдентификаторЗапроса остаётся от агрегата: идентификатор пересланного запроса жил
    /// в te_qbch_dlrequests.request_xml и потерян вместе с Redis.
    /// </summary>
    public static string? BuildTaskResultXml(XElement ответ, string ogrn)
    {
        var документ = new XElement(ответ);

        foreach (var сведения in Nodes(документ, "Сведения").ToList())
        {
            foreach (var кбки in Nodes(сведения, "КБКИ").ToList())
            {
                if (Attr(кбки, "ОГРН") != ogrn)
                    кбки.Remove();
            }

            if (!Nodes(сведения, "КБКИ").Any())
                сведения.Remove();
        }

        if (!Nodes(документ, "Сведения").Any())
            return null;

        документ.SetAttributeValue("ОГРН", ogrn);

        var responseId = Nodes(документ, "Сведения")
            .SelectMany(x => Nodes(x, "КБКИ"))
            .Select(x => Attr(x, "ИдентификаторОтвета"))
            .FirstOrDefault(x => x is not null);

        if (responseId is not null)
            документ.SetAttributeValue("ИдентификаторОтвета", responseId);

        return документ.ToString();
    }

    /// <summary>
    /// Пересланный в бюро ЗапросСведений: конверт (блок Абонент, ТипЗапроса) берётся из образца
    /// реального обмена, пакеты и коды — из восстанавливаемого запроса абонента.
    /// </summary>
    public static string? BuildForwardedRequest(string? template, XElement original, string? requestId)
    {
        var документ = Parse(template);

        if (документ is null)
            return null;

        foreach (var запрос in Nodes(документ, "Запрос").ToList())
            запрос.Remove();

        foreach (var запрос in Nodes(original, "Запрос"))
            документ.Add(new XElement(запрос));

        Restamp(документ, "ИдентификаторЗапроса", requestId);
        Restamp(документ, "ДатаЗапроса", Attr(original, "ДатаЗапроса"));
        Restamp(документ, "КодСведений", Attr(original, "КодСведений"));
        Restamp(документ, "РежимЗапроса", Attr(original, "РежимЗапроса"));

        return документ.ToString();
    }

    /// <summary>
    /// Результат от бюро по образцу реального обмена: в конверте заменяются только идентификаторы
    /// восстанавливаемой записи. Атрибуты, которых в образце нет, не добавляются.
    /// </summary>
    public static string? StampResult(string? template, string ogrn, string? responseId, string? requestId, string? requestDate)
    {
        var документ = Parse(template);

        if (документ is null)
            return null;

        Restamp(документ, "ОГРН", ogrn);

        var идентификатор = Node(документ, "ИдентификаторОтвета");

        if (идентификатор is not null && responseId is not null)
            идентификатор.Value = responseId;

        foreach (var узел in new[] { документ, идентификатор, Node(документ, "Ошибка"), Node(документ, "Успешно") })
        {
            Restamp(узел, "ИдентификаторЗапроса", requestId);
            Restamp(узел, "ДатаЗапроса", requestDate);
        }

        return документ.ToString();
    }

    /// <summary>
    /// Переставляет значение атрибута, только если он в документе уже есть: выдумывать атрибуты,
    /// которых в реальном обмене не было, нельзя.
    /// </summary>
    private static void Restamp(XElement? element, string name, string? value)
    {
        if (element is null || value is null)
            return;

        var attribute = element.Attributes().FirstOrDefault(x => x.Name.LocalName == name);

        if (attribute is not null)
            attribute.Value = value;
    }

    /// <summary>
    /// Есть ли у бюро блок с ошибкой: такой ответ бюро могло и не прислать вовсе.
    /// </summary>
    public static bool HasError(XElement ответ, string ogrn)
        => Nodes(ответ, "Сведения")
            .SelectMany(x => Nodes(x, "КБКИ"))
            .Any(x => Attr(x, "ОГРН") == ogrn && Node(x, "Ошибка") is not null);

    public static int GetErrorCode(XElement кбки)
        => AttrInt(Node(кбки, "Ошибка"), "Код") ?? 0;

    public static string? GetErrorMessage(XElement кбки)
        => Normalize((string?)Node(кбки, "Ошибка"));

    private static bool HasAnySection(XElement кбки) => KbkiSections.Any(x => Node(кбки, x) is not null);

    private static int? ElemInt(XElement element, string name)
        => int.TryParse(Elem(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
