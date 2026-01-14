using db_lib.Entity.qcb_xml.Enums;
using System.Xml.Serialization;

namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипИЮ
    {
        /// <summary>
        /// 
        /// </summary>
        public string признакРегистрацииРФ = "0";

        /// <remarks/>
        [XmlElement("НомерНП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("РегНомер")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public string? LEI { get; set; }

        /// <remarks/>
        public string? ПолноеНаименование { get; set; }

        /// <remarks/>
        public string? СокращенноеНаименование { get; set; }

        /// <remarks/>
        public string? ИноеНаименование { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public КодыВидаПользователя КодВидаПользователя { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string? ПризнакРегистрацииРФ { get; set; }
    }
}