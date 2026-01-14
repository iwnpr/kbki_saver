using db_lib.Entity.qcb_xml.qcb_result;

namespace db_lib.Entity.CommonTypes.Api
{
    /// <summary>
    /// Базовый класс ошибки
    /// </summary>
    public class BaseResult
    {
        /// <summary>
        /// Список ошибок
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Тип ошибки
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Тикет
        /// </summary>
        public Результат? Ticket { get; set; }
    }
}
