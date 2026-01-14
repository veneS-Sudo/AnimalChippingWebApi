using System.Data.Common;
using System.Net.Http.Headers;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor is not null) services.Remove(dbContextDescriptor);
            var dbConnectionDescriptor = services.SingleOrDefault(service => service.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor is not null) services.Remove(dbConnectionDescriptor);
            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                
                return connection;
            });
            services.AddDbContext<AppDbContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
            services.AddHostedService<DataBaseMigrationHostedService>();
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        var username = "test@test.com";
        var password = "123456789";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")));
    }
}