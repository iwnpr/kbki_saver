using System.Collections.Generic;

namespace db_lib.Entity.qcb_xml.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class Обязательства
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElement("БКИ")]
        public List<БКИ>? БКИ { get; set; }
    }
}