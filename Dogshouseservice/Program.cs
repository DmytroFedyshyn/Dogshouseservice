using Microsoft.EntityFrameworkCore;
using Dogshouseservice;
using Dogshouseservice.Services.Implementation;
using Dogshouseservice.Services.Interfaces;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Dogshouseservice.Extensions;
using Dogshouseservice.Middleware;
using Dogshouseservice.Helpers;

var builder = WebApplication.CreateBuilder(args);


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddRateLimitingServices(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DogModelValidator>();

builder.Services.AddScoped<IDogService, DogService>();
builder.Services.AddScoped<DogModelValidator>();
builder.Services.AddScoped<DogQueryValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();