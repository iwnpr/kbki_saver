namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class Ошибка
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "integer")]
        public string? Код { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlText()]
        public string? Value { get; set; }
    }
}