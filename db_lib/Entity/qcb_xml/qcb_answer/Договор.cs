using db_lib.Entity.CommonTypes.Xml;

namespace db_lib.Entity.qcb_xml.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class Договор
    {

        /// <remarks/>
        public ТипСреднемесячныйПлатеж? СреднемесячныйПлатеж { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? УИД { get; set; }
    }
}