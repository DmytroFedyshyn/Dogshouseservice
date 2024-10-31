using Dogshouseservice.Constants;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogshouseservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DogController : ControllerBase
    {
        private readonly IDogService _dogService;

        public DogController(IDogService dogService)
        {
            _dogService = dogService;
        }

        // GET: api/dog/ping
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var message = await _dogService.PingAsync();
            return Ok(message);
        }

        // GET: api/dog
        [HttpGet]
        public async Task<IActionResult> GetDogs(string attribute = "name", string order = "asc", int pageNumber = 1, int pageSize = 10)
        {
            var dogs = await _dogService.GetDogsAsync(attribute, order, pageNumber, pageSize);
            return Ok(dogs);
        }

        // POST: api/dog
        [HttpPost]
        public async Task<IActionResult> CreateDog([FromBody] Dog newDog)
        {
            var result = await _dogService.CreateDogAsync(newDog);
            if (result == ResponseMessages.DogExists)
                return Conflict(result);

            if (result == ResponseMessages.InvalidDogData)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetDogs), new { name = newDog.Name }, newDog);
        }
    }
}
