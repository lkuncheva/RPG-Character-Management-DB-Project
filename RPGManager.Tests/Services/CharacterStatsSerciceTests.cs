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
    private List<Character> _testCharacterList = null!;
    private CharacterStats _testCharacterStats = null!;
    private CharacterStats _newTestStats = null!;
    private List<CharacterStats> _testCharacterStatsList = null!;

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
            Id = 1,
            Name = "Character1"
        };

        _testCharacterList = new List<Character>
        {
            new Character{ Id = 2, Name = "Character2" },
            new Character{ Id = 3, Name = "Character3" }
        };

        _testCharacterStats = new CharacterStats
        {
            Id = 1,
            CharacterId = 1,
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

        _testCharacterStatsList = new List<CharacterStats>
        {
            new CharacterStats 
            {
                Id = 2,
                CharacterId = 2,
                Strength = 18,
                Dexterity = 0,
                Intelligence = 5,
                Constitution = 9,
                Wisdom = 12,
                Charisma = 20
            },
            new CharacterStats
            {
                Id = 3,
                CharacterId = 3,
                Strength = 11,
                Dexterity = 3,
                Intelligence = 0,
                Constitution = 18,
                Wisdom = 20,
                Charisma = 4
            }
        };

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

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void GetCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.GetCharacterStatsAsync(id));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
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

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void CreateCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.CreateCharacterStatsAsync(id, _newTestStats));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
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

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void UpdateCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.UpdateCharacterStatsAsync(id, _newTestStats));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
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

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void DeleteCharacterStatsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.DeleteCharacterStatsAsync(id));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
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

    [TestCase(null!)]
    [TestCase("")]
    [TestCase("   ")]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithInvalidPath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertCharacterStatsFromJsonAsync_WithNonExistingFilePath_ThrowsFileNotFoundException()
    {
        var nonExistingPath = "non_existing_file.json";

        var ex = Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(nonExistingPath));

        Assert.That(ex.Message, Does.Contain($"File not found: {nonExistingPath}"));
    }

    [Test]
    public void BulkInsertCharacterStatsFromJsonAsync_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "invalid_character_stats.json";
        var invalidJsonContent = "{ invalid json }";

        File.WriteAllText(jsonFilePath, invalidJsonContent);

        var ex = Assert.ThrowsAsync<JsonReaderException>(
            async () => await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("Invalid character"));

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharacterStatsFromJsonAsync_WithValidJson_InsertsCharacterStats()
    {
        var jsonContent = JsonConvert.SerializeObject(_testCharacterStatsList);
        var jsonFilePath = "test_character_stats.json";

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => _testCharacterList.FirstOrDefault(c => c.Id == id));
        _mockCharacterStatsRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats>());
        _mockCharacterStatsRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterStats>>()))
            .Returns(Task.CompletedTask);

        await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(jsonFilePath);

        _mockCharacterStatsRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterStats>>(cs => cs.Count() == _testCharacterStatsList.Count)), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(2), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(3), Times.Once);
        _mockCharacterStatsRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()),
            Times.Exactly(_testCharacterStatsList.Count));

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertCharacterStatsFromJsonAsync_WithEmptyJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "empty_character_stats.json";
        var emptyJsonContent = "[]";

        File.WriteAllText(jsonFilePath, emptyJsonContent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("No character stats found in JSON file."));

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertCharacterStatsFromJsonAsync_WithEmptyCharacterStatstList_ThrowsArgumentException()
    {
        var jsonFilePath = "empty_character_stats.json";
        var emptyCharacterStatsList = new List<CharacterStats>();
        var jsonContent = JsonConvert.SerializeObject(emptyCharacterStatsList);

        File.WriteAllText(jsonFilePath, jsonContent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("No character stats found"));

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharacterStatsFromJsonAsync_WithLargeCharacterStatstList_InsertsAllCharacterStats()
    {
        var jsonFilePath = "large_character_stats.json";

        var largeCharacterStats = Enumerable.Range(1, 1000)
            .Select(i => new CharacterStats
            {
                Id = i,
                CharacterId = i,
                Strength = i % 20,
                Dexterity = (i + 1) % 20,
                Intelligence = (i + 2) % 20,
                Constitution = (i + 3) % 20,
                Wisdom = (i + 4) % 20,
                Charisma = (i + 5) % 20
            }).ToList();

        var largeCharacters = Enumerable.Range(1, 1000)
            .Select(i => new Character
            {
                Id = i,
                Name = $"Char{i}"
            }).ToList();

        var jsonContent = JsonConvert.SerializeObject(largeCharacterStats);

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => largeCharacters.FirstOrDefault(c => c.Id == id));
        _mockCharacterStatsRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats>());
        _mockCharacterStatsRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterStats>>()))
            .Returns(Task.CompletedTask);

        await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(jsonFilePath);

        _mockCharacterStatsRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterStats>>(cs => cs.Count() == largeCharacterStats.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void BulkInsertCharacterStatsFromJsonAsync_WithNonExistingCharacterInFile_ThrowsInvalidOperationException(int id)
    {
        var jsonContent = JsonConvert.SerializeObject(_testCharacterList);
        var jsonFilePath = "missing_char_stats.json";

        File.WriteAllText(jsonFilePath, jsonContent);

        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character?)null);
        _mockCharacterStatsRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterStats, bool>>>()))
            .ReturnsAsync(new List<CharacterStats>());

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterStatsService.BulkInsertCharacterStatsFromJsonAsync(jsonFilePath),
            $"Character with ID {id} not found.");

        _mockCharacterStatsRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterStats>>()), Times.Never);

        File.Delete(jsonFilePath);
    }
}