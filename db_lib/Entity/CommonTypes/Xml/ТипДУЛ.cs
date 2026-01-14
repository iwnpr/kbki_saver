using db_lib.Entity.qcb_xml.Enums;

namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипДУЛ
    {

        /// <remarks/>
        public string? Серия { get; set; }

        /// <remarks/>
        public string? Номер { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement(DataType = "date")]
        public DateTime ДатаВыдачи { get; set; }

        /// <remarks/>
        public string? Гражданство { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникДУЛ КодДУЛ { get; set; }
    }
}