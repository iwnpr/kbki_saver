using StackExchange.Redis;

namespace db_lib.Services.Interfaces.V3;

/// <summary>
/// Контракт репозитория для сохранения сущностей API v3 из Redis-хэшей
/// </summary>
public interface IRepositoryV3
{
    /// <summary>
    /// Создаёт запись dlrequest (v3) на основе данных Redis
    /// </summary>
    /// <param name="hashKey">Ключ Redis-хэша с данными запроса</param>
    /// <param name="hashset">Содержимое Redis-хэша</param>
    /// <param name="checkAlreadySaved">Проверять ли по маркеру, в какие схемы запись уже прошла (true — при повторной обработке).</param>
    /// <returns><see langword="true"/>, если сохранение выполнено успешно; иначе <see langword="false"/>.</returns>
    Task<bool> CreateDlRequestV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false);

    /// <summary>
    /// Создаёт запись dlrequest (v3) на основе данных Redis
    /// </summary>
    /// <param name="hashKey">Ключ Redis-хэша с данными ответа</param>
    /// <param name="hashset">Содержимое Redis-хэша</param>
    /// <param name="checkAlreadySaved">Проверять ли по маркеру, в какие схемы запись уже прошла (true — при повторной обработке).</param>
    /// <returns><see langword="true"/>, если сохранение выполнено успешно; иначе <see langword="false"/>.</returns>
    Task<bool> CreateDlAnswerV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false);

    /// <summary>
    /// Создаёт запись dlput (v3) на основе данных Redis
    /// </summary>
    /// <param name="hashKey">Ключ Redis-хэша с данными запроса dlput</param>
    /// <param name="hashset">Содержимое Redis-хэша</param>
    /// <param name="checkAlreadySaved">Проверять ли по маркеру, в какие схемы запись уже прошла (true — при повторной обработке).</param>
    /// <returns><see langword="true"/>, если сохранение выполнено успешно; иначе <see langword="false"/>.</returns>
    Task<bool> CreateDlPutV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false);

    /// <summary>
    /// Создаёт запись dlputanswer (v3) на основе данных Redis
    /// </summary>
    /// <param name="hashKey">Ключ Redis-хэша с данными ответа dlput</param>
    /// <param name="hashset">Содержимое Redis-хэша</param>
    /// <param name="checkAlreadySaved">Проверять ли по маркеру, в какие схемы запись уже прошла (true — при повторной обработке).</param>
    /// <returns><see langword="true"/>, если сохранение выполнено успешно; иначе <see langword="false"/>.</returns>
    Task<bool> CreateDlPutAnswerV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false);
}