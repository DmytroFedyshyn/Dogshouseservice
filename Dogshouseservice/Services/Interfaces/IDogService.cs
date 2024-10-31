using Dogshouseservice.Models;

namespace Dogshouseservice.Services.Interfaces
{
    public interface IDogService
    {
        Task<string> PingAsync();
        Task<List<Dog>> GetDogsAsync(string attribute, string order, int pageNumber, int pageSize);
        Task<string> CreateDogAsync(Dog newDog);
    }
}
