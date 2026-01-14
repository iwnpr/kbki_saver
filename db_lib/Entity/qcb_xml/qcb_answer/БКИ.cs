using System.Collections.Generic;

namespace db_lib.Entity.qcb_xml.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class БКИ
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Договор")]
        public List<Договор>? Договор { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }
    }
}