using db_lib.Entity.CommonTypes.Xml;
using System.Xml.Serialization;

namespace db_lib.Entity.qcb_xml.qcb_request
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ЗапросИсточник
    {
        private ТипИП? _ИндивидуальныйПредприниматель;
        private ТипИностранныйПредприниматель? _ИностранныйПредприниматель;
        private ЗапросИсточникИностранноеЮЛ? _ИностранноеЮЛ;
        private ЗапросИсточникЮридическоеЛицо? _ЮридическоеЛицо;

        /// <summary>
        /// 
        /// </summary>
        public ТипИП? ИндивидуальныйПредприниматель {
            get
            {
                return _ИндивидуальныйПредприниматель;
            }
            set
            {
                Ogrn = value?.ОГРН;
                _ИндивидуальныйПредприниматель = value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public ЗапросИсточникИностранноеЮЛ? ИностранноеЮЛ {
            get
            {
                return _ИностранноеЮЛ;
            }
            set
            {
                Ogrn = value?.ОГРН;
                _ИностранноеЮЛ = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ТипИностранныйПредприниматель? ИностранныйПредприниматель
        {
            get
            {
                return _ИностранныйПредприниматель;
            }
            set
            {
                Ogrn = value?.ОГРН;
                _ИностранныйПредприниматель = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ЗапросИсточникЮридическоеЛицо? ЮридическоеЛицо {
            get 
            { 
                return _ЮридическоеЛицо; 
            }
            set
            {
                Ogrn = value?.ОГРН;
                _ЮридическоеЛицо = value;
            }
        }

        [XmlIgnore]
        public string? Ogrn { get; set; }
    }
}