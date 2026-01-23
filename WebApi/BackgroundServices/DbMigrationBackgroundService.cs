using DatabaseLayer.Context;

namespace AnimalChippingWebApi.BackgroundServices;

public class DbMigrationBackgroundService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DbMigrationBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}