using Dogshouseservice.Constants;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dogshouseservice.Services.Implementation
{
    public class DogService : IDogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DogService> _logger;

        public DogService(ApplicationDbContext context, ILogger<DogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<string> PingAsync()
        {
            _logger.LogInformation("Returning version message for Ping.");
            return Task.FromResult(ResponseMessages.VersionMessage);
        }

        public async Task<List<Dog>> GetDogsAsync(string attribute, string order, int pageNumber, int pageSize)
        {
            _logger.LogInformation("Fetching dogs from database with sorting attribute: {Attribute} and order: {Order}.", attribute, order);

            var dogsQuery = _context.Dogs.AsQueryable();

            // Sorting logic
            dogsQuery = attribute.ToLower() switch
            {
                "weight" => order == "desc" ? dogsQuery.OrderByDescending(d => d.Weight) : dogsQuery.OrderBy(d => d.Weight),
                "tail_length" => order == "desc" ? dogsQuery.OrderByDescending(d => d.TailLength) : dogsQuery.OrderBy(d => d.TailLength),
                _ => order == "desc" ? dogsQuery.OrderByDescending(d => d.Name) : dogsQuery.OrderBy(d => d.Name),
            };

            // Pagination logic
            var result = await dogsQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            _logger.LogInformation("Fetched {Count} dogs from database.", result.Count);
            return result;
        }

        public async Task<string> CreateDogAsync(Dog newDog)
        {
            _logger.LogInformation("Checking if dog with name {DogName} exists.", newDog.Name);

            if (_context.Dogs.Any(d => d.Name == newDog.Name))
            {
                _logger.LogWarning("Dog with name {DogName} already exists in the database.", newDog.Name);
                return ResponseMessages.DogExists;
            }

            if (newDog.TailLength < 0 || newDog.Weight <= 0)
            {
                _logger.LogWarning("Invalid data for dog: {DogName}. TailLength: {TailLength}, Weight: {Weight}", newDog.Name, newDog.TailLength, newDog.Weight);
                return ResponseMessages.InvalidDogData;
            }

            _context.Dogs.Add(newDog);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Dog {DogName} added to the database successfully.", newDog.Name);
            return string.Empty; // Empty string indicates successful creation
        }
    }
}
