using db_lib.Entity.CommonTypes.Xml;
using db_lib.Entity.qcb_xml.Enums;
using System;

namespace db_lib.Entity.qcb_xml.qcb_result
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Результат
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ИдентификаторОтвета", typeof(РезультатИдентификаторОтвета))]
        [System.Xml.Serialization.XmlElement("Ошибка", typeof(Ошибка))]
        [System.Xml.Serialization.XmlElement("Успешно", typeof(РезультатУспешно))]
        public object? Item { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Версия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <summary>
        /// Создать тикет с ответом
        /// </summary>
        /// <param name="type">Тикет/Ошибка</param>
        /// <param name="code">Код ошибки</param>
        /// <param name="text">Текст ошибки</param>
        /// <param name="requestId">id запроса</param>
        /// <param name="guid">guid ответа</param>
        /// <returns></returns>
        public static Результат CreateResult(ResultType type, string? code = null, string? text = null, string? requestId = null, string? guid = null)
        {
            switch (type)
            {
                case ResultType.Ticket:
                    return new Результат()
                    {
                        ОГРН = "1077333000003",
                        Версия = "1.2",
                        Item = new РезультатИдентификаторОтвета()
                        {
                            ИдентификаторЗапроса = requestId,
                            Value = guid
                        }
                    };
                case ResultType.Error:
                    return new Результат()
                    {
                        ОГРН = "1077333000003",
                        Версия = "1.2",
                        Item = new Ошибка()
                        {
                            Код = code,
                            Value = text
                        }
                    };
                default:
                    throw new Exception("type не определен");
            }
        }
    }
}