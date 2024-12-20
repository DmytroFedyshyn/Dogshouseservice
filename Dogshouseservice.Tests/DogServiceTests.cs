﻿using AutoFixture;
using AutoFixture.AutoMoq;
using Dogshouseservice.Constants;
using Dogshouseservice.Helpers;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dogshouseservice.Tests
{
    public class DogServiceTests : IAsyncLifetime
    {
        private readonly Fixture _fixture;
        private readonly DogService _dogService;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<DogService>> _loggerMock;
        private readonly ApplicationDbContext _context;
        private readonly DogQueryValidator _dogQueryValidator;

        public DogServiceTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _context = new ApplicationDbContext(options);

            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = _fixture.Freeze<Mock<ILogger<DogService>>>();

            _dogQueryValidator = new DogQueryValidator();

            _dogService = new DogService(_context, _memoryCache, _dogQueryValidator, _loggerMock.Object);
        }

        public async Task InitializeAsync()
        {
            _context.Dogs.RemoveRange(_context.Dogs);
            await _context.SaveChangesAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void Ping_ReturnsExpectedMessage()
        {
            // Act
            var result = _dogService.Ping();

            // Assert
            Assert.Equal(ResponseMessages.VersionMessage, result);
        }

        [Fact]
        public async Task GetDogsAsync_ReturnsDogList()
        {
            // Arrange
            var dogs = _fixture.CreateMany<DogModel>(3).ToList();

            foreach (var dog in dogs)
            {
                await _dogService.CreateDogAsync(dog);
            }

            // Act
            var result = await _dogService.GetDogsAsync(DogSortingAttribute.Name, "asc", 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task CreateDogAsync_ReturnsConflictIfDogExists()
        {
            // Arrange
            var newDog = _fixture.Create<DogModel>();

            await _dogService.CreateDogAsync(newDog);

            // Act
            var result = await _dogService.CreateDogAsync(newDog);

            // Assert
            Assert.Equal(ResponseMessages.DogExists, result);
        }

        [Fact]
        public async Task CreateDogAsync_ReturnsBadRequestIfInvalidData()
        {
            // Arrange
            var newDog = _fixture.Build<DogModel>()
                .With(d => d.TailLength, -1)
                .With(d => d.Weight, 0)
                .Create();

            // Act
            var result = await _dogService.CreateDogAsync(newDog);

            // Assert
            Assert.Equal(ResponseMessages.InvalidDogData, result);
        }

        [Fact]
        public async Task CreateDogAsync_ReturnsCreatedAtActionIfSuccessful()
        {
            // Arrange
            var newDog = _fixture.Create<DogModel>();

            // Act
            var result = await _dogService.CreateDogAsync(newDog);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}