using System.Xml.Serialization;

namespace db_lib.Entity.qcb_xml.qcb_request
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class АбонентИностранноеЛицо
    {

        /// <remarks/>
        [XmlElement("НомерНП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("РегНомер")]
        public string? ОГРН { get; set; }
    }
}