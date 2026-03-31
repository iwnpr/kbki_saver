using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Запросы dlrequest которые поступили к нам. Здесь есть как запросы от других КБКИ так и от наших контрагентов.
/// Код ответа 12 не отдается клиенту - является информационным.
/// </summary>
public partial class TeDlrequest
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    /// <summary>
    /// Дата и время поступления запроса dlrequest
    /// </summary>
    public DateTime RequestDateTime { get; set; }

    /// <summary>
    /// Время окончания валидации
    /// </summary>
    public DateTime? ValidationDateTime { get; set; }

    /// <summary>
    /// Время выполнения задачи по сбору данных в БД или из других КБКИ
    /// </summary>
    public DateTime? QbchTasksEndDateTime { get; set; }

    /// <summary>
    /// Дата и время ответа на dlrequest
    /// </summary>
    public DateTime? ResponseDateTime { get; set; }

    public string? QbchTasksResultXml { get; set; }

    /// <summary>
    /// Guid по которому будет сформирован ответ. Генерируется автоматически при получении запроса
    /// </summary>
    public string ResponseGuid { get; set; } = null!;

    public string? IpAddress { get; set; }

    /// <summary>
    /// Массив байт канального сертификата
    /// </summary>
    public byte[]? RequestCertificateData { get; set; }

    /// <summary>
    /// Отпечаток сертификата запроса (Канальный	 сертификат)
    /// </summary>
    public string? RequestCertificateThumbprint { get; set; }

    public int? AbonentId { get; set; }

    /// <summary>
    /// id запроса
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Атрибут «ТипЗапроса» заполняется одним из следующих значений:
    /// 1 – абонент запрашивает сведения только у запрашиваемого КБКИ;
    /// 2 – абонент запрашивает сведения всех КБКИ путем обращения в одно КБКИ (режим
    /// «одно окно»).
    /// </summary>
    public int? RequestType { get; set; }

    /// <summary>
    /// Атрибут «РежимЗапроса» заполняется одним из следующих значений:
    /// 1 – абонент запрашивает сведения в отношении одного субъекта;
    /// 2 – абонент запрашивает сведения в отношении нескольких субъектов (далее – пакет)
    /// </summary>
    public int? RequestMode { get; set; }

    /// <summary>
    /// Атрибут «КодСведений» заполняется одним из следующих значений:
    /// 3 – абонент запрашивает сведения о среднемесячных платежах Субъекта, включая
    /// сведения о запрете (снятии запрета) на заключение договоров потребительского займа
    /// (кредита);
    /// 6 – абонент запрашивает сведения о запрете (снятии запрета) на заключение договоров
    /// потребительского займа (кредита);
    /// </summary>
    public int? InformationCode { get; set; }

    public string? RequestXml { get; set; }

    public byte[]? RequestSignedData { get; set; }

    public string? ErrorMessage { get; set; }

    public int ErrorCode { get; set; }

    public string? ResponseXml { get; set; }

    public byte[]? ResponseSignedData { get; set; }

    public virtual TrAbonent? Abonent { get; set; }

    public virtual TrErrorCode ErrorCodeNavigation { get; set; } = null!;

    public virtual TrInformationCode? InformationCodeNavigation { get; set; }

    public virtual TrRequestMode? RequestModeNavigation { get; set; }

    public virtual TrDlrequestType? RequestTypeNavigation { get; set; }

    public virtual ICollection<TeQbchTask> TeQbchTasks { get; set; } = new List<TeQbchTask>();

    public virtual ICollection<TeRequest> TeRequests { get; set; } = new List<TeRequest>();
}
