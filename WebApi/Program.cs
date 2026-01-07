using System.Globalization;
using AnimalChippingWebApi.Authentication;
using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using AnimalChippingWebApi.Middlewares;
using AnimalChippingWebApi.Models;
using AnimalChippingWebApi.ValidationFilters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using ServiceLayer;
using ServiceLayer.Admin;
using ServiceLayer.Admin.AccountServices;
using ServiceLayer.Admin.Generic;
using ServiceLayer.Admin.LocationServices;
using ServiceLayer.Admin.TypeAnimalServices;
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
app.UseMiddleware<ExceptionUniqueEmailHandlerMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var groupWithValidationId = app.MapGroup("/")
    .AddEndpointFilter<IdValidationFilter>()
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
});
app.MapPost("/registration",
    ([FromBody] UserRegistrationDto dto, HttpContext context,
        [FromServices] IRegistrationUserService registrationService) => 
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            return Results.Problem("You are already logged in", statusCode: 403);
        }
        var accountId = registrationService.RegistrationUser(dto);
        return TypedResults.Created($"/accounts/{accountId}");
    }).WithParameterValidation();


// ===========================================
// animal endpoints
// ===========================================
groupWithValidationId.MapGet("/animals/{animalId:long}", (long animalId, 
    [FromServices] IAnimalService animalService) =>
{
    var animal = animalService.GetAnimalById(animalId);
    return TypedResults.Ok(animal);
});
groupWithValidationSearchParams.MapGet("/animals/search", ([AsParameters] AnimalSearchDto animalSearchDto,
    [FromServices] IAnimalService animalService) =>
{
    if (!Enum.TryParse<LiveStatusType>(animalSearchDto.LifeStatus, true, out var lifeStatusParsed)
        && animalSearchDto.LifeStatus is not null )
        return Results.Problem("Available life status: " + string.Join(", ", Enum.GetNames<LiveStatusType>()), statusCode: 400);
    if (!Enum.TryParse<GenderType>(animalSearchDto.Gender, true,out var genderParsed) 
        && animalSearchDto.Gender is not null)
        return Results.Problem("Available gender: " + string.Join(", ", Enum.GetNames<GenderType>()), statusCode: 400);
    DateTime? startDateTimeParsed = animalSearchDto.StartDateTime is not null
        ? DateTime.ParseExact(animalSearchDto.StartDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
    DateTime? endDateTimeParsed = animalSearchDto.EndDateTime is not null
        ? DateTime.ParseExact(animalSearchDto.EndDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
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
        .Skip((animalSearchDto.From % animalSearchDto.Size) ?? 0).ToList();
    return TypedResults.Ok(result);
}).AddEndpointFilterFactory(SearchParamsValidationFilterFactory.ValidateSearchDateParamsFactory)
.AddEndpointFilterFactory(IdValidationFilterFactory.ValidateIdFactory);

// ===========================================
// animal type endpoints
// ===========================================
groupWithValidationId.MapGet("/animals/types/{typeId:long}", (long typeId,
    [FromServices] ITypeAnimalService typeAnimalService) =>
{
    var typeAnimal = typeAnimalService.GetAnimalTypeById(typeId);
    return Results.Ok(typeAnimal);
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
        .Skip((visitedLocationSearchDto.From % visitedLocationSearchDto.Size) ?? 0).ToList();
    return TypedResults.Ok(result);
}).AddEndpointFilterFactory(SearchParamsValidationFilterFactory.ValidateSearchDateParamsFactory)
.AddEndpointFilterFactory(IdValidationFilterFactory.ValidateIdFactory);

app.Run();
