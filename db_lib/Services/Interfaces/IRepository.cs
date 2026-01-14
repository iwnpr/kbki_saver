using Confluent.Kafka;
using db_lib.DBEntity;
using db_lib.Entity.qcb_xml.qcb_put;
using db_lib.Entity.qcb_xml.qcb_request;

namespace db_lib.Services.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        Task SaveDlRequest(string key, string guid);

        /// <summary>
        /// Запись запроса dlputrequest
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="thumbprint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task SaveDlPut(string key, string guid);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task SaveDlAnswer(string key, string guid);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        Task SaveDlPutAnswer(string key, string guid);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="_producer"></param>
        /// <param name="_errorTopic"></param>
        /// <returns></returns>
        Task SaveToDB(string key, IProducer<Null, string> _producer, string _errorTopic);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task SaveToDBErrorApp(string key);
    }
}
