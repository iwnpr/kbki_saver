using db_lib.Entity.qcb_xml.Enums;

namespace db_lib.Entity.CommonTypes.Xml
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипЮЛ
    {
        /// <summary>
        /// 
        /// </summary>
        public string признакРегистрацииРФ = "1";

        /// <remarks/>
        public string? ИНН { get; set; }

        /// <remarks/>
        public string? ОГРН { get; set; }

        /// <remarks/>
        public string? ПолноеНаименование { get; set; }

        /// <remarks/>
        public string? СокращенноеНаименование { get; set; }

        /// <remarks/>
        public string? ИноеНаименование { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public КодыВидаПользователя КодВидаПользователя { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ПризнакРегистрацииРФ { get; set; }
    }
}