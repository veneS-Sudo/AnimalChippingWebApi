using System.Globalization;
using System.Security.Claims;
using AnimalChippingWebApi.Authentication;
using AnimalChippingWebApi.DataBaseLayer.Context;
using AnimalChippingWebApi.Middlewares;
using AnimalChippingWebApi.Models;
using AnimalChippingWebApi.Models.Animal;
using AnimalChippingWebApi.Models.Location;
using AnimalChippingWebApi.Models.VisitedLocation;
using AnimalChippingWebApi.ValidationFilters;
using DatabaseLayer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using ServiceLayer;
using ServiceLayer.Admin;
using ServiceLayer.Admin.AccountServices;
using ServiceLayer.Admin.AnimalServices.LocationServices;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Admin.Generic;
using ServiceLayer.Admin.TypeAnimalServices;
using ServiceLayer.Admin.VisitedLocationServices;
using ServiceLayer.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Animal Chipping API", Version = "v1" });
    var security = new OpenApiSecurityScheme
    {
        Name = HeaderNames.Authorization,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Basic Authorization header using the Basic scheme.",
        Reference = new OpenApiReference
        {
            Id = UserAuthenticationHandler.AuthenticationSchemeName,
            Type = ReferenceType.SecurityScheme
        }
    };
    options.AddSecurityDefinition(security.Reference.Id, security);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {{security, Array.Empty<string>()}});
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddProblemDetails();
builder.Services.AddCoreServices();

builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, UserAuthenticationHandler>("Basic", null);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRouting();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ExceptionExistsHandlerMiddleware>();
app.UseMiddleware<ExceptionUniqueValueHandlerMiddleware>();
app.UseMiddleware<ExceptionInvalidDataHandlerMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var groupWithValidationId = app.MapGroup("/")
    .AddEndpointFilterFactory(IdValidationFilterFactory.ValidateParameterIdFactory)
    .RequireAuthorization();
var groupWithValidationSearchParams = app.MapGroup("/")
    .AddEndpointFilterFactory(SearchParamsValidationFilterFactory.ValidateSearchPaginationParamsFactory)
    .RequireAuthorization();


// ===========================================
// Account endpoints
// ===========================================
groupWithValidationId.MapGet("/accounts/{accountId:int}", (int accountId, 
    [FromServices] IAccountService accountService) =>
{
    var account = accountService.GetAccountById(accountId);
    return TypedResults.Ok(account);
});
groupWithValidationSearchParams.MapGet("/accounts/search", ([AsParameters] AccountSearchDto accountSearchDto,
    [FromServices] IAccountService accountService) =>
{
    var searchOptions = new AccountSortFilterPageOptions()
    {
        PageNum = (accountSearchDto.From / accountSearchDto.Size) ?? 1,
        PageSize = accountSearchDto.Size ?? SortFilterPageBaseOptions.DefaultPageSize,
        filterValues = new AccountSearchFilterValues()
        {
            FirstName = accountSearchDto.FirstName,
            LastName = accountSearchDto.LastName,
            Email = accountSearchDto.Email
        }
    };
    var result = accountService.GetAccounts(searchOptions)
        .Skip((accountSearchDto.From % accountSearchDto.Size) ?? 0).ToList();
    return TypedResults.Ok(result);
}).WithParameterValidation();
app.MapPost("/registration",
    ([FromBody] UserRegistrationDto dto, ClaimsPrincipal user,
        [FromServices] IRegistrationUserService registrationService, [FromServices] IAccountService accountService) => 
    {
        if (user.Identity?.IsAuthenticated ?? false)
        {
            return Results.Problem("You are already logged in", statusCode: 403);
        }
        var accountId = registrationService.RegistrationUser(dto);
        var account = accountService.GetAccountById(accountId);
        return Results.Created($"/accounts/{accountId}", account);
    }).WithParameterValidation();
groupWithValidationId.MapPut("/accounts/{accountId:int}", (int accountId, 
    [FromBody] UserUpdateDto dto, [FromServices] IAccountService accountService) =>
{
    var isUpdated = accountService.UpdateAccountById(accountId, new UpdateAccountDto()
    {
        Email = dto.Email,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Password = dto.Password,
    });
    if (isUpdated)
    {
        var account = accountService.GetAccountById(accountId);
        return TypedResults.Ok(account);
    }
    return Results.NoContent();
}).WithParameterValidation()
.AddEndpointFilter<OwnedAccountValidationFilter>();
groupWithValidationId.MapDelete("/accounts/{accountId:int}", (int accountId, 
    [FromServices] IAccountService accountService) =>
{
    var isDeleted = accountService.DeleteAccountById(accountId);
    if (isDeleted)
        return Results.Ok();
    return Results.NoContent();
}).AddEndpointFilter<OwnedAccountValidationFilter>();


// ===========================================
// animal endpoints
// ===========================================
groupWithValidationId.MapGet("/animals/{animalId:long}", (long animalId, 
    [FromServices] IAnimalService animalService) =>
{
    var animal = animalService.GetAnimalById(animalId).ToDto();
    return TypedResults.Ok(animal);
});
groupWithValidationSearchParams.MapGet("/animals/search", ([AsParameters] AnimalSearchDto animalSearchDto,
        [FromServices] IAnimalService animalService) =>
    {
        if (!Enum.TryParse<LiveStatusType>(animalSearchDto.LifeStatus, true, out var lifeStatusParsed)
            && animalSearchDto.LifeStatus is not null)
            return Results.Problem("Available life status: " + string.Join(", ", Enum.GetNames<LiveStatusType>()),
                statusCode: 400);
        if (!Enum.TryParse<GenderType>(animalSearchDto.Gender, true, out var genderParsed)
            && animalSearchDto.Gender is not null)
            return Results.Problem("Available gender: " + string.Join(", ", Enum.GetNames<GenderType>()),
                statusCode: 400);
        DateTime? startDateTimeParsed = animalSearchDto.StartDateTime is not null
            ? DateTime.ParseExact(animalSearchDto.StartDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
        DateTime? endDateTimeParsed = animalSearchDto.EndDateTime is not null
            ? DateTime.ParseExact(animalSearchDto.EndDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
        var searchOptions = new AnimalSortFilterPageOptions()
        {
            PageNum = (animalSearchDto.From / animalSearchDto.Size) ?? 1,
            PageSize = animalSearchDto.Size ?? SortFilterPageBaseOptions.DefaultPageSize,
            FilterValues = new AnimalSearchFilterValues()
            {
                ChipperId = animalSearchDto.ChipperId,
                ChippingLocationId = animalSearchDto.ChipperLocationId,
                Gender = animalSearchDto.Gender is not null ? genderParsed : null,
                LiveStatus = animalSearchDto.LifeStatus is not null ? lifeStatusParsed : null,
                StartDate = animalSearchDto.StartDateTime is not null ? startDateTimeParsed : null,
                EndDate = animalSearchDto.EndDateTime is not null ? endDateTimeParsed : null
            }
        };
        var result = animalService.GetAnimals(searchOptions)
            .Skip((animalSearchDto.From % animalSearchDto.Size) ?? 0).Select(a => a.ToDto()).ToList();
        return TypedResults.Ok(result);
    }).WithParameterValidation()
    .AddEndpointFilterFactory(SearchParamsValidationFilterFactory.ValidateSearchDateParamsFactory)
    .AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory);
app.MapPost("/animals", ([FromBody] CreateAnimalDto dto, [FromServices] IAnimalService animalService) =>
    {
        if (!Enum.TryParse<GenderType>(dto.Gender, true, out var _))
            return Results.Problem("Available gender: " + string.Join(", ", Enum.GetNames<GenderType>()),
                statusCode: 400);
        var animalId = animalService.AddNewAnimal(dto.ToAddAnimalDto());
        var animal = animalService.GetAnimalById(animalId).ToDto();
        return Results.Created($"/animals/{animalId}", animal);
    }).RequireAuthorization().WithParameterValidation()
    .AddEndpointFilter<BaseAnimalIndicatorFilter>()
    .AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory)
    .AddEndpointFilter<AnimalTypesValidatorFilter>();
groupWithValidationId.MapPut("/animals/{animalId:long}", ([FromBody] UpdateAnimalDto dto, long animalId, 
    [FromServices] IAnimalService animalService) =>
    {
        if (!Enum.TryParse<LiveStatusType>(dto.LiveStatus, true, out _))
            return Results.Problem("Available life status: " + string.Join(", ", Enum.GetNames<LiveStatusType>()),
                statusCode: 400);
        if (!Enum.TryParse<GenderType>(dto.Gender, true, out var _))
            return Results.Problem("Available gender: " + string.Join(", ", Enum.GetNames<GenderType>()),
                statusCode: 400);
        var isUpdated = animalService.UpdateDataAnimal(animalId, dto.ToUpdateDataAnimalDto());
        if (isUpdated)
        {
            var animal = animalService.GetAnimalById(animalId).ToDto();
            return TypedResults.Ok(animal);
        }
        return Results.NoContent();
    }).WithParameterValidation()
.AddEndpointFilter<BaseAnimalIndicatorFilter>()
.AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory);
groupWithValidationId.MapDelete("/animals/{animalId:long}",
    (long animalId, [FromServices] IAnimalService animalService) =>
    {
        var isDeleted = animalService.SoftDelete(animalId);
        if (isDeleted)
        {
            return Results.Ok();
        }

        return Results.NoContent();
    });
groupWithValidationId.MapPost("/animals/{animalId:long}/types/{typeId:long}",
    (long animalId, long typeId, [FromServices] IChangeAnimalTypeAtAnimalService changeAnimalTypeService,
        [FromServices] IAnimalService animalService) =>
    {
        var createdAnimalId = changeAnimalTypeService.AddAnimalTypeAtAnimal(animalId, typeId);
        var animal = animalService.GetAnimalById(createdAnimalId).ToDto();
        return Results.Created($"/animals/{createdAnimalId}", animal);
    });
groupWithValidationId.MapPut("/animals/{animalId:long}/types",
    (long animalId, [FromBody] UpdateAnimalTypeInAnimalDataDto dto,
        [FromServices] IChangeAnimalTypeAtAnimalService changeAnimalTypeService,
        [FromServices] IAnimalService animalService) =>
    {
        var updatedAnimalId = changeAnimalTypeService.UpdateAnimalTypeAtAnimal(animalId, dto);
        var updatedAnimal = animalService.GetAnimalById(updatedAnimalId).ToDto();
        return TypedResults.Ok(updatedAnimal);
    }).WithParameterValidation().
    AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory);
groupWithValidationId.MapDelete("/animals/{animalId:long}/types/{typeId:long}",
    (long animalId, long typeId, [FromServices] IChangeAnimalTypeAtAnimalService changeAnimalTypeService,
        [FromServices] IAnimalService animalService) =>
    {
        var changedAnimalId = changeAnimalTypeService.DeleteAnimalTypeAtAnimal(animalId, typeId);
        var animal = animalService.GetAnimalById(changedAnimalId).ToDto();
        return TypedResults.Ok(animal);
    });


// ===========================================
// animal type endpoints
// ===========================================
groupWithValidationId.MapGet("/animals/types/{typeId:long}", (long typeId,
    [FromServices] ITypeAnimalService typeAnimalService) =>
{
    var typeAnimal = typeAnimalService.GetAnimalTypeById(typeId);
    return Results.Ok(typeAnimal);
});
app.MapPost("/animals/types",
    ([FromBody] CreateUpdateAnimalTypeDto dto, [FromServices] ITypeAnimalService typeAnimalService) =>
    {
        var typeId = typeAnimalService.AddAnimalType(dto);
        var animalType = typeAnimalService.GetAnimalTypeById(typeId);
        return Results.Created($"/animals/types/{typeId}", animalType);
    }).RequireAuthorization().WithParameterValidation();
groupWithValidationId.MapPut("/animals/types/{typeId:long}", (long typeId, [FromBody] CreateUpdateAnimalTypeDto dto,
    [FromServices] ITypeAnimalService typeAnimalService) =>
{
    var isUpdated = typeAnimalService.UpdateAnimalType(new AnimalType { AnimalTypeId = typeId, Type = dto.Type });
    if (isUpdated)
    {
        var animalType = typeAnimalService.GetAnimalTypeById(typeId);
        return TypedResults.Ok(animalType);
    }
    return Results.NoContent();
}).WithParameterValidation();
groupWithValidationId.MapDelete("/animals/types/{typeId:long}",
    (long typeId, [FromServices] ITypeAnimalService typeAnimalService) =>
    {
        var isDeleted = typeAnimalService.DeleteAnimalType(typeId);
        if (isDeleted)
        {
            return Results.Ok();
        }

        return Results.NoContent();
    });

// ===========================================
// location endpoints
// ===========================================
groupWithValidationId.MapGet("/locations/{pointId:long}", (long pointId,
    [FromServices] ILocationService locationService) =>
{
    var location = locationService.GetLocationPointById(pointId);
    return Results.Ok(location);
});
app.MapPost("/locations", ([FromBody] LocationCreateUpdateDto dto, [FromServices] ILocationService locationService) =>
{
    var locationId = locationService.AddLocationPoint(new LocationPoint()
        { Longitude = dto.Longitude, Latitude = dto.Latitude });
    var location = locationService.GetLocationPointById(locationId);
    return Results.Created($"/locations/{locationId}", location);
}).RequireAuthorization().WithParameterValidation();
groupWithValidationId.MapPut("/location/{pointId:long}", (long pointId, [FromBody] LocationCreateUpdateDto dto,
    [FromServices] ILocationService locationService) =>
{
    var isUpdated = locationService.UpdateLocationPoint(new LocationPoint()
        { LocationPointId = pointId, Longitude = dto.Longitude, Latitude = dto.Latitude });
    if (isUpdated)
    {
        var location = locationService.GetLocationPointById(pointId);
        return TypedResults.Ok(location);
    }
    return Results.NoContent();
}).WithParameterValidation();
groupWithValidationId.MapDelete("/locations/{pointId:long}",
    (long pointId, [FromServices] ILocationService locationService) =>
    {
        var isDeleted = locationService.DeletedLocationPoint(pointId);
        if (isDeleted)
        {
            return Results.Ok();
        }

        return Results.NoContent();
    });


// ===========================================
// visited locations endpoints
// ===========================================
groupWithValidationSearchParams.MapGet("/animals/{animalId:long}/locations", ([AsParameters] VisitedLocationSearchDto visitedLocationSearchDto,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    DateTime? startDateTimeParsed = visitedLocationSearchDto.StartDateTime is not null
        ? DateTime.ParseExact(visitedLocationSearchDto.StartDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture)
        : null;
    DateTime? endDateTimeParsed = visitedLocationSearchDto.EndDateTime is not null
        ? DateTime.ParseExact(visitedLocationSearchDto.EndDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture)
        : null;
    var searchOptions = new LocationVisitedSortFilterOptions()
    {
        PageNum = (visitedLocationSearchDto.From / visitedLocationSearchDto.Size) ?? 1,
        PageSize = visitedLocationSearchDto.Size ?? SortFilterPageBaseOptions.DefaultPageSize,
        FilterValue = string.Join(',', visitedLocationSearchDto.StartDateTime is not null 
            ? startDateTimeParsed 
            : DateTime.MinValue, 
            visitedLocationSearchDto.EndDateTime is not null 
            ? endDateTimeParsed
            : DateTime.MaxValue)
    };
    var result = visitedLocationService.GetAnimalVisitedLocationsById(visitedLocationSearchDto.AnimalId, searchOptions)
        .Skip((visitedLocationSearchDto.From % visitedLocationSearchDto.Size) ?? 0).Select(vl => vl.ToDto()).ToList();
    return TypedResults.Ok(result);
}).AddEndpointFilterFactory(SearchParamsValidationFilterFactory.ValidateSearchDateParamsFactory)
.AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory);
groupWithValidationId.MapGet("/animals/{animalId:long}/locations/{visitedLocationId}", (long animalId,
    long visitedLocationId,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    var visitedLocation = visitedLocationService.GetAnimalVisitedLocationById(animalId, visitedLocationId).ToDto();
    return TypedResults.Ok(visitedLocation);
});
groupWithValidationId.MapPost("/animals/{animalId:long}/locations/{pointId:long}", (long animalId, long pointId,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    var addedVisitedLocation = visitedLocationService.AddVisitedLocationToAnimal(animalId, pointId).ToDto();
    return Results.Created($"/animals/{animalId}/locations/{addedVisitedLocation.VisitedLocationId}",
        addedVisitedLocation);
});
groupWithValidationId.MapPut("/animals/{animalId:long}/locations",
    (long animalId, UpdateVisitedLocationDto visitedLocationDto,
        [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
    {
        var isUpdated = visitedLocationService.UpdateVisitedLocationToAnimal(animalId, visitedLocationDto);
        if (isUpdated)
        {
            var visitedLocation = visitedLocationService
                .GetAnimalVisitedLocationById(animalId, visitedLocationDto.VisitedLocationId).ToDto();
            return TypedResults.Ok(visitedLocation);
        }

        return Results.NoContent();
    }).AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory);
groupWithValidationId.MapDelete("/animals/{animalId:long}/locations/{visitedLocationId:long}", (long animalId,
    long visitedLocationId,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    var isDeleted = visitedLocationService.DeleteVisitedLocationFromAnimalData(animalId, visitedLocationId);
    if (isDeleted)
    {
        return Results.Ok();
    }

    return Results.NoContent();
});
app.Run();

public partial class Program {}