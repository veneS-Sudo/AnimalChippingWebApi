using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WevApi.Tests;

public class DataBaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DataBaseMigrationHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
        string email = "test@test.com", password = "123456789", firstName = "John", lastName = "Doe";
        await context.Database.EnsureCreatedAsync(cancellationToken);
        context.Accounts.Add(new Account()
            { FirstName = firstName, LastName = lastName, Email = email, Password = password });
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}