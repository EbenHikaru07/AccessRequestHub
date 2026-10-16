using AccessRequestHub.Web.Data;
using AccessRequestHub.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessRequestHub.Tests
{
    public class DatabaseFixture : IAsyncLifetime
    {
        public const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=AccessRequestHub_Test;Trusted_Connection=True;TrustServerCertificate=True";

        public AppDbContext NewContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

        // Setiap service memakai DbContext sendiri, seperti request web yang terpisah
        public AccessRequestService NewService() =>
            new(NewContext(), NullLogger<AccessRequestService>.Instance);

        public async Task InitializeAsync()
        {
            await using var db = NewContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();   // sekaligus membuktikan migration valid
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [CollectionDefinition("db")]
    public class DbCollection : ICollectionFixture<DatabaseFixture> { }
}
