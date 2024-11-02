using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Dogshouseservice.Constants;
using Dogshouseservice.Models;
using Dogshouseservice.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dogshouseservice.Tests
{
    public class DogServiceTests : IDisposable
    {
        private readonly Fixture _fixture;
        private readonly ApplicationDbContext _context;
        private readonly DogService _dogService;

        public DogServiceTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase") // Use InMemoryDatabase
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureDeleted(); // Clear the database before each test
            _context.Database.EnsureCreated();

            var logger = _fixture.Create<ILogger<DogService>>();
            _dogService = new DogService(_context, logger);
        }

        public void Dispose()
        {
            // Ensure the database is deleted after each test to prevent data sharing
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task PingAsync_ReturnsVersionMessage()
        {
            // Act
            var result = await _dogService.PingAsync();

            // Assert
            Assert.Equal(ResponseMessages.VersionMessage, result);
        }

        [Fact]
        public async Task CreateDogAsync_AddsNewDog()
        {
            // Arrange
            var newDog = _fixture.Create<DogModel>();

            // Act
            var result = await _dogService.CreateDogAsync(newDog);

            // Assert
            Assert.Equal(string.Empty, result); // Empty result means success
            Assert.Single(_context.Dogs); // Verify the dog is added to the context
        }

        [Fact]
        public async Task CreateDogAsync_ReturnsDogExistsIfDogWithSameNameExists()
        {
            // Arrange
            var existingDog = _fixture.Build<DogModel>()
                .With(d => d.Name, "Buddy") // Use the name "Buddy" to create a duplicate
                .Create();

            _context.Dogs.Add(existingDog);
            await _context.SaveChangesAsync();

            var newDog = _fixture.Build<DogModel>()
                .With(d => d.Name, "Buddy") // Same "Buddy" name to check for existence validation
                .Create();

            // Act
            var result = await _dogService.CreateDogAsync(newDog);

            // Assert
            Assert.Equal(ResponseMessages.DogExists, result);
        }

        [Fact]
        public async Task CreateDogAsync_ReturnsInvalidDogDataIfInvalidTailLengthOrWeight()
        {
            // Arrange
            var invalidDog = _fixture.Build<DogModel>()
                .With(d => d.TailLength, -1) // Invalid TailLength
                .With(d => d.Weight, 0) // Invalid Weight
                .Create();

            // Act
            var result = await _dogService.CreateDogAsync(invalidDog);

            // Assert
            Assert.Equal(ResponseMessages.InvalidDogData, result);
        }

        [Fact]
        public async Task GetDogsAsync_ReturnsSortedAndPagedDogs()
        {
            // Arrange
            var dogs = _fixture.CreateMany<DogModel>(5); // Generate 5 random dogs
            await _context.Dogs.AddRangeAsync(dogs);
            await _context.SaveChangesAsync();

            // Act
            var result = await _dogService.GetDogsAsync("name", "asc", 1, 2);

            // Assert
            Assert.Equal(2, result.Count); // Expecting 2 dogs per page
        }
    }
}
