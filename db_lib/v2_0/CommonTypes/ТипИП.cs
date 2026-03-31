using QBCH_lib.qcb_xml.v2_0.qcb_request;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v2_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипИП
    {

        /// <remarks/>
        [XmlElement("ИННИП")]
        public required string ИНН { get; set; }

        /// <remarks/>
        [XmlElement("ОГРНИП")]
        public required string ОГРН { get; set; }

        /// <remarks/>
        public required string СНИЛС { get; set; }

        /// <remarks/>
        public required ТипФИО ФИО { get; set; }

        /// <remarks/>
        public required ТипДУЛПредпринимателя ДокументЛичности { get; set; }

        /// <remarks/>
        [XmlElement(DataType = "date")]
        public System.DateTime ДатаРождения { get; set; }

        /// <remarks/>
        public required string МестоРождения { get; set; }
    }
}