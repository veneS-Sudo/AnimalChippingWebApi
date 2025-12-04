using AnimalChippingWebApi.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimalChippingWebApi.DataBaseLayer.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> context) 
        : base(context)
    { }
    
    public DbSet<Account> Accounts { get; set; }
    public DbSet<LocationPoint> Locations { get; set; }
    public DbSet<Animal> Animals { get; set; }
    public DbSet<AnimalType> AnimalTypes { get; set; }
    public DbSet<AnimalVisitedLocation> AnimalVisitedLocations { get; set; }
}