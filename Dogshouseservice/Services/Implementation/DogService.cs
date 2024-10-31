using Dogshouseservice.Constants;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dogshouseservice.Services.Implementation
{
    public class DogService : IDogService
    {
        private readonly ApplicationDbContext _context;

        public DogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<string> PingAsync()
        {
            return Task.FromResult(ResponseMessages.VersionMessage);
        }

        public async Task<List<Dog>> GetDogsAsync(string attribute, string order, int pageNumber, int pageSize)
        {
            var dogsQuery = _context.Dogs.AsQueryable();

            // Sorting logic
            dogsQuery = attribute.ToLower() switch
            {
                "weight" => order == "desc" ? dogsQuery.OrderByDescending(d => d.Weight) : dogsQuery.OrderBy(d => d.Weight),
                "tail_length" => order == "desc" ? dogsQuery.OrderByDescending(d => d.TailLength) : dogsQuery.OrderBy(d => d.TailLength),
                _ => order == "desc" ? dogsQuery.OrderByDescending(d => d.Name) : dogsQuery.OrderBy(d => d.Name),
            };

            // Pagination logic
            return await dogsQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<string> CreateDogAsync(Dog newDog)
        {
            if (_context.Dogs.Any(d => d.Name == newDog.Name))
                return ResponseMessages.DogExists;

            if (newDog.TailLength < 0 || newDog.Weight <= 0)
                return ResponseMessages.InvalidDogData;

            _context.Dogs.Add(newDog);
            await _context.SaveChangesAsync();
            return string.Empty; // Empty string indicates successful creation
        }
    }
}
