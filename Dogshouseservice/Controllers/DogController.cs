using Dogshouseservice.Constants;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dogshouseservice.Controllers
{
    [ApiController]
    public class DogController : ControllerBase
    {
        private readonly IDogService _dogService;
        private readonly ILogger<DogController> _logger;

        public DogController(IDogService dogService, ILogger<DogController> logger)
        {
            _dogService = dogService;
            _logger = logger;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            _logger.LogInformation("Ping endpoint was called.");
            var message = await _dogService.PingAsync();
            _logger.LogInformation("Ping response: {Message}", message);
            return Ok(message);
        }

        [HttpGet("dogs")]
        public async Task<IActionResult> Dogs(string attribute = "name", string order = "asc", int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Fetching dogs with parameters - Attribute: {Attribute}, Order: {Order}, PageNumber: {PageNumber}, PageSize: {PageSize}", attribute, order, pageNumber, pageSize);
            var dogs = await _dogService.GetDogsAsync(attribute, order, pageNumber, pageSize);
            _logger.LogInformation("Fetched {Count} dogs.", dogs.Count);
            return Ok(dogs);
        }

        [HttpPost("dog")]
        public async Task<IActionResult> Dog([FromBody] DogModel newDog)
        {
            _logger.LogInformation("Attempting to create a new dog: {DogName}", newDog.Name);
            var result = await _dogService.CreateDogAsync(newDog);

            if (result == ResponseMessages.DogExists)
            {
                _logger.LogWarning("Dog with name {DogName} already exists.", newDog.Name);
                return Conflict(result);
            }

            if (result == ResponseMessages.InvalidDogData)
            {
                _logger.LogWarning("Invalid data for dog: {DogName}. TailLength: {TailLength}, Weight: {Weight}", newDog.Name, newDog.TailLength, newDog.Weight);
                return BadRequest(result);
            }

            _logger.LogInformation("Dog {DogName} created successfully.", newDog.Name);
            return CreatedAtAction(nameof(Dogs), new { name = newDog.Name }, newDog);
        }
    }
}
