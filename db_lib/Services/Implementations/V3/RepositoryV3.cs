using db_lib.Entities;
using db_lib.Services.Interfaces.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QBCH.Lib.qcb_xml.v3_0;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xml_service_lib;

namespace db_lib.Services.Implementations.V3;

public class RepositoryV3(qbchContext context, ILogger<RepositoryV3> logger, IXmlService xmlService) : IRepositoryV3
{
    private readonly qbchContext _context = context;
    private readonly ILogger<RepositoryV3> _logger = logger;
    private readonly IXmlService _xmlService = xmlService;

    private static DateTime? GetDateTimeValue(string? value, string pattern = "dd.MM.yyyy HH:mm:ss:ffff")
        => DateTime.TryParseExact(value, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null;

    private static string? TryParseXmlBytesToString(byte[]? bytes)
    {
        try
        {
            return bytes is not null
                ? XDocument.Load(new MemoryStream(bytes)).ToString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private T? TryDeserialize<T>(string? value) where T : class
    {
        try { return _xmlService.Deserialize<T>(value); }
        catch (Exception ex) { _logger.LogDebug(ex, "V3 deserialize error"); }
        return default;
    }

    private async Task<TrAbonent?> GetAbonentByThumbprint(string? thumbprint)
    {
        var upper = thumbprint?.ToUpper();
        if (string.IsNullOrWhiteSpace(upper)) return null;
        return await _context.TrAbonentCertificates.Include(x => x.Abonent)
            .Where(x => x.Thumbprint.ToUpper() == upper)
            .Select(x => x.Abonent)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> SaveAsync(string hashKey)
    {
        try { await _context.SaveChangesAsync(); return true; }
        catch (Exception ex) { _logger.LogCritical(ex, "Ошибка записи V3 в БД. Ключ {key}", hashKey); return false; }
    }

    public Task<bool> CreateDlRequestV3(string hashKey, HashEntry[]? hashset)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateDlAnswerV3(string hashKey, HashEntry[]? hashset)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateDlPutV3(string hashKey, HashEntry[]? hashset)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateDlPutAnswerV3(string hashKey, HashEntry[]? hashset)
    {
        throw new NotImplementedException();
    }
}

