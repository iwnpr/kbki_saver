namespace QBCH_lib.qcb_xml.v3_0.qcb_result
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public class Результат
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ИдентификаторОтвета", typeof(РезультатИдентификаторОтвета))]
        [System.Xml.Serialization.XmlElement("Ошибка", typeof(Ошибка))]
        [System.Xml.Serialization.XmlElement("Успешно", typeof(РезультатУспешно))]
        public object РезультатДанные { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Версия { get; set; } = "3.0";

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ОГРН { get; set; }

        public static Результат CreateSuccess(string psrn, string requestId, System.DateTime requestDate)
        {
            return new Результат
            {
                ОГРН = psrn,
                РезультатДанные = new РезультатУспешно
                {
                    ИдентификаторЗапроса = requestId,
                    ДатаЗапроса = requestDate
                }
            };
        }

        public static Результат CreateError(string psrn, string errorCode, string errorText)
        {
            return new Результат
            {
                ОГРН = psrn,
                РезультатДанные = new Ошибка
                {
                    Код = errorCode,
                    Value = errorText
                }
            };
        }

        public static Результат CreateTicket(string psrn, string requestId, System.DateTime requestDate, string responseId, long? readyInSeconds = null)
        {
            return new Результат
            {
                ОГРН = psrn,
                РезультатДанные = new РезультатИдентификаторОтвета
                {
                    ИдентификаторЗапроса = requestId,
                    ДатаЗапроса = requestDate,
                    Value = responseId,
                    ВремяГотовности = readyInSeconds.GetValueOrDefault(),
                    ВремяГотовностиSpecified = readyInSeconds.HasValue
                }
            };
        }
    }
}