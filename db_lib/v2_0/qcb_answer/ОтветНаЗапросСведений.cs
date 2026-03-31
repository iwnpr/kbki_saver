using QBCH_lib.qcb_xml.v2_0.Enums;
using QBCH_lib.qcb_xml.v2_0.qcb_result;
using System;
using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v2_0.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public class ОтветНаЗапросСведений
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Сведения")]
        public List<Сведения> Сведения { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Версия { get; set; } = "2.0";

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ДатаЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторОтвета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ТипОтвета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? РежимЗапроса { get; set; }


        /// <summary>
        /// Сформировать сведения о платежах с ошибкой, сформированной не на стороне КБКИ
        /// </summary>
        /// <param name="psrn">ОГРН КБКИ</param>
        /// <param name="errorCode">Код ошибки</param>
        /// <param name="errorValue">Значение ошибки</param>
        /// <returns>Сведения о платежах с ошибкой</returns>
        public static ОтветНаЗапросСведений CreateError(string psrn, string errorCode, string errorValue)
        {
            return new()
            {
                Версия = "2.0",
                ОГРН = psrn,
                Сведения = new()
            {
                new()
                {
                    КБКИ = new()
                    {
                        new()
                        {
                            ПоСостояниюНа = DateTime.Now,
                            ОГРН = psrn,
                            Ошибка = new()
                            {
                                Код = errorCode,
                                Value = errorValue,
                            }
                        }
                    }
                }
            }
            };
        }

        /// <summary>
        /// Сформировать сведения о платежах с ошибкой, которая вернулась из КБКИ
        /// </summary>
        /// <param name="psrn">ОГРН</param>
        /// <param name="error">Ошибка</param>
        /// <returns>Сведения о платежах с ошибкой КБКИ</returns>
        public static ОтветНаЗапросСведений CreateError(string psrn, Ошибка error)
        {
            return new()
            {
                Версия = "2.0",
                ОГРН = psrn,
                Сведения = new()
            {
                new()
                {
                    КБКИ = new()
                    {
                        new()
                        {
                            ПоСостояниюНа = DateTime.Now,
                            ОГРН = psrn,
                            Ошибка = error
                        }
                    }
                }
            }
            };
        }
    }
}