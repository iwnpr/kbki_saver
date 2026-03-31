using QBCH_lib.qcb_xml.v2_0.CommonTypes;
using QBCH_lib.qcb_xml.v2_0.qcb_result;

namespace QBCH_lib.qcb_xml.v2_0.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class КБКИ
    {
        public ОбязательствНет? ОбязательствНет { get; set; }

        public Обязательства? Обязательства { get; set; }

        public Ошибка? Ошибка { get; set; }

        public СведенийОЗапретеНет? СведенийОЗапретеНет { get; set; }

        public СведенияОЗапретеНеПредоставляются? СведенияОЗапретеНеПредоставляются { get; set; }

        public СубъектНеНайден? СубъектНеНайден { get; set; }

        public УсловияЗапрета? УсловияЗапрета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public System.DateTime ПоСостояниюНа { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторОтвета { get; set; }
    }
}