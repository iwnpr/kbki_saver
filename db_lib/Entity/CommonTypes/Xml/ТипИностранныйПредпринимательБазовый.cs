using System.Xml.Serialization;

namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипИностранныйПредпринимательБазовый
    {

        /// <remarks/>
        [XmlElement("НомерНП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("РегНомер")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public ТипФИО? ФИО { get; set; }
    }
}