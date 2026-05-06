namespace db_lib.Entity.CommonTypes.Api
{
    /// <summary>
    /// Ошибка
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="message">Сообщение</param>
        public Error(string? message = null)
        {
            Message = message;
        }

        /// <summary>
        ///  Текст ошибки
        /// </summary>
        public string? Message { get; set; }
    }
}