using Dogshouseservice.Helpers;
using Dogshouseservice.Models;

namespace Dogshouseservice.Services.Interfaces
{
    public interface IDogService
    {
        public string Ping();
        Task<List<DogModel>> GetDogsAsync(DogSortingAttribute attribute, string order, int pageNumber, int pageSize);
        Task<string> CreateDogAsync(DogModel newDog);
    }
}
