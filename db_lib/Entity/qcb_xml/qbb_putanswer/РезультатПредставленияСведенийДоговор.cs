using db_lib.Entity.CommonTypes.Xml;
using db_lib.Entity.qcb_xml.Enums;
using System;
using System.Xml.Serialization;

namespace db_lib.Entity.qcb_xml.qbb_putanswer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class РезультатПредставленияСведенийДоговор
    {
        /// <remarks/>
        [XmlElement("Ошибка", typeof(Ошибка))]
        [XmlElement("Успешно", typeof(object))]
        public object? Item { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string? УИД { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string? ДатаРасчета { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public СправочникОперации Операция { get; set; }
    }
}