using db_lib.Entities;
using Microsoft.EntityFrameworkCore;

namespace qbch_db_saver.test.moq;

internal class MoqDbContext
{
    public static qbchContext Create()
    {
        var option = new DbContextOptionsBuilder<qbchContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new qbchContext(option);
        return ctx;
    }
}
