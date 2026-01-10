using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin;
using ServiceLayer.Admin.AccountServices;
using ServiceLayer.Admin.AnimalServices;
using ServiceLayer.Admin.AnimalServices.LocationServices;
using ServiceLayer.Admin.TypeAnimalServices;
using ServiceLayer.Admin.VisitedLocationServices;
using ServiceLayer.Authentication;
using ServiceLayer.Registration;

namespace ServiceLayer;

public static class Bootstraps
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Add account service
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IGenericValidator<Account>, AccountValidatorExists>();
        
        // Add animal service
        services.AddScoped<IAnimalService, AnimalService>();
        services.AddScoped<IGenericValidator<Animal>, AnimalValidatorExists>();
        services.AddScoped<IGenericValidator<LocationPoint>, LocationPointValidatorExists>();
        
        // Add change animal type service
        services.AddScoped<IChangeAnimalTypeAtAnimalService, ChangeAnimalTypeAtAnimalService>();
        services.AddScoped<IGenericValidator<AnimalType>, AnimalTypeValidatorExists>();
        
        // add location service
        services.AddScoped<ILocationService, LocationService>();
        
        // Add type animal service
        services.AddScoped<ITypeAnimalService, TypeAnimalService>();
        
        // Add visited location service
        services.AddScoped<IVisitedLocationAnimalService, VisitedLocationAnimalService>();
        services.AddScoped<IGenericValidator<AnimalVisitedLocation>, VisitedLocationValidatorExists>();
        
        // Add registration user service
        services.AddScoped<IRegistrationUserService, RegistrationUserService>();
        
        // Add authenticationService
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        return services;
    }
}