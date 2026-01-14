using db_lib.Entity.CommonTypes.Xml;

namespace db_lib.Entity.qcb_xml.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class КБКИ
    {
        public ОбязательствНет? ОбязательствНет { get; set; }

        public Обязательства? Обязательства { get; set; }

        public Ошибка? Ошибка { get; set; }

        public СубъектНеНайден? СубъектНеНайден { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public DateTime ПоСостояниюНа { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ИдентификаторОтвета { get; set; }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class СубъектНеНайден
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ОбязательствНет
    {
    }
}