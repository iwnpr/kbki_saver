using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace db_lib.Entity.CommonTypes.Api
{
    /// <summary>
    /// Интерфейс для взаимодействия с данными бюро в конфигах
    /// </summary>
    public interface IBKIRequisitsHandler
    {
        /// <summary>
        /// Получить список бюро
        /// </summary>
        /// <returns>Список бюро</returns>
        public ConcurrentBag<BKIReqiusit> GetBureaList();
    }

    /// <summary>
    /// Реквиизиты БКИ
    /// </summary>
    public class BKIRequsits : IBKIRequisitsHandler
    {
        /// <summary>
        /// Конфигурация
        /// </summary>
        private readonly IConfiguration _config;

        /// <summary>
        /// Список Бюро
        /// </summary>
        private ConcurrentBag<BKIReqiusit> _bureauList { get; set; } = new();

        /// <summary>
        /// Значения считаны
        /// </summary>
        private bool Initialized { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="config">Конфигурация</param>
        public BKIRequsits(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Получить список бюро
        /// </summary>
        /// <returns>Список бюро</returns>
        public ConcurrentBag<BKIReqiusit> GetBureaList()
        {
            if (!Initialized && _bureauList.Count == 0)
            {
                Initialize();
            }
            return _bureauList;
        }

        /// <summary>
        /// Инициализировать значения
        /// </summary>
        public void Initialize()
        {
            foreach (var item in _config.GetSection("QBCH").GetChildren())
            {
                _bureauList.Add(new BKIReqiusit()
                {
                    Name = item.GetSection("Name").Value,
                    ogrn = item.GetSection("Ogrn").Value
                });
            }

            Initialized = true;
        }
    }

    /// <summary>
    /// Реквизиты БКИ
    /// </summary>
    public class BKIReqiusit : Requisites
    {
        /// <summary>
        /// Наименование Бюро
        /// </summary>
        public string? Name { get; set; }
    }
}
