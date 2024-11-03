﻿using Dogshouseservice.Constants;
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

        private readonly HashSet<string> _cacheKeys = new();

        public DogService(ApplicationDbContext context, IMemoryCache cache, DogQueryValidator validator, ILogger<DogService> logger)
        {
            _context = context;
            _cache = cache;
            _validator = validator;
            _logger = logger;
        }

        public async Task<List<DogModel>> GetDogsAsync(DogSortingAttribute attribute, string order, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                _logger.LogWarning("Invalid pagination parameters.");
                return new List<DogModel>();
            }

            var baseCacheKey = $"GetDogs_{pageNumber}_{pageSize}";

            if (_cache.TryGetValue(baseCacheKey, out List<DogModel>? cachedDogs) && cachedDogs != null)
            {
                _logger.LogInformation("Cache hit for paginated dog list. Sorting data in memory.");
                return ApplySorting(cachedDogs, attribute, order);
            }

            _logger.LogInformation("Cache miss for paginated dog list. Fetching from database.");
            var query = _context.Dogs.AsQueryable();
            var paginatedDogs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _cache.Set(baseCacheKey, paginatedDogs, TimeSpan.FromMinutes(5));
            _cacheKeys.Add(baseCacheKey);

            return ApplySorting(paginatedDogs, attribute, order);
        }

        public async Task<string> CreateDogAsync(DogModel newDog)
        {
            _logger.LogInformation("Creating a new dog entry.");

            if (await _context.Dogs.AnyAsync(d => d.Name == newDog.Name))
            {
                _logger.LogWarning("Duplicate dog entry detected: a dog with the same name already exists.");
                return ResponseMessages.DogExists;
            }

            if (newDog.TailLength < 0 || newDog.Weight <= 0)
            {
                _logger.LogWarning("Invalid dog data: tail length or weight is incorrect.");
                return ResponseMessages.InvalidDogData;
            }

            await _context.Dogs.AddAsync(newDog);
            await _context.SaveChangesAsync();
            _logger.LogInformation("New dog entry created successfully.");

            InvalidateCache();
            return string.Empty;
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

        private void InvalidateCache()
        {
            _logger.LogInformation("Invalidating all cache entries related to paginated dog lists.");
            foreach (var key in _cacheKeys)
            {
                _cache.Remove(key);
            }
            _cacheKeys.Clear();
        }

        public string Ping()
        {
            return ResponseMessages.VersionMessage;
        }
    }
}