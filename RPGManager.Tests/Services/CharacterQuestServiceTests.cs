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
public class CharacterQuestServiceTests
{
    private Mock<ICharacterRepository> _mockCharacterRepository = null!;
    private Mock<IRepository<CharacterQuest>> _mockCharacterQuestRepository = null!;
    private Mock<IRepository<Quest>> _mockQuestRepository = null!;
    private CharacterQuestService _characterQuestService = null!;

    private Character _testCharacter = null!;
    private Quest _testQuest = null!;
    private CharacterQuest _testAssignment = null!;

    private const int ValidCharacterId = 10;
    private const int ValidQuestId = 20;

    [SetUp]
    public void Setup()
    {
        _mockCharacterRepository = new Mock<ICharacterRepository>();
        _mockCharacterQuestRepository = new Mock<IRepository<CharacterQuest>>();
        _mockQuestRepository = new Mock<IRepository<Quest>>();

        _characterQuestService = new CharacterQuestService(
            _mockCharacterRepository.Object,
            _mockCharacterQuestRepository.Object,
            _mockQuestRepository.Object);

        _testCharacter = new Character
        {
            Id = ValidCharacterId,
            Name = "Anya",
            Level = 15,
            Gold = 100,
            Experience = 500
        };

        _testQuest = new Quest
        {
            Id = ValidQuestId,
            Title = "The Goblin Menace",
            RequiredLevel = 10,
            RewardGold = 50,
            RewardExperience = 1000
        };

        _testAssignment = new CharacterQuest
        {
            CharacterId = ValidCharacterId,
            QuestId = ValidQuestId,
            Status = "InProgress",
            StartedDate = DateTime.UtcNow.AddDays(-1)
        };

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(ValidCharacterId))
            .ReturnsAsync(_testCharacter);
        _mockQuestRepository
            .Setup(repo => repo.GetByIdAsync(ValidQuestId))
            .ReturnsAsync(_testQuest);
        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest>());
    }

    //  -----------------
    //  Constructor Tests
    //  -----------------

    [Test]
    public void Constructor_WithNullCharacterRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterQuestService(null, _mockCharacterQuestRepository.Object, _mockQuestRepository.Object));

        Assert.That(ex.ParamName, Is.EqualTo("characterRepository"));
    }

    [Test]
    public void Constructor_WithNullCharacterQuestsRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterQuestService(_mockCharacterRepository.Object, null, _mockQuestRepository.Object));

        Assert.That(ex.ParamName, Is.EqualTo("characterQuestRepository"));
    }

    [Test]
    public void Constructor_WithNullQuestsRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterQuestService(_mockCharacterRepository.Object, _mockCharacterQuestRepository.Object, null));

        Assert.That(ex.ParamName, Is.EqualTo("questRepository"));
    }

    [Test]
    public void Constructor_WithValidRepository_CreatesInstance()
    {
        var service = new CharacterQuestService(
            _mockCharacterRepository.Object,
            _mockCharacterQuestRepository.Object,
            _mockQuestRepository.Object);

        Assert.That(service, Is.Not.Null);
    }

    // ------------------------------
    //  GetCharacterQuestsAsync Tests
    // ------------------------------

    [Test]
    public async Task GetCharacterQuestsAsync_WithExistingCharacterAndQuests_ReturnsQuests()
    {
        var expectedQuests = new List<CharacterQuest> { _testAssignment };

        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(expectedQuests);

        var result = await _characterQuestService.GetCharacterQuestsAsync(ValidCharacterId);

        Assert.That(result, Is.EqualTo(expectedQuests));
        _mockCharacterQuestRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterQuest, bool>>>()), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void GetCharacterQuestsAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.GetCharacterQuestsAsync(id),
            $"Character with ID {id} not found.");
    }

    [Test]
    public async Task GetCharacterQuestsAsync_WhenNoQuestsExist_ReturnsNull()
    {
        var result = await _characterQuestService.GetCharacterQuestsAsync(ValidCharacterId);

        Assert.That(result, Is.Empty);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(ValidCharacterId), Times.Once);
        _mockCharacterQuestRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterQuest, bool>>>()), Times.Once);
    }

    // ----------------------------------
    //  AssignQuestToCharacterAsync Tests
    // ----------------------------------

    [Test]
    public async Task AssignQuestToCharacterAsync_ValidInput_AssignsQuest()
    {
        _mockCharacterQuestRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterQuest>>()))
            .Returns(Task.CompletedTask);

        var result = await _characterQuestService.AssignQuestToCharacterAsync(ValidCharacterId, ValidQuestId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CharacterId, Is.EqualTo(ValidCharacterId));
        Assert.That(result.QuestId, Is.EqualTo(ValidQuestId));
        Assert.That(result.Status, Is.EqualTo("NotStarted"));

        _mockCharacterQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterQuest>>(list => list.Count() == 1)), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void AssignQuestToCharacterAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.AssignQuestToCharacterAsync(id, ValidQuestId),
            $"Character with ID {id} not found.");
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void AssignQuestToCharacterAsync_WithInvalidQuestId_ThrowsInvalidOperationException(int id)
    {
        _mockQuestRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Quest)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.AssignQuestToCharacterAsync(ValidCharacterId, id),
            $"Quest with ID {id} not found.");
    }

    [Test]
    public void AssignQuestToCharacterAsync_QuestAlreadyAssigned_ThrowsInvalidOperationException()
    {
        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest> { _testAssignment });

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.AssignQuestToCharacterAsync(ValidCharacterId, ValidQuestId),
            $"Quest with ID {ValidQuestId} is already assigned to character with ID {ValidCharacterId}.");
    }

    [Test]
    public void AssignQuestToCharacterAsync_CharacterLevelTooLow_ThrowsInvalidOperationException()
    {
        _testCharacter.Level = 5;

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.AssignQuestToCharacterAsync(ValidCharacterId, ValidQuestId),
            "Character level (5) is too low. Required Level: 10.");
    }

    // -----------------------------
    //  UpdateQuestStatusAsync Tests
    // -----------------------------

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void UpdateQuestStatusAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.UpdateQuestStatusAsync(id, ValidQuestId, "Completed"),
            $"Character with ID {id} not found.");
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void UpdateQuestStatusAsync_WithInvalidQuestId_ThrowsInvalidOperationException(int id)
    {
        _mockQuestRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Quest)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.UpdateQuestStatusAsync(ValidCharacterId, id, "Completed"),
            $"Quest with ID {id} not found.");
    }

    [Test]
    public async Task UpdateQuestStatusAsync_ToCompleted_RewardsCharacterAndUpdatesStatus()
    {
        var originalGold = _testCharacter.Gold;
        var originalExp = _testCharacter.Experience;

        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest> { _testAssignment });
        _mockCharacterRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Character>()))
            .Returns(Task.CompletedTask);
        _mockCharacterQuestRepository.Setup(repo => repo.UpdateAsync(It.IsAny<CharacterQuest>()))
            .Returns(Task.CompletedTask);

        var result = await _characterQuestService.UpdateQuestStatusAsync(ValidCharacterId, ValidQuestId, "Completed");

        Assert.That(result, Is.True);
        Assert.That(_testAssignment.Status, Is.EqualTo("Completed"));
        Assert.That(_testAssignment.CompletedDate, Is.Not.Null);

        Assert.That(_testCharacter.Gold, Is.EqualTo(originalGold + _testQuest.RewardGold));
        Assert.That(_testCharacter.Experience, Is.EqualTo(originalExp + _testQuest.RewardExperience));

        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(_testCharacter), Times.Once);
        _mockCharacterQuestRepository.Verify(repo => repo.UpdateAsync(_testAssignment), Times.Once);
    }

    [Test]
    public async Task UpdateQuestStatusAsync_ToFailed_SetsCompletedDateAndUpdatesStatus()
    {
        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest> { _testAssignment });
        _mockCharacterQuestRepository.Setup(repo => repo.UpdateAsync(It.IsAny<CharacterQuest>()))
            .Returns(Task.CompletedTask);

        var result = await _characterQuestService.UpdateQuestStatusAsync(ValidCharacterId, ValidQuestId, "Failed");

        Assert.That(result, Is.True);
        Assert.That(_testAssignment.Status, Is.EqualTo("Failed"));
        Assert.That(_testAssignment.CompletedDate, Is.Not.Null);

        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
        _mockCharacterQuestRepository.Verify(repo => repo.UpdateAsync(_testAssignment), Times.Once);
    }

    [Test]
    public async Task UpdateQuestStatusAsync_NoAssignmentFound_ReturnsFalse()
    {
        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest>());

        var result = await _characterQuestService.UpdateQuestStatusAsync(ValidCharacterId, ValidQuestId, "InProgress");

        Assert.That(result, Is.False);
        _mockCharacterQuestRepository.Verify(repo => repo.UpdateAsync(It.IsAny<CharacterQuest>()), Times.Never);
    }

    [Test]
    public async Task UpdateQuestStatusAsync_StatusAlreadySame_ReturnsTrueAndSkipsUpdate()
    {
        _testAssignment.Status = "InProgress";
        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest> { _testAssignment });

        var result = await _characterQuestService.UpdateQuestStatusAsync(ValidCharacterId, ValidQuestId, "InProgress");

        Assert.That(result, Is.True);
        _mockCharacterQuestRepository.Verify(repo => repo.UpdateAsync(It.IsAny<CharacterQuest>()), Times.Never);
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase(" ")]
    public void UpdateQuestStatusAsync_InvalidStatus_ThrowsArgumentException(string status)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterQuestService.UpdateQuestStatusAsync(ValidCharacterId, ValidQuestId, status));

        Assert.That(ex.ParamName, Is.EqualTo("status"));
        Assert.That(ex.Message, Does.Contain("Status cannot be empty or whitespace."));
    }

    [Test]
    public void UpdateQuestStatusAsync_NonExistingStatus_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterQuestService.UpdateQuestStatusAsync(ValidCharacterId, ValidQuestId, "Pending"));
    }

    // ------------------------------------
    //  RemoveQuestFromCharacterAsync Tests
    // ------------------------------------

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void RemoveQuestFromCharacterAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.RemoveQuestFromCharacterAsync(id, ValidQuestId),
            $"Character with ID {id} not found.");
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void RemoveQuestFromCharacterAsync_WithInvalidQuestId_ThrowsInvalidOperationException(int id)
    {
        _mockQuestRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Quest)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.RemoveQuestFromCharacterAsync(ValidCharacterId, id),
            $"Quest with ID {id} not found.");
    }

    [Test]
    public async Task RemoveQuestFromCharacterAsync_AssignmentExists_DeletesAndReturnsTrue()
    {
        _mockCharacterQuestRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest> { _testAssignment });
        _mockCharacterQuestRepository.Setup(repo => repo.DeleteAsync(It.IsAny<CharacterQuest>()))
            .Returns(Task.CompletedTask);

        var result = await _characterQuestService.RemoveQuestFromCharacterAsync(ValidCharacterId, ValidQuestId);

        Assert.That(result, Is.True);

        _mockCharacterQuestRepository.Verify(repo => repo.DeleteAsync(_testAssignment), Times.Once);
    }

    [Test]
    public async Task RemoveQuestFromCharacterAsync_AssignmentNotFound_ReturnsFalse()
    {
        var result = await _characterQuestService.RemoveQuestFromCharacterAsync(ValidCharacterId, ValidQuestId);

        Assert.That(result, Is.False);
        _mockCharacterQuestRepository.Verify(repo => repo.DeleteAsync(It.IsAny<CharacterQuest>()), Times.Never);
    }

    // ---------------------------------------------
    //  BulkInsertCharacterQuestsFromJsonAsync Tests
    // ---------------------------------------------

    [Test]
    public async Task BulkInsertCharacterQuestsFromJsonAsync_WithValidJson_InsertsAllQuests()
    {
        var questsToInsert = new List<CharacterQuest>
        {
            _testAssignment,
            new CharacterQuest { CharacterId = ValidCharacterId + 1, QuestId = ValidQuestId + 1 }
        };

        var characters = new List<Character>
        {
            _testCharacter,
            new Character { Id = ValidCharacterId + 1, Name = "Bob", Level = 5 }
        };

        var quests = new List<Quest>
        {
            _testQuest,
            new Quest { Id = ValidQuestId + 1, Title = "Rescue the Princess", RequiredLevel = 5, RewardGold = 200, RewardExperience = 1500 }
        };

        var jsonContent = JsonConvert.SerializeObject(questsToInsert);
        var jsonFilePath = "bulk_character_quests.json";

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => characters.FirstOrDefault(c => c.Id == id));
        _mockQuestRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => quests.FirstOrDefault(q => q.Id == id)!);
        _mockCharacterQuestRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterQuest>>()))
            .Returns(Task.CompletedTask);

        await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(jsonFilePath);

        _mockCharacterQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterQuest>>(cq => cq.Count() == questsToInsert.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharacterQuestsFromJsonAsync_WithLargeCharacterQuestList_InsertsAllQuests()
    {
        var characters = new List<Character>();
        var quests = new List<Quest>();
        var questsToInsert = new List<CharacterQuest>();

        for (int i = 1; i <= 1000; i++)
        {
            characters.Add(new Character { Id = i, Name = $"BulkChar{i}", Level = 10 });
            quests.Add(new Quest { Id = i * 100, Title = $"BulkQuest{i}", RequiredLevel = 1 });
            questsToInsert.Add(new CharacterQuest { CharacterId = i, QuestId = i * 100 });
        }

        var jsonContent = JsonConvert.SerializeObject(questsToInsert);
        var jsonFilePath = "bulk_character_quests.json";

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => characters.FirstOrDefault(c => c.Id == id));
        _mockQuestRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => quests.FirstOrDefault(q => q.Id == id)!);
        _mockCharacterQuestRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterQuest>>()))
            .Returns(Task.CompletedTask);

        await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(jsonFilePath);

        _mockCharacterQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterQuest>>(cq => cq.Count() == questsToInsert.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase("   ")]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithInvalidPath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty or whitespace."));
    }

    [Test]
    public void BulkInsertCharacterQuestsFromJsonAsync_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        var nonExistentPath = "does_not_exist.json";

        var ex = Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(nonExistentPath));

        Assert.That(ex.Message, Does.Contain("File not found"));
    }

    [Test]
    public void BulkInsertCharacterQuestsFromJsonAsync_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "invalid_character_equipment.json";
        var invalidJsonContent = "{ invalid json }";

        File.WriteAllText(jsonFilePath, invalidJsonContent);

        var ex = Assert.ThrowsAsync<JsonReaderException>(
            async () => await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("Invalid character"));

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertCharacterQuestsFromJsonAsync_WithEmptyJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "empty_character_equipment.json";
        var emptyJsonContent = "[]";

        File.WriteAllText(jsonFilePath, emptyJsonContent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("No character quests found in JSON file."));

        File.Delete(jsonFilePath);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task BulkInsertCharacterQuestsFromJsonAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        var questsToInsert = new List<CharacterQuest>
        {
            new CharacterQuest { CharacterId = id, QuestId = ValidQuestId }
        };

        var jsonContent = JsonConvert.SerializeObject(questsToInsert);
        var jsonFilePath = "missing_char_bulk.json";
        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(jsonFilePath),
            $"Character with ID {id} not found.");

        _mockCharacterQuestRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterQuest>>()), Times.Never);

        File.Delete(jsonFilePath);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task BulkInsertCharacterQuestsFromJsonAsync_WithInvalidQuestId_ThrowsInvalidOperationException(int id)
    {
        var questsToInsert = new List<CharacterQuest>
        {
            new CharacterQuest { CharacterId = ValidCharacterId, QuestId = id }
        };

        var jsonContent = JsonConvert.SerializeObject(questsToInsert);
        var jsonFilePath = "missing_quest_bulk.json";
        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockQuestRepository
            .Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Quest)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(jsonFilePath),
            $"Character with ID {id} not found.");

        _mockCharacterQuestRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterQuest>>()), Times.Never);

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharacterQuestsFromJsonAsync_ExistingAssignment_ThrowsInvalidOperationException()
    {
        var questsToInsert = new List<CharacterQuest>
        {
            _testAssignment
        };

        var jsonContent = JsonConvert.SerializeObject(questsToInsert);
        var jsonFilePath = "existing_assignment_bulk.json";
        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterQuestRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterQuest, bool>>>()))
            .ReturnsAsync(new List<CharacterQuest> { _testAssignment });

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(jsonFilePath),
            $"Quest with ID {ValidQuestId} is already assigned to character with ID {ValidCharacterId}.");

        _mockCharacterQuestRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterQuest>>()), Times.Never);

        File.Delete(jsonFilePath);
    }
}