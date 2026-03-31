using NetTopologySuite.Precision;
using QBCH_lib.qcb_xml.v2_0.CommonTypes;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v2_0.qcb_request
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ЗапросСведенийЗапросИсточник
    {
        
        private ТипИП? _ИндивидуальныйПредприниматель;
        private ЗапросСведенийЗапросИсточникИностранноеЮЛ? _ИностранноеЮЛ;
        private ТипИностранныйПредприниматель? _ИностранныйПредприниматель;
        private ЗапросСведенийЗапросИсточникЮридическоеЛицо? _ЮридическоеЛицо;

        public ТипИП? ИндивидуальныйПредприниматель
        {
            get => _ИндивидуальныйПредприниматель;
            set 
            {
                CommonITN = _ИндивидуальныйПредприниматель?.ИНН;
                CommonPSRN = _ИндивидуальныйПредприниматель?.ОГРН;
                _ИндивидуальныйПредприниматель = value;
            }
        }
        public ЗапросСведенийЗапросИсточникИностранноеЮЛ? ИностранноеЮЛ
        {
            get => _ИностранноеЮЛ;
            set
            {
                CommonITN = _ИностранноеЮЛ?.ИНН;
                CommonPSRN = _ИностранноеЮЛ?.ОГРН;
                _ИностранноеЮЛ = value;
            }
        }
        public ТипИностранныйПредприниматель? ИностранныйПредприниматель
        {
            get => _ИностранныйПредприниматель;
            set
            {
                CommonITN = _ИностранныйПредприниматель?.ИНН;
                CommonPSRN = _ИностранныйПредприниматель?.ОГРН;
                _ИностранныйПредприниматель = value;
            }
        }
        public ЗапросСведенийЗапросИсточникЮридическоеЛицо? ЮридическоеЛицо
        {
            get => _ЮридическоеЛицо;
            set
            {
                CommonITN = _ЮридическоеЛицо?.ИНН;
                CommonPSRN = _ЮридическоеЛицо?.ОГРН;
                _ЮридическоеЛицо = value;
            }
        }

        [XmlIgnore]
        public string? CommonITN { get; set; }
        [XmlIgnore]
        public string? CommonPSRN { get; set; }
    }
}