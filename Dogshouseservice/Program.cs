using Microsoft.EntityFrameworkCore;
using Dogshouseservice;
using Dogshouseservice.Services.Implementation;
using Dogshouseservice.Services.Interfaces;
using AspNetCoreRateLimit;
using Dogshouseservice.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimitingServices(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDogService, DogService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Dogshouseservice", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
