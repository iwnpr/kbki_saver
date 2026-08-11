using Confluent.Kafka;
using static db_lib.Services.Implementations.SaverService;

namespace db_lib.Services.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISaverService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="_producer"></param>
        /// <param name="_errorTopic"></param>
        /// <returns></returns>
        Task ErrorTopicHandler(string key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task TopicHandler(string key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task SaveCriticalError(string message);
    }
}
