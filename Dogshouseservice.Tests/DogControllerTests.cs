using AutoFixture;
using AutoFixture.AutoMoq;
using Dogshouseservice.Constants;
using Dogshouseservice.Controllers;
using Dogshouseservice.Helpers;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

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

            _dogServiceMock = _fixture.Freeze<Mock<IDogService>>();
            var logger = _fixture.Create<ILogger<DogController>>();
            _dogController = new DogController(_dogServiceMock.Object, logger);
        }

        [Fact]
        public void Ping_ReturnsOkWithMessage()
        {
            // Arrange
            _dogServiceMock.Setup(service => service.Ping())
                .Returns(ResponseMessages.VersionMessage);

            // Act
            var result = _dogController.Ping();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(ResponseMessages.VersionMessage, okResult.Value);
        }

        [Fact]
        public async Task Dogs_ReturnsOkWithDogList()
        {
            // Arrange
            var dogs = _fixture.CreateMany<DogModel>(2).ToList();
            _dogServiceMock.Setup(service => service.GetDogsAsync(DogSortingAttribute.Name, "asc", 1, 10))
                .ReturnsAsync(dogs);

            // Act
            var result = await _dogController.Dogs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedDogs = Assert.IsType<List<DogModel>>(okResult.Value);
            Assert.Equal(2, returnedDogs.Count);
        }

        [Fact]
        public async Task Dog_ReturnsConflictIfDogExists()
        {
            // Arrange
            var newDog = _fixture.Create<DogModel>();
            _dogServiceMock.Setup(service => service.CreateDogAsync(newDog))
                .ReturnsAsync(ResponseMessages.DogExists);

            // Act
            var result = await _dogController.Dog(newDog);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(ResponseMessages.DogExists, conflictResult.Value);
        }

        [Fact]
        public async Task Dog_ReturnsBadRequestIfInvalidData()
        {
            // Arrange
            var newDog = _fixture.Build<DogModel>()
                .With(d => d.TailLength, -1)
                .With(d => d.Weight, 0)
                .Create();

            _dogServiceMock.Setup(service => service.CreateDogAsync(newDog))
                .ReturnsAsync(ResponseMessages.InvalidDogData);

            // Act
            var result = await _dogController.Dog(newDog);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(ResponseMessages.InvalidDogData, badRequestResult.Value);
        }

        [Fact]
        public async Task Dog_ReturnsCreatedAtActionIfSuccessful()
        {
            // Arrange
            var newDog = _fixture.Create<DogModel>();
            _dogServiceMock.Setup(service => service.CreateDogAsync(newDog))
                .ReturnsAsync(string.Empty);

            // Act
            var result = await _dogController.Dog(newDog);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(DogController.Dogs), createdAtActionResult.ActionName);
            Assert.Equal(newDog, createdAtActionResult.Value);
        }
    }
}
