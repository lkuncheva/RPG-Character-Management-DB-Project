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

    //  --------------------------
    //  CreateCharacterAsync Tests
    //  --------------------------

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

    //  ---------------------------
    //  GetCharacterByIdAsync Tests
    //  ---------------------------

    [Test]
    public async Task GetCharacterByIdAsync_WithValidId_ReturnsCharacter()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);

        var result = await _characterService.GetCharacterByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("TestHero"));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetCharacterByIdAsync_WithNonExistentId_ReturnsNull()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.GetCharacterByIdAsync(999);

        Assert.That(result, Is.Null);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
    }

    [Test]
    public async Task GetCharacterByIdAsync_WithNegativeId_ReturnsNull()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(-1))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.GetCharacterByIdAsync(-1);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCharacterByIdAsync_WithZeroId_ReturnsNull()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(0))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.GetCharacterByIdAsync(0);

        Assert.That(result, Is.Null);
    }

    //  --------------------------
    //  UpdateCharacterAsync Tests
    //  --------------------------

    [Test]
    public async Task UpdateCharacterAsync_WithValidCharacter_ReturnsUpdatedCharacter()
    {
        var updatedCharacter = new Character
        {
            Id = 1,
            Name = "UpdatedHero",
            Level = 10,
            Experience = 2000,
            Gold = 1000
        };

        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);
        _mockCharacterRepository.Setup(repo => repo.UpdateAsync(updatedCharacter))
            .Returns(Task.CompletedTask);

        var result = await _characterService.UpdateCharacterAsync(updatedCharacter);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("UpdatedHero"));
        Assert.That(result.Level, Is.EqualTo(10));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(updatedCharacter), Times.Once);
    }

    [Test]
    public void UpdateCharacterAsync_WithNullCharacter_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _characterService.UpdateCharacterAsync(null));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
    }

    [Test]
    public void UpdateCharacterAsync_WithNonExistentCharacter_ThrowsInvalidOperationException()
    {
        var nonExistentCharacter = new Character { Id = 999, Name = "Ghost" };
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterService.UpdateCharacterAsync(nonExistentCharacter));

        Assert.That(ex.Message, Is.EqualTo("Character with ID 999 not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
    }

    //  ------------------------------
    //  UpdateCharacterNameAsync Tests
    //  ------------------------------

    [Test]
    public async Task UpdateCharacterNameAsync_WithValidData_ReturnsTrue()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);
        _mockCharacterRepository.Setup(repo => repo.UpdateAsync(_testCharacter))
            .Returns(Task.CompletedTask);

        var result = await _characterService.UpdateCharacterNameAsync(1, "NewHeroName");

        Assert.That(result, Is.True);
        Assert.That(_testCharacter.Name, Is.EqualTo("NewHeroName"));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(_testCharacter), Times.Once);
    }

    [Test]
    public async Task UpdateCharacterNameAsync_WithNonExistentCharacter_ReturnsFalse()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.UpdateCharacterNameAsync(999, "GhostName");

        Assert.That(result, Is.False);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
    }

    [Test]
    public void UpdateCharacterNameAsync_WithEmptyName_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.UpdateCharacterNameAsync(1, ""));

        Assert.That(ex.ParamName, Is.EqualTo("newName"));
        Assert.That(ex.Message, Does.Contain("New name cannot be empty"));
    }

    [Test]
    public void UpdateCharacterNameAsync_WithWhitespaceName_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.UpdateCharacterNameAsync(1, "   "));

        Assert.That(ex.ParamName, Is.EqualTo("newName"));
    }

    [Test]
    public async Task UpdateCharacterNameAsync_WithSameName_ReturnsTrue()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);

        var result = await _characterService.UpdateCharacterNameAsync(1, "TestHero");

        Assert.That(result, Is.True);
        Assert.That(_testCharacter.Name, Is.EqualTo("TestHero"));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
    }

    //  -------------------------------
    //  UpdateCharacterLevelAsync Tests
    //  -------------------------------

    [Test]
    public async Task UpdateCharacterLevelAsync_WithValidData_ReturnsTrue()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);
        _mockCharacterRepository.Setup(repo => repo.UpdateAsync(_testCharacter))
            .Returns(Task.CompletedTask);

        var result = await _characterService.UpdateCharacterLevelAsync(1, 69);

        Assert.That(result, Is.True);
        Assert.That(_testCharacter.Level, Is.EqualTo(69));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(_testCharacter), Times.Once);
    }

    [Test]
    public async Task UpdateCharacterLevelAsync_WithNonExistentCharacter_ReturnsFalse()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.UpdateCharacterLevelAsync(999, 10);
        Assert.That(result, Is.False);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
    }

    [Test]
    public void UpdateCharacterLevelAsync_WithZeroLevel_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.UpdateCharacterLevelAsync(1, 0));

        Assert.That(ex.ParamName, Is.EqualTo("newLevel"));
        Assert.That(ex.Message, Does.Contain("Level must be at least 1"));
    }

    [Test]
    public void UpdateCharacterLevelAsync_WithNegativeLevel_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.UpdateCharacterLevelAsync(1, -5));

        Assert.That(ex.ParamName, Is.EqualTo("newLevel"));
        Assert.That(ex.Message, Does.Contain("Level must be at least 1"));
    }

    [Test]
    public async Task UpdateCharacterLevelAsync_WithSameLevel_ReturnsTrue()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);

        var result = await _characterService.UpdateCharacterLevelAsync(1, 5);

        Assert.That(result, Is.True);
        Assert.That(_testCharacter.Level, Is.EqualTo(5));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
    }
}