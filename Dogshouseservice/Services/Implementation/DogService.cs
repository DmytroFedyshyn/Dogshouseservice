using Dogshouseservice.Constants;
using Dogshouseservice.Helpers;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Dogshouseservice.Services.Implementation
{
    public class DogService : IDogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly DogQueryValidator _validator;
        private readonly ILogger<DogService> _logger;

        public DogService(ApplicationDbContext context, IMemoryCache cache, DogQueryValidator validator, ILogger<DogService> logger)
        {
            _context = context;
            _cache = cache;
            _validator = validator;
            _logger = logger;
        }

        public async Task<List<DogModel>> GetDogsAsync(DogSortingAttribute attribute, string order, int pageNumber, int pageSize)
        {
            const string allDogsCacheKey = "AllDogsCache";

            if (!_cache.TryGetValue(allDogsCacheKey, out List<DogModel>? allDogs))
            {
                _logger.LogInformation("Cache miss for all dogs list. Fetching from database.");
                allDogs = await _context.Dogs.ToListAsync();
                _cache.Set(allDogsCacheKey, allDogs, TimeSpan.FromMinutes(5));
            }
            else
            {
                _logger.LogInformation("Cache hit for all dogs list.");
            }

            var sortedDogs = SortDogs(allDogs, attribute, order);

            if (pageNumber > 0 && pageSize > 0)
            {
                var cacheKey = $"GetDogsAsync_{attribute}_{order}_{pageNumber}_{pageSize}";
                if (!_cache.TryGetValue(cacheKey, out List<DogModel>? paginatedDogs))
                {
                    _logger.LogInformation("Cache miss for paginated dog list. Applying pagination.");
                    paginatedDogs = sortedDogs.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                    _cache.Set(cacheKey, paginatedDogs, TimeSpan.FromMinutes(5));
                }
                else
                {
                    _logger.LogInformation("Cache hit for paginated dog list.");
                }
                return paginatedDogs;
            }

            return sortedDogs;
        }

        private List<DogModel> SortDogs(List<DogModel> dogs, DogSortingAttribute attribute, string order)
        {
            _logger.LogInformation($"Sorting dogs by {attribute} in {order} order.");

            return attribute switch
            {
                DogSortingAttribute.Weight => order == SortingConstants.Descending ? dogs.OrderByDescending(d => d.Weight).ToList() : dogs.OrderBy(d => d.Weight).ToList(),
                DogSortingAttribute.TailLength => order == SortingConstants.Descending ? dogs.OrderByDescending(d => d.TailLength).ToList() : dogs.OrderBy(d => d.TailLength).ToList(),
                _ => order == SortingConstants.Descending ? dogs.OrderByDescending(d => d.Name).ToList() : dogs.OrderBy(d => d.Name).ToList(),
            };
        }

        public async Task<string> CreateDogAsync(DogModel newDog)
        {
            _logger.LogInformation("Creating a new dog entry.");

            if (_context.Dogs.Any(d => d.Name == newDog.Name))
            {
                _logger.LogWarning("Duplicate dog entry detected: a dog with the same name already exists.");
                return ResponseMessages.DogExists;
            }

            if (newDog.TailLength < 0 || newDog.Weight <= 0)
            {
                _logger.LogWarning("Invalid dog data: tail length or weight is incorrect.");
                return ResponseMessages.InvalidDogData;
            }

            _context.Dogs.Add(newDog);
            await _context.SaveChangesAsync();
            _logger.LogInformation("New dog entry created successfully.");

            const string allDogsCacheKey = "AllDogsCache";
            _cache.Remove(allDogsCacheKey);

            var allDogs = await _context.Dogs.ToListAsync();
            _cache.Set(allDogsCacheKey, allDogs, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Cache updated for all dogs list after adding a new dog.");

            return string.Empty;
        }


        public string Ping()
        {
            return ResponseMessages.VersionMessage;
        }
    }
}