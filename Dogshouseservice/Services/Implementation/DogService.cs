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
        private const string AllDogsCacheKey = "AllDogsCache";

        public DogService(ApplicationDbContext context, IMemoryCache cache, DogQueryValidator validator, ILogger<DogService> logger)
        {
            _context = context;
            _cache = cache;
            _validator = validator;
            _logger = logger;
        }

        public async Task<List<DogModel>> GetDogsAsync(DogSortingAttribute attribute, string order, int pageNumber, int pageSize)
        {
            var allDogs = await GetOrSetAllDogsCache();

            var sortedDogs = ApplySorting(allDogs, attribute, order);

            if (pageNumber > 0 && pageSize > 0)
            {
                var paginatedDogs = ApplyPagination(sortedDogs, pageNumber, pageSize, attribute, order);
                return paginatedDogs;
            }

            return sortedDogs;
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

            InvalidateCache();
            return string.Empty;
        }

        private async Task<List<DogModel>> GetOrSetAllDogsCache()
        {
            if (_cache.TryGetValue(AllDogsCacheKey, out List<DogModel>? allDogs) && allDogs != null)
            {
                _logger.LogInformation("Cache hit for all dogs list.");
                return allDogs;
            }

            _logger.LogInformation("Cache miss for all dogs list. Fetching from database.");
            allDogs = await _context.Dogs.ToListAsync();
            _cache.Set(AllDogsCacheKey, allDogs, TimeSpan.FromMinutes(5));
            return allDogs ?? [];
        }

        private List<DogModel> ApplySorting(List<DogModel> dogs, DogSortingAttribute attribute, string order)
        {
            _logger.LogInformation($"Sorting cached dogs by {attribute} in {order} order.");
            return attribute switch
            {
                DogSortingAttribute.Weight => order == SortingConstants.Descending ? dogs.OrderByDescending(d => d.Weight).ToList() : dogs.OrderBy(d => d.Weight).ToList(),
                DogSortingAttribute.TailLength => order == SortingConstants.Descending ? dogs.OrderByDescending(d => d.TailLength).ToList() : dogs.OrderBy(d => d.TailLength).ToList(),
                _ => order == SortingConstants.Descending ? dogs.OrderByDescending(d => d.Name).ToList() : dogs.OrderBy(d => d.Name).ToList(),
            };
        }

        private List<DogModel> ApplyPagination(List<DogModel> sortedDogs, int pageNumber, int pageSize, DogSortingAttribute attribute, string order)
        {
            var cacheKey = $"PaginatedDogs_{attribute}_{order}_{pageNumber}_{pageSize}";

            if (_cache.TryGetValue(cacheKey, out List<DogModel>? paginatedDogs) && paginatedDogs != null)
            {
                _logger.LogInformation("Cache hit for paginated dog list.");
                return paginatedDogs;
            }

            _logger.LogInformation("Applying pagination to sorted dogs and caching result.");
            paginatedDogs = sortedDogs.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            _cache.Set(cacheKey, paginatedDogs, TimeSpan.FromMinutes(5));

            return paginatedDogs ?? [];
        }

        private void InvalidateCache()
        {
            _cache.Remove(AllDogsCacheKey);
            _logger.LogInformation("Cache cleared for all dogs list after adding a new dog.");
        }

        public string Ping()
        {
            return ResponseMessages.VersionMessage;
        }
    }
}