using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace db_lib.Entities;


public class QbchSecondaryContext : QbchContext
{
    public QbchSecondaryContext(DbContextOptions<QbchSecondaryContext> options, IConfiguration configuration)
        : base(options, configuration.GetValue<string>("Database:SchemaSecondary")) { }
}
