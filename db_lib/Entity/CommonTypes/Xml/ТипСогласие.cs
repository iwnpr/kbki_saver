using db_lib.Entity.qcb_xml.Enums;
using System.Collections.Generic;

namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипСогласие
    {
        public string обОтветственностиПредупрежден = "1";

        /// <remarks/>
        public ТипСогласиеВыдано? Выдано { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Цель")]
        public List<ТипЦель>? Цель { get; set; }

        /// <remarks/>
        public ТипСогласиеДоговор? Договор { get; set; }

        /// <remarks/>
        public string? ХэшКод { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public DateTime ДатаВыдачи { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public КодыСрокаСогласия СрокДействия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public КодыОснованийПередачиСогласия ОснованиеПередачи { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnore()]
        public bool ОснованиеПередачиSpecified { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОбОтветственностиПредупрежден { get; set; }
    }
}