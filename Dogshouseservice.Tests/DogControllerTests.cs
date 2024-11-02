using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Dogshouseservice.Constants;
using Dogshouseservice.Controllers;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dogshouseservice.Tests
{
    public class DogControllerTests
    {
        private readonly Fixture _fixture;
        private readonly DogController _dogController;
        private readonly Mock<IDogService> _dogServiceMock;

        public DogControllerTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());

            _dogServiceMock = _fixture.Freeze<Mock<IDogService>>(); // Freeze the mock to be shared in the fixture
            var logger = _fixture.Create<ILogger<DogController>>(); // Create a mock logger for the controller
            _dogController = new DogController(_dogServiceMock.Object, logger); // Inject the mock service and logger into the controller
        }

        [Fact]
        public async Task Ping_ReturnsOkWithMessage()
        {
            // Arrange
            _dogServiceMock.Setup(service => service.PingAsync())
                .ReturnsAsync(ResponseMessages.VersionMessage);

            // Act
            var result = await _dogController.Ping();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(ResponseMessages.VersionMessage, okResult.Value);
        }

        [Fact]
        public async Task Dogs_ReturnsOkWithDogList()
        {
            // Arrange
            var dogs = _fixture.CreateMany<Dog>(2).ToList(); // Convert to List<Dog> with ToList()
            _dogServiceMock.Setup(service => service.GetDogsAsync("name", "asc", 1, 10))
                .ReturnsAsync(dogs); // Now returns List<Dog> without casting issues

            // Act
            var result = await _dogController.Dogs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result); // Check if the result is of type OkObjectResult
            var returnedDogs = Assert.IsType<List<Dog>>(okResult.Value); // Verify that the returned value is a list of dogs
            Assert.Equal(2, returnedDogs.Count); // Ensure the count of returned dogs matches the expected count
        }

        [Fact]
        public async Task Dog_ReturnsConflictIfDogExists()
        {
            // Arrange
            var newDog = _fixture.Create<Dog>();
            _dogServiceMock.Setup(service => service.CreateDogAsync(newDog))
                .ReturnsAsync(ResponseMessages.DogExists); // Mock a conflict scenario

            // Act
            var result = await _dogController.Dog(newDog);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result); // Check for Conflict response
            Assert.Equal(ResponseMessages.DogExists, conflictResult.Value);
        }

        [Fact]
        public async Task Dog_ReturnsBadRequestIfInvalidData()
        {
            // Arrange
            var newDog = _fixture.Build<Dog>()
                .With(d => d.TailLength, -1) // Invalid TailLength
                .With(d => d.Weight, 0) // Invalid Weight
                .Create();

            _dogServiceMock.Setup(service => service.CreateDogAsync(newDog))
                .ReturnsAsync(ResponseMessages.InvalidDogData); // Mock invalid data scenario

            // Act
            var result = await _dogController.Dog(newDog);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result); // Check for BadRequest response
            Assert.Equal(ResponseMessages.InvalidDogData, badRequestResult.Value);
        }

        [Fact]
        public async Task Dog_ReturnsCreatedAtActionIfSuccessful()
        {
            // Arrange
            var newDog = _fixture.Create<Dog>();
            _dogServiceMock.Setup(service => service.CreateDogAsync(newDog))
                .ReturnsAsync(string.Empty); // Mock a successful creation

            // Act
            var result = await _dogController.Dog(newDog);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result); // Check for CreatedAtAction response
            Assert.Equal(nameof(DogController.Dogs), createdAtActionResult.ActionName); // Verify the correct action name
            Assert.Equal(newDog, createdAtActionResult.Value); // Ensure the created dog matches the expected dog
        }
    }
}
