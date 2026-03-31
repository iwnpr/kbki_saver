using QBCH_lib.qcb_xml.v2_0.CommonTypes;
using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v2_0.qcb_request
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class Запрос
    {

        /// <remarks/>
        public required ЗапросСведенийЗапросИсточник Источник { get; set; }

        /// <remarks/>
        public required ТипСубъектТитул Субъект { get; set; }

        /// <remarks/>
        public ТипСогласие? Согласие { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Цель")]
        public List<ТипЦель> Цель { get; set; } = new();

        /// <remarks/>
        public ЗапросСведенийЗапросСуммаОбязательства? СуммаОбязательства { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public required int ПорядковыйНомер { get; set; }
    }
}