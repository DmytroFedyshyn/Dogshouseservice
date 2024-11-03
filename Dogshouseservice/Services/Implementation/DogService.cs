using Dogshouseservice.Constants;
using Dogshouseservice.Helpers;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Dogshouseservice.Services.Implementation
{
    public class DogService : IDogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IValidator<DogModel> _dogValidator;
        private readonly ILogger<DogService> _logger;

        public DogService(ApplicationDbContext context, IMemoryCache cache, IValidator<DogModel> dogValidator, ILogger<DogService> logger)
        {
            _context = context;
            _cache = cache;
            _dogValidator = dogValidator;
            _logger = logger;
        }

        public string Ping()
        {
            _logger.LogInformation("Ping endpoint was called.");
            return ResponseMessages.VersionMessage;
        }

        public async Task<List<DogModel>> GetDogsAsync(DogSortingAttribute attribute, string order, int pageNumber, int pageSize)
        {
            string cacheKey = $"GetDogsAsync_{attribute}_{order}_{pageNumber}_{pageSize}";
            _logger.LogInformation("Fetching dogs with cache key: {CacheKey}", cacheKey);

            if (_cache.TryGetValue(cacheKey, out List<DogModel>? cachedDogs))
            {
                _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
                return cachedDogs ?? new List<DogModel>();
            }

            _logger.LogInformation("Cache miss for key: {CacheKey}. Retrieving data from the database.", cacheKey);
            var dogs = await _context.Dogs.ToListAsync();

            var comparer = new DogModelComparer(attribute, order);
            dogs = dogs.OrderBy(d => d, comparer).ToList();

            dogs = dogs.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            _logger.LogInformation("Caching result for key: {CacheKey}", cacheKey);
            _cache.Set(cacheKey, dogs, TimeSpan.FromMinutes(5));

            return dogs;
        }

        public async Task<string> CreateDogAsync(DogModel newDog)
        {
            _logger.LogInformation("Creating a new dog entry.");

            var validationResult = await _dogValidator.ValidateAsync(newDog);
            if (!validationResult.IsValid)
            {
                var errorMessage = validationResult.Errors.First().ErrorMessage;
                _logger.LogWarning("Validation failed for new dog: {ErrorMessage}", errorMessage);
                return errorMessage;
            }

            if (_context.Dogs.Any(d => d.Name == newDog.Name))
            {
                _logger.LogWarning("A dog with the name {DogName} already exists.", newDog.Name);
                return ResponseMessages.DogExists;
            }

            _context.Dogs.Add(newDog);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Dog with name {DogName} added successfully.", newDog.Name);

            _logger.LogInformation("Clearing cache after creating a new dog entry.");
            _cache.Remove("GetDogsAsync_cache_key");

            return string.Empty;
        }
    }
}
