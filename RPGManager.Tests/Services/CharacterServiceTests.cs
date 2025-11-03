using Moq;
using NUnit.Framework;
using RPGManager.Interfaces;
using RPGManager.Models;
using RPGManager.Services;
using System;
using System.Threading.Tasks;

namespace RPGManager.Tests.Services;

[TestFixture]
public class CharacterServiceTests
{
    private Mock<ICharacterRepository> _mockCharacterRepository;
    private CharacterService _characterService;
    private Character _testCharacter;

    [SetUp]
    public void Setup()
    {
        _mockCharacterRepository = new Mock<ICharacterRepository>();
        _characterService = new CharacterService(_mockCharacterRepository.Object);

        _testCharacter = new Character
        {
            Id = 1,
            Name = "TestHero",
            Level = 5,
            Experience = 1000,
            Gold = 500,
            CharacterClassId = 1,
            IsActive = true
        };
    }

    [Test]
    public async Task CreateCharacterAsync_WithValidCharacter_ReturnsCharacter()
    {
        // What: Testing that a valid character can be created successfully
        // Why: This is the core happy path for character creation
        // How: Mock repository to return the character when added, then call service method

        // Arrange
        _mockCharacterRepository.Setup(repo => repo.AddAsync(It.IsAny<Character>()))
            .Returns(Task.FromResult(_testCharacter));

        // Act
        var result = await _characterService.CreateCharacterAsync(_testCharacter);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("TestHero"));
        Assert.That(result.CreatedDate, Is.Not.EqualTo(default(DateTime))); // Should be set to UTC now
        _mockCharacterRepository.Verify(repo => repo.AddAsync(_testCharacter), Times.Once);
    }

    [Test]
    public void CreateCharacterAsync_WithNullCharacter_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _characterService.CreateCharacterAsync(null));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
    }
}