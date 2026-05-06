namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипСреднемесячныйПлатеж
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Валюта { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ДатаРасчета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlText()]
        public double Value { get; set; }
    }
}