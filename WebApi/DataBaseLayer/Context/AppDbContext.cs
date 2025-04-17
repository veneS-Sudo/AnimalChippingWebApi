using AnimalChippingWebApi.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimalChippingWebApi.DataBaseLayer.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> context) 
        : base(context)
    { }
    
    DbSet<Account> Accounts { get; set; }
    DbSet<LocationPoint> Locations { get; set; }
    DbSet<Animal> Animals { get; set; }
    DbSet<AnimalType> AnimalTypes { get; set; }
    DbSet<AnimalVisitedLocation> AnimalVisitedLocations { get; set; }
}