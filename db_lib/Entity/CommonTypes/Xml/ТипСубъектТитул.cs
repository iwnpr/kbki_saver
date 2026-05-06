using System.Collections.Generic;

namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипСубъектТитул
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ФИО")]
        public List<ТипФИО>? ФИО { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement(DataType = "date")]
        public DateTime ДатаРождения { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ДокументЛичности")]
        public List<ТипДУЛ>? ДокументЛичности { get; set; }

        /// <remarks/>
        public string? ИНН { get; set; }

        /// <remarks/>
        public string? ИнНомер { get; set; }

        /// <remarks/>
        public string? СНИЛС { get; set; }
    }
}