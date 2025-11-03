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
        _mockCharacterRepository.Setup(repo => repo.AddAsync(It.IsAny<Character>()))
            .Returns(Task.FromResult(_testCharacter));

        var result = await _characterService.CreateCharacterAsync(_testCharacter);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("TestHero"));
        Assert.That(result.CreatedDate, Is.Not.EqualTo(default(DateTime)));
        _mockCharacterRepository.Verify(repo => repo.AddAsync(_testCharacter), Times.Once);
    }

    [Test]
    public void CreateCharacterAsync_WithNullCharacter_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _characterService.CreateCharacterAsync(null));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
    }

    [Test]
    public void CreateCharacterAsync_WithEmptyName_ThrowsArgumentException()
    {
        var invalidCharacter = new Character { Name = "" };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.CreateCharacterAsync(invalidCharacter));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
        Assert.That(ex.Message, Does.Contain("Character name cannot be empty"));
    }

    [Test]
    public void CreateCharacterAsync_WithWhitespaceName_ThrowsArgumentException()
    {
        var invalidCharacter = new Character { Name = "   " };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.CreateCharacterAsync(invalidCharacter));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
    }

    [Test]
    public void CreateCharacterAsync_NameTooLong_ThrowsArgumentException()
    {
        var MaxNameLength = 100;
        var excessivelyLongName = new string('A', MaxNameLength + 1);
        var invalidCharacter = new Character { Name = excessivelyLongName };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.CreateCharacterAsync(invalidCharacter));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
        Assert.That(ex.Message, Does.Contain($"Character name cannot exceed {MaxNameLength} characters"));

        _mockCharacterRepository.Verify(repo => repo.AddAsync(It.IsAny<Character>()), Times.Never);
    }
}