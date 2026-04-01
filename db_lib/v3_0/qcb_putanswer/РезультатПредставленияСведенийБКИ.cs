using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_putanswer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class РезультатПредставленияСведенийБКИ
    {

        /// <remarks/>
        [XmlAttribute()]
        public string ОГРН { get; set; }

        /// <remarks/>
        [XmlText()]
        public string Value { get; set; }
    }
}