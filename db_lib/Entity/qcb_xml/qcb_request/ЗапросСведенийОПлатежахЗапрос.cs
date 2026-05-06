using db_lib.Entity.CommonTypes.Xml;
using System.Collections.Generic;

namespace db_lib.Entity.qcb_xml.qcb_request
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ЗапросСведенийОПлатежахЗапрос
    {

        /// <remarks/>
        public ЗапросИсточник? Источник { get; set; }

        /// <remarks/>
        public ТипСубъектТитул? Субъект { get; set; }

        /// <remarks/>
        public ТипСогласие? Согласие { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Цель")]
        public List<ТипЦель>? Цель { get; set; }

        /// <remarks/>
        public ЗапросСуммаОбязательства? СуммаОбязательства { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public DateTime Дата { get; set; }
    }
}