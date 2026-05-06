using db_lib.Entity.qcb_xml.Enums;

namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипЦель
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public ТипЦельКодЦели КодЦели { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Описание { get; set; }
    }
}