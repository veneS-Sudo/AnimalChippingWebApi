using System.Globalization;
using System.Security.Claims;
using AnimalChippingWebApi.Authentication;
using AnimalChippingWebApi.BackgroundServices;
using AnimalChippingWebApi.Middlewares;
using AnimalChippingWebApi.Models.Account;
using AnimalChippingWebApi.Models.Animal;
using AnimalChippingWebApi.Models.Location;
using AnimalChippingWebApi.Models.VisitedLocation;
using AnimalChippingWebApi.ValidationFilters;
using AnimalChippingWebApi.ValidationFilters.AnimalFilters;
using AnimalChippingWebApi.ValidationFilters.IdFilters;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Serilog;
using ServiceLayer;
using ServiceLayer.Admin.AccountServices;
using ServiceLayer.Admin.AccountServices.SearchPagination;
using ServiceLayer.Admin.AnimalServices;
using ServiceLayer.Admin.AnimalServices.AnimalTypeAtAnimal;
using ServiceLayer.Admin.AnimalServices.SearchPagination;
using ServiceLayer.Admin.Generic.SortFilter;
using ServiceLayer.Admin.LocationServices;
using ServiceLayer.Admin.TypeAnimalServices;
using ServiceLayer.Admin.VisitedLocationServices;
using ServiceLayer.Admin.VisitedLocationServices.SearchPagination;
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
builder.Logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File(Path.Combine("logs", "log.txt"), rollingInterval: RollingInterval.Day).CreateLogger());
builder.Services.AddProblemDetails();
builder.Services.AddCoreServices();
builder.Services.AddHostedService<DbMigrationBackgroundService>();

builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, UserAuthenticationHandler>("Basic", null);
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();
app.UseAntiforgery();
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

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<LoggingInvocation>();

var groupWithValidationId = app.MapGroup("/")
    .AddEndpointFilterFactory(IdValidationFilterFactory.ValidateParameterIdFactory);
var groupWithValidationIdRequireAuthorization = groupWithValidationId.RequireAuthorization();
var groupAnonymousEndpoints = groupWithValidationId.AddEndpointFilter(async (context, next) =>
{
    var authenticationResult = await context.HttpContext.AuthenticateAsync();
    if (authenticationResult.Failure != null)
    {
        return Results.Unauthorized();
    }
    
    return await next(context);
});
var groupAnonymousWithValidationSearchParams =
    groupAnonymousEndpoints.AddEndpointFilterFactory(SearchParamsValidationFilterFactory
        .ValidateSearchPaginationParamsFactory);

// ===========================================
// Account endpoints
// ===========================================
groupAnonymousEndpoints.MapGet("/accounts/{accountId:int}", (int accountId, 
    [FromServices] IAccountService accountService) =>
    {
    var account = accountService.GetAccountById(accountId);
    return TypedResults.Ok(account);
    }).AllowAnonymous()
    .WithTags("Account")
.Produces<AccountDto>()
.ProducesProblem(404)
.ProducesProblem(401)
.ProducesValidationProblem();

groupAnonymousWithValidationSearchParams.MapGet("/accounts/search", ([AsParameters] AccountSearchDto accountSearchDto,
    [FromServices] IAccountService accountService) =>
{
    var searchOptions = new AccountSortFilterPageOptions()
    {
        PageNum = (accountSearchDto.From / accountSearchDto.Size + 1) ?? 1,
        PageSize = accountSearchDto.Size ?? SortFilterPageBaseOptions.DefaultPageSize,
        Skip = accountSearchDto.From ?? 0,
        filterValues = new AccountSearchFilterValues()
        {
            FirstName = accountSearchDto.FirstName,
            LastName = accountSearchDto.LastName,
            Email = accountSearchDto.Email
        }
    };
    var result = accountService.GetAccounts(searchOptions).ToList();
    return TypedResults.Ok(result);
}).AllowAnonymous().WithParameterValidation()
.WithTags("Account")
.Produces<List<AccountDto>>()
.ProducesProblem(401)
.ProducesValidationProblem();

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
    }).WithParameterValidation()
    .WithTags("Account")
    .Produces<AccountDto>(201)
    .ProducesProblem(403)
    .ProducesProblem(409)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPut("/accounts/{accountId:int}", (int accountId, 
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
.AddEndpointFilter<OwnedAccountValidationFilter>()
.WithTags("Account")
.Produces<AccountDto>()
.ProducesProblem(401)
.ProducesProblem(403)
.ProducesProblem(409)
.ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapDelete("/accounts/{accountId:int}", (int accountId, 
    [FromServices] IAccountService accountService) =>
{
    var isDeleted = accountService.DeleteAccountById(accountId);
    if (isDeleted)
        return Results.Ok();
    return Results.NoContent();
}).AddEndpointFilter<OwnedAccountValidationFilter>()
.WithTags("Account")
.Produces(200)
.ProducesProblem(401)
.ProducesProblem(403)
.ProducesValidationProblem();



// ===========================================
// animal endpoints
// ===========================================
groupAnonymousEndpoints.MapGet("/animals/{animalId:long}", (long animalId,
    [FromServices] IAnimalService animalService) =>
{
    var animal = animalService.GetAnimalById(animalId).ToDto();
    return TypedResults.Ok(animal);
}).AllowAnonymous().WithTags("Animal")
.Produces<AnimalDto>()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesValidationProblem();

groupAnonymousWithValidationSearchParams.MapGet("/animals/search", ([AsParameters] AnimalSearchDto animalSearchDto,
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
            ? DateTime.ParseExact(animalSearchDto.StartDateTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
            : null;
        DateTime? endDateTimeParsed = animalSearchDto.EndDateTime is not null
            ? DateTime.ParseExact(animalSearchDto.EndDateTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
            : null;
        var searchOptions = new AnimalSortFilterPageOptions()
        {
            PageNum = (animalSearchDto.From / animalSearchDto.Size + 1) ?? 1,
            PageSize = animalSearchDto.Size ?? SortFilterPageBaseOptions.DefaultPageSize,
            Skip = animalSearchDto.From ?? 0,
            FilterValues = new AnimalSearchFilterValues()
            {
                ChipperId = animalSearchDto.ChipperId,
                ChippingLocationId = animalSearchDto.ChippingLocationId,
                Gender = animalSearchDto.Gender is not null ? genderParsed : null,
                LiveStatus = animalSearchDto.LifeStatus is not null ? lifeStatusParsed : null,
                StartDate = animalSearchDto.StartDateTime is not null ? startDateTimeParsed : null,
                EndDate = animalSearchDto.EndDateTime is not null ? endDateTimeParsed : null
            }
        };
        var result = animalService.GetAnimals(searchOptions).Select(a => a.ToDto()).ToList();
        return TypedResults.Ok(result);
    }).AllowAnonymous().WithParameterValidation()
    .AddEndpointFilterFactory(SearchParamsValidationFilterFactory.ValidateSearchDateParamsFactory)
    .AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory)
    .WithTags("Animal")
    .Produces<List<AnimalDto>>()
    .ProducesProblem(401)
    .ProducesValidationProblem();

app.MapPost("/animals",
        ([FromBody] CreateAnimalDto dto, [FromServices] IAnimalService animalService) =>
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
    .AddEndpointFilter<AnimalTypesValidatorFilter>()
    .WithTags("Animal")
    .Produces<AnimalDto>(201)
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesProblem(409)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPut("/animals/{animalId:long}", ([FromBody] UpdateAnimalDto dto,
        long animalId,
        [FromServices] IAnimalService animalService) =>
    {
        if (!Enum.TryParse<LiveStatusType>(dto.LiveStatus, true, out _))
            return Results.Problem("Available life status: " + string.Join(", ", Enum.GetNames<LiveStatusType>()),
                statusCode: 400);
        if (!Enum.TryParse<GenderType>(dto.Gender, true, out var _))
            return Results.Problem("Available gender: " + string.Join(", ", Enum.GetNames<GenderType>()),
                statusCode: 400);
        var isUpdated = animalService.UpdateDataAnimal(animalId, dto.ToUpdateDataAnimalDto());
        var animal = animalService.GetAnimalById(animalId).ToDto();
        return TypedResults.Ok(animal);
    }).WithParameterValidation()
.AddEndpointFilter<BaseAnimalIndicatorFilter>()
.AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory)
    .WithTags("Animal")
    .Produces<AnimalDto>()
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapDelete("/animals/{animalId:long}",
    (long animalId, [FromServices] IAnimalService animalService) =>
    {
        var isDeleted = animalService.SoftDelete(animalId);
        if (isDeleted)
        {
            return Results.Ok();
        }

        return Results.NoContent();
    }).WithTags("Animal")
    .Produces(200)
    .ProducesProblem(403)
    .ProducesProblem(404)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPost("/animals/{animalId:long}/types/{typeId:long}",
    (long animalId, long typeId, [FromServices] IChangeAnimalTypeAtAnimalService changeAnimalTypeService,
        [FromServices] IAnimalService animalService) =>
    {
        var createdAnimalId = changeAnimalTypeService.AddAnimalTypeAtAnimal(animalId, typeId);
        var animal = animalService.GetAnimalById(createdAnimalId).ToDto();
        return Results.Created($"/animals/{createdAnimalId}", animal);
    }).WithTags("Animal")
    .Produces<AnimalDto>(201)
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesProblem(409)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPut("/animals/{animalId:long}/types",
    (long animalId, [FromBody] UpdateAnimalTypeInAnimalDataDto dto,
        [FromServices] IChangeAnimalTypeAtAnimalService changeAnimalTypeService,
        [FromServices] IAnimalService animalService) =>
    {
        var updatedAnimalId = changeAnimalTypeService.UpdateAnimalTypeAtAnimal(animalId, dto);
        var updatedAnimal = animalService.GetAnimalById(updatedAnimalId).ToDto();
        return TypedResults.Ok(updatedAnimal);
    }).WithParameterValidation().
    AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory)
    .WithTags("Animal")
    .Produces<AnimalDto>()
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesProblem(409)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapDelete("/animals/{animalId:long}/types/{typeId:long}",
    (long animalId, long typeId, [FromServices] IChangeAnimalTypeAtAnimalService changeAnimalTypeService,
        [FromServices] IAnimalService animalService) =>
    {
        var changedAnimalId = changeAnimalTypeService.DeleteAnimalTypeAtAnimal(animalId, typeId);
        var animal = animalService.GetAnimalById(changedAnimalId).ToDto();
        return TypedResults.Ok(animal);
    }).WithTags("Animal")
    .Produces(200)
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesValidationProblem();



// ===========================================
// animal type endpoints
// ===========================================
groupAnonymousEndpoints.MapGet("/animals/types/{typeId:long}", (long typeId,
    [FromServices] ITypeAnimalService typeAnimalService) =>
{
    var typeAnimal = typeAnimalService.GetAnimalTypeById(typeId);
    return Results.Ok(typeAnimal);
}).AllowAnonymous().WithTags("Animal type")
.Produces<AnimalTypeDto>()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesValidationProblem();

app.MapPost("/animals/types",
    ([FromBody] CreateUpdateAnimalTypeDto dto, [FromServices] ITypeAnimalService typeAnimalService) =>
    {
        var typeId = typeAnimalService.AddAnimalType(dto);
        var animalType = typeAnimalService.GetAnimalTypeById(typeId);
        return Results.Created($"/animals/types/{typeId}", animalType);
    }).RequireAuthorization().WithParameterValidation()
    .WithTags("Animal type")
    .Produces<AnimalTypeDto>(201)
    .ProducesProblem(401)
    .ProducesProblem(409)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPut("/animals/types/{typeId:long}", (long typeId, [FromBody] CreateUpdateAnimalTypeDto dto,
    [FromServices] ITypeAnimalService typeAnimalService) =>
{
    var isUpdated = typeAnimalService.UpdateAnimalType(new AnimalType { AnimalTypeId = typeId, Type = dto.Type });
    if (isUpdated)
    {
        var animalType = typeAnimalService.GetAnimalTypeById(typeId);
        return TypedResults.Ok(animalType);
    }
    return Results.NoContent();
}).WithParameterValidation()
.WithTags("Animal type")
.Produces<AnimalTypeDto>()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesProblem(409)
.ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapDelete("/animals/types/{typeId:long}",
    (long typeId, [FromServices] ITypeAnimalService typeAnimalService) =>
    {
        var isDeleted = typeAnimalService.DeleteAnimalType(typeId);
        if (isDeleted)
        {
            return Results.Ok();
        }

        return Results.NoContent();
    }).WithTags("Animal type")
    .Produces(200)
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesValidationProblem();



// ===========================================
// location endpoints
// ===========================================
groupAnonymousEndpoints.MapGet("/locations/{pointId:long}", (long pointId,
    [FromServices] ILocationService locationService) =>
{
    var location = locationService.GetLocationPointById(pointId);
    return Results.Ok(location);
}).AllowAnonymous().WithTags("Location")
.Produces<LocationPointDto>()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesValidationProblem();

app.MapPost("/locations", ([FromBody] LocationCreateUpdateDto dto, [FromServices] ILocationService locationService) =>
{
    var locationId = locationService.AddLocationPoint(new LocationPoint()
        { Longitude = dto.Longitude, Latitude = dto.Latitude });
    var location = locationService.GetLocationPointById(locationId);
    return Results.Created($"/locations/{locationId}", location);
}).RequireAuthorization().WithParameterValidation()
.WithTags("Location")
.Produces<LocationPointDto>(201)
.ProducesProblem(401)
.ProducesProblem(409)
.ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPut("/locations/{pointId:long}", (long pointId, [FromBody] LocationCreateUpdateDto dto,
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
}).WithParameterValidation()
.WithTags("Location")
.Produces<LocationPointDto>()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesProblem(409)
.ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapDelete("/locations/{pointId:long}",
    (long pointId, [FromServices] ILocationService locationService) =>
    {
        var isDeleted = locationService.DeletedLocationPoint(pointId);
        if (isDeleted)
        {
            return Results.Ok();
        }

        return Results.NoContent();
    }).WithTags("Location")
    .Produces(200)
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesValidationProblem();



// ===========================================
// visited locations endpoints
// ===========================================
groupAnonymousWithValidationSearchParams.MapGet("/animals/{animalId:long}/locations", ([AsParameters] VisitedLocationSearchDto visitedLocationSearchDto,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    DateTime? startDateTimeParsed = visitedLocationSearchDto.StartDateTime is not null
        ? DateTime.ParseExact(visitedLocationSearchDto.StartDateTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
        : null;
    DateTime? endDateTimeParsed = visitedLocationSearchDto.EndDateTime is not null
        ? DateTime.ParseExact(visitedLocationSearchDto.EndDateTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
        : null;
    var searchOptions = new LocationVisitedSortFilterOptions()
    {
        PageNum = (visitedLocationSearchDto.From / visitedLocationSearchDto.Size + 1) ?? 1,
        PageSize = visitedLocationSearchDto.Size ?? SortFilterPageBaseOptions.DefaultPageSize,
        Skip = visitedLocationSearchDto.From ?? 0,
        FilterValue = string.Join(',', visitedLocationSearchDto.StartDateTime is not null 
            ? startDateTimeParsed 
            : DateTime.MinValue, 
            visitedLocationSearchDto.EndDateTime is not null 
            ? endDateTimeParsed
            : DateTime.MaxValue)
    };
    var result = visitedLocationService.GetAnimalVisitedLocationsById(visitedLocationSearchDto.AnimalId, searchOptions)
        .Select(vl => vl.ToDto()).ToList();
    return TypedResults.Ok(result);
}).AllowAnonymous()
    .AddEndpointFilterFactory(SearchParamsValidationFilterFactory.ValidateSearchDateParamsFactory)
.AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory)
.WithTags("Visited location")
.Produces<List<VisitedLocationDto>>()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapGet("/animals/{animalId:long}/locations/{visitedLocationId}", (long animalId,
    long visitedLocationId,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    var visitedLocation = visitedLocationService.GetAnimalVisitedLocationById(animalId, visitedLocationId).ToDto();
    return TypedResults.Ok(visitedLocation);
}).WithTags("Visited location")
.Produces<VisitedLocationDto>()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPost("/animals/{animalId:long}/locations/{pointId:long}", (long animalId, long pointId,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    var addedVisitedLocation = visitedLocationService.AddVisitedLocationToAnimal(animalId, pointId).ToDto();
    return Results.Created($"/animals/{animalId}/locations/{addedVisitedLocation.VisitedLocationId}",
        addedVisitedLocation);
}).WithTags("Visited location")
.Produces<VisitedLocationDto>(201)
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapPut("/animals/{animalId:long}/locations",
    (long animalId, [FromBody] UpdateVisitedLocationDto visitedLocationDto,
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
    }).AddEndpointFilterFactory(IdValidationFilterFactory.ValidatePropertyIdFactory)
    .WithTags("Visited location")
    .Produces<VisitedLocationDto>()
    .ProducesProblem(401)
    .ProducesProblem(404)
    .ProducesValidationProblem();

groupWithValidationIdRequireAuthorization.MapDelete("/animals/{animalId:long}/locations/{visitedLocationId:long}", (long animalId,
    long visitedLocationId,
    [FromServices] IVisitedLocationAnimalService visitedLocationService) =>
{
    var isDeleted = visitedLocationService.DeleteVisitedLocationFromAnimalData(animalId, visitedLocationId);
    if (isDeleted)
    {
        return Results.Ok();
    }

    return Results.NoContent();
}).WithTags("Visited location")
.Produces(200)
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesValidationProblem();



app.Run();

public partial class Program {}