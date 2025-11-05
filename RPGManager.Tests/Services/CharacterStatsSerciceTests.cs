using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RPGManager.Interfaces;
using RPGManager.Models;
using RPGManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RPGManager.Tests.Services;

[TestFixture]
public class CharacterStatsServiceTests
{
    private Mock<IRepository<CharacterStats>> _mockCharacterStatsRepository = null!;
    private CharacterStatsService _characterStatsService = null!;
    private Mock<ICharacterRepository> _mockCharacterRepository = null!;
    
    private Character _testCharacter = null!;
    private CharacterStats _testCharacterStats = null!;
    private CharacterStats _newTestStats = null!;

    private string _emptyName;
    private string _whitespaceName;
    private string _invalidTest;
    private string _validTest;

    [SetUp]
    public void Setup()
    {
        _mockCharacterStatsRepository = new Mock<IRepository<CharacterStats>>();
        _mockCharacterRepository = new Mock<ICharacterRepository>();

        _characterStatsService = new CharacterStatsService(
            _mockCharacterRepository.Object,
            _mockCharacterStatsRepository.Object);

        _testCharacter = new Character
        {
            Id = 10,
            Name = "TestHero"
        };

        _testCharacterStats = new CharacterStats
        {
            Id = 1,
            CharacterId = 10,
            Strength = 16,
            Dexterity = 12,
            Intelligence = 10,
            Constitution = 15,
            Wisdom = 12,
            Charisma = 14
        };

        _newTestStats = new CharacterStats
        {
            Strength = 14,
            Dexterity = 13,
            Intelligence = 12,
            Constitution = 15,
            Wisdom = 11,
            Charisma = 10
        };

        _emptyName = "";
        _whitespaceName = "   ";
        _invalidTest = "Invalid";
        _validTest = "Valid";

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(_testCharacter.Id))
            .ReturnsAsync(_testCharacter);
        _mockCharacterStatsRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats> { _testCharacterStats });
    }

    //  -----------------
    //  Constructor Tests
    //  -----------------

    [Test]
    public void Constructor_WithNullCharacterRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterStatsService(null, _mockCharacterStatsRepository.Object));

        Assert.That(ex.ParamName, Is.EqualTo("characterRepository"));
    }

    [Test]
    public void Constructor_WithNullCharacterStatsRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterStatsService(_mockCharacterRepository.Object, null));

        Assert.That(ex.ParamName, Is.EqualTo("characterStatsRepository"));
    }

    [Test]
    public void Constructor_WithValidRepository_CreatesInstance()
    {
        var service = new CharacterStatsService(
            _mockCharacterRepository.Object,
            _mockCharacterStatsRepository.Object);

        Assert.That(service, Is.Not.Null);
    }

    //  ----------------------------
    //  GetCharacterStatsAsync Tests
    //  ----------------------------

    [Test]
    public async Task GetCharacterStatsAsync_WithValidCharacterIdAndStats_ReturnsCharacterStats()
    {
        var result = await _characterStatsService.GetCharacterStatsAsync(_testCharacter.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CharacterId, Is.EqualTo(_testCharacter.Id));
        Assert.That(result.Strength, Is.EqualTo(16));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
    }

    [Test]
    public void GetCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(_testCharacter.Id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.GetCharacterStatsAsync(_testCharacter.Id));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {_testCharacter.Id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Never);
    }

    [Test]
    public async Task GetCharacterStatsAsync_WhenNoStatsExist_ReturnsNull()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats>());

        var result = await _characterStatsService.GetCharacterStatsAsync(_testCharacter.Id);

        Assert.That(result, Is.Null);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
    }

    //  -------------------------------
    //  CreateCharacterStatsAsync Tests
    //  -------------------------------

    [Test]
    public async Task CreateCharacterStatsAsync_WithValidData_CreatesAndReturnsCharacterStats()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats>());
        _mockCharacterStatsRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterStats>>()))
            .Returns(Task.CompletedTask);

        var result = await _characterStatsService.CreateCharacterStatsAsync(_testCharacter.Id, _newTestStats);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CharacterId, Is.EqualTo(_testCharacter.Id));
        Assert.That(result.Strength, Is.EqualTo(14));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.AddRangeAsync(
            It.IsAny<IEnumerable<CharacterStats>>()), Times.Once);
    }

    [Test]
    public void CreateCharacterStatsAsync_WithNullStats_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _characterStatsService.CreateCharacterStatsAsync(_testCharacter.Id, null!));

        Assert.That(ex.ParamName, Is.EqualTo("stats"));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Never);
        _mockCharacterStatsRepository.Verify(repo => repo.AddRangeAsync(
            It.IsAny<IEnumerable<CharacterStats>>()), Times.Never);
    }

    [Test]
    public void CreateCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(_testCharacter.Id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.CreateCharacterStatsAsync(_testCharacter.Id, _newTestStats));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {_testCharacter.Id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Never);
        _mockCharacterStatsRepository.Verify(repo => repo.AddRangeAsync(
            It.IsAny<IEnumerable<CharacterStats>>()), Times.Never);
    }

    [Test]
    public void CreateCharacterStatsAsync_WhenStatsAlreadyExist_ThrowsInvalidOperationException()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.CreateCharacterStatsAsync(_testCharacter.Id, _newTestStats));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {_testCharacter.Id} already has stats defined."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.AddRangeAsync(
            It.IsAny<IEnumerable<CharacterStats>>()), Times.Never);
    }

    //  -------------------------------
    //  UpdateCharacterStatsAsync Tests
    //  -------------------------------

    [Test]
    public async Task UpdateCharacterStatsAsync_WithValidData_UpdatesAndReturnsTrue()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<CharacterStats>()))
            .Returns(Task.CompletedTask);

        var result = await _characterStatsService.UpdateCharacterStatsAsync(_testCharacter.Id, _newTestStats);

        Assert.That(result, Is.True);
        Assert.That(_testCharacterStats.Strength, Is.EqualTo(14));
        Assert.That(_testCharacterStats.Dexterity, Is.EqualTo(13));

        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.UpdateAsync(
            It.IsAny<CharacterStats>()), Times.Once);
    }

    [Test]
    public void UpdateCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(_testCharacter.Id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.UpdateCharacterStatsAsync(_testCharacter.Id, _newTestStats));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {_testCharacter.Id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Never);
        _mockCharacterStatsRepository.Verify(repo => repo.UpdateAsync(
            It.IsAny<CharacterStats>()), Times.Never);
    }

    [Test]
    public void UpdateCharacterStatsAsync_WithNullStats_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _characterStatsService.UpdateCharacterStatsAsync(_testCharacter.Id, null!));

        Assert.That(ex.ParamName, Is.EqualTo("stats"));

        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Never);
        _mockCharacterStatsRepository.Verify(repo => repo.UpdateAsync(
            It.IsAny<CharacterStats>()), Times.Never);
    }

    [Test]
    public async Task UpdateCharacterStatsAsync_WhenNoStatsExist_ReturnsFalse()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats>());

        var result = await _characterStatsService.UpdateCharacterStatsAsync(_testCharacter.Id, _newTestStats);

        Assert.That(result, Is.False);

        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.UpdateAsync(
            It.IsAny<CharacterStats>()), Times.Never);
    }

    [Test]
    public void UpdateCharacterStatsAsync_WhenUpdateFails_ThrowsException()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<CharacterStats>()))
            .ThrowsAsync(new Exception("Database update failed."));

        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _characterStatsService.UpdateCharacterStatsAsync(_testCharacter.Id, _newTestStats));

        Assert.That(ex.Message, Is.EqualTo("Database update failed."));

        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.UpdateAsync(
            It.IsAny<CharacterStats>()), Times.Once);
    }

    // -------------------------------
    // DeleteCharacterStatsAsync Tests
    // -------------------------------

    [Test]
    public async Task DeleteCharacterStatsAsync_WithValidCharacterId_DeletesAndReturnsTrue()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.DeleteAsync(It.IsAny<CharacterStats>()))
            .Returns(Task.CompletedTask);

        var result = await _characterStatsService.DeleteCharacterStatsAsync(_testCharacter.Id);

        Assert.That(result, Is.True);

        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.DeleteAsync(
            It.IsAny<CharacterStats>()), Times.Once);
    }

    [Test]
    public async Task DeleteCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(_testCharacter.Id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.DeleteCharacterStatsAsync(_testCharacter.Id));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {_testCharacter.Id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(_testCharacter.Id), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Never);
        _mockCharacterStatsRepository.Verify(repo => repo.DeleteAsync(
            It.IsAny<CharacterStats>()), Times.Never);
    }

    [Test]
    public async Task DeleteCharacterStatsAsync_WhenNoStatsExist_ReturnsFalse()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats>());

        var result = await _characterStatsService.DeleteCharacterStatsAsync(_testCharacter.Id);

        Assert.That(result, Is.False);

        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.DeleteAsync(
            It.IsAny<CharacterStats>()), Times.Never);
    }

    [Test]
    public void DeleteCharacterStatsAsync_WhenDeleteFails_ThrowsException()
    {
        _mockCharacterStatsRepository
            .Setup(repo => repo.DeleteAsync(It.IsAny<CharacterStats>()))
            .ThrowsAsync(new Exception("Database delete failed."));

        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _characterStatsService.DeleteCharacterStatsAsync(_testCharacter.Id));

        Assert.That(ex.Message, Is.EqualTo("Database delete failed."));

        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.DeleteAsync(
            It.IsAny<CharacterStats>()), Times.Once);
    }

    // -------------------------------------------
    // BulkInsertCharacterStatsFromJsonAsync Tests
    // -------------------------------------------
    
}