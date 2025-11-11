using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RPGManager.Data.Interfaces;
using RPGManager.Data.Models;
using RPGManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RPGManager.Tests.Services;

[TestFixture]
public class QuestServiceTests
{
    private Mock<IRepository<Quest>> _mockQuestRepository = null!;
    private QuestService _questService = null!;
    private Quest _testQuest = null!;
    private List<Quest> _testQuestList = null!;

    private string[] _validDifficulties = null!;

    private string _emptyName;
    private string _whitespaceName;
    private string _invalidTest;
    private string _validTest;

    [SetUp]
    public void Setup()
    {
        _mockQuestRepository = new Mock<IRepository<Quest>>();
        _questService = new QuestService(_mockQuestRepository.Object);

        _testQuest = new Quest
        {
            Id = 1,
            Title = "Dragon Slayer",
            Description = "Slay the ancient dragon threatening the kingdom",
            RewardExperience = 5000,
            RewardGold = 1000,
            RequiredLevel = 10,
            Difficulty = "Hard"
        };

        _testQuestList = new List<Quest>
        {
            new Quest { Id = 1, Title = "Quest1", Description = "Description1", RewardExperience = 1, RewardGold = 0, RequiredLevel = 1, Difficulty = "Easy"},
            new Quest { Id = 2, Title = "Quest2", Description = "Description2", RewardExperience = 10, RewardGold = 5, RequiredLevel = 2, Difficulty = "Medium"},
            new Quest { Id = 3, Title = "Quest3", Description = "Description3", RewardExperience = 4, RewardGold = 15, RequiredLevel = 13, Difficulty = "Hard"},
            new Quest { Id = 4, Title = "Quest4", Description = "Description4", RewardExperience = 8, RewardGold = 19, RequiredLevel = 16, Difficulty = "Expert"},
            new Quest { Id = 5, Title = "Quest5", Description = "Description5", RewardExperience = 0, RewardGold = 7, RequiredLevel = 3, Difficulty = "Medium"},
            new Quest { Id = 6, Title = "Quest6", Description = "Description6", RewardExperience = 15, RewardGold = 11, RequiredLevel = 8, Difficulty = "Medium"}
        };

        _validDifficulties = new[] { "Easy", "Medium", "Hard", "Expert"};

        _emptyName = "";
        _whitespaceName = "   ";
        _invalidTest = "Invalid";
        _validTest = "Valid";
    }

    // -----------------
    // Constructor Tests
    // -----------------

    [Test]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
                () => new QuestService(null));

        Assert.That(ex.ParamName, Is.EqualTo("questRepository"));
    }

    [Test]
    public void Constructor_WithValidRepository_CreatesInstance()
    {
        var service = new QuestService(_mockQuestRepository.Object);

        Assert.That(service, Is.Not.Null);
    }

    // ----------------------
    // CreateQuestAsync Tests
    // ----------------------

    [Test]
    public async Task CreateQuestAsync_WithValidQuest_ReturnsQuest()
    {
        _mockQuestRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Quest>>()))
            .Returns(Task.FromResult(_testQuest));

        var result = await _questService.CreateQuestAsync(_testQuest);

        Assert.That(result, Is.EqualTo(_testQuest));

        _mockQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Quest>>(q => q.Count() == 1 && q.First() == _testQuest)), Times.Once);
    }

    [Test]
    public void CreateQuestAsync_WithNullQuest_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _questService.CreateQuestAsync(null));

        Assert.That(ex.ParamName, Is.EqualTo("quest"));
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase(" ")]
    public void CreateQuestAsync_WithEmptyTitle_ThrowsArgumentException(string title)
    {
        var invalidQuest = new Quest { Title = title };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.CreateQuestAsync(invalidQuest));

        Assert.That(ex.ParamName, Is.EqualTo("quest"));
        Assert.That(ex.Message, Does.Contain("Quest title cannot be empty."));
    }

    [Test]
    public async Task CreateQuestAsync_WithMaxLengthTitle_Succeeds()
    {
        var maxLengthTitle = new string('A', 200);
        var validQuest = new Quest { Title = maxLengthTitle };

        _mockQuestRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Quest>>()))
            .Returns(Task.FromResult(validQuest));

        var result = await _questService.CreateQuestAsync(validQuest);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Title, Is.EqualTo(maxLengthTitle));

        _mockQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Quest>>(q => q.Count() == 1 && q.First() == validQuest)), Times.Once);
    }

    [Test]
    public void CreateQuestAsync_WithExceedingMaxLengthTitle_ThrowsArgumentException()
    {
        var exceedingLengthTitle = new string('A', 201);
        var invalidQuest = new Quest { Title = exceedingLengthTitle };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.CreateQuestAsync(invalidQuest));

        Assert.That(ex.ParamName, Is.EqualTo("quest"));
        Assert.That(ex.Message, Does.Contain("Quest title cannot exceed 200 characters."));
    }

    [Test]
    public void CreateQuestAsync_WithExceedingMaxLengthDescription_ThrowsArgumentException()
    {
        var exceedingLengthDescription = new string('A', 1001);
        var invalidQuest = new Quest
        {
            Title = _invalidTest,
            Description = exceedingLengthDescription
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.CreateQuestAsync(invalidQuest));

        Assert.That(ex.ParamName, Is.EqualTo("quest"));
        Assert.That(ex.Message, Does.Contain("Quest description cannot exceed 1000 characters."));
    }

    [TestCase(-1, 10, "RewardExperience")]
    [TestCase(10, -1, "RewardGold")]
    public void CreateQuestAsync_WithNegativeRewards_ThrowsArgumentException(int experience, int gold, string paramName)
    {
        var invalidQuest = new Quest
        {
            Title = _invalidTest,
            RewardExperience = experience,
            RewardGold = gold
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.CreateQuestAsync(invalidQuest));

        Assert.That(ex.ParamName, Is.EqualTo(paramName));
        Assert.That(ex.Message, Does.Contain("cannot be negative"));
    }

    [Test]
    public void CreateQuestAsync_WithInvalidRequiredLevel_ThrowsArgumentException()
    {
        var invalidQuest = new Quest
        {
            Title = _invalidTest,
            RequiredLevel = 0
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.CreateQuestAsync(invalidQuest));

        Assert.That(ex.ParamName, Is.EqualTo("quest"));
        Assert.That(ex.Message, Does.Contain("Required level must be at least 1"));
    }

    [TestCase(null!)]
    [TestCase("")]
    public async Task CreateQuestAsync_WithNullDifficulty_Succeeds(string difficulty)
    {
        var validQuest = new Quest 
        { 
            Title = _validTest,
            Difficulty = difficulty
        };

        _mockQuestRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Quest>>()))
            .Returns(Task.CompletedTask);

        var result = await _questService.CreateQuestAsync(validQuest);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Difficulty, Is.EqualTo(difficulty));

        _mockQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Quest>>(q => q.Count() == 1 && q.First() == validQuest)), Times.Once);
    }

    [TestCase("   ")]
    [TestCase("Invalid")]
    public void CreateQuestAsync_WithWhitespaceDifficulty_ThrowsArgumentException(string difficulty)
    {
        var invalidQuest = new Quest
        {
            Title = _invalidTest,
            Difficulty = difficulty
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.CreateQuestAsync(invalidQuest));

        Assert.That(ex.ParamName, Is.EqualTo("quest"));
        Assert.That(ex.Message, Does.Contain("Difficulty must be one of: Easy, Medium, Hard, Expert."));

        _mockQuestRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Quest>>()), Times.Never);
    }

    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public void CreateQuestAsync_WithZeroOrExtremelyHighRewards_CreatesQuest(int rewards)
    {
        var validQuest = new Quest
        {
            Title = _validTest,
            RewardExperience = rewards,
            RewardGold = rewards,
            RequiredLevel = 1,
            Difficulty = "Easy"
        };

        _mockQuestRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Quest>>()))
            .Returns(Task.FromResult(validQuest));

        Assert.DoesNotThrowAsync(async () => await _questService.CreateQuestAsync(validQuest));

        _mockQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Quest>>(q => q.Count() == 1 && q.First() == validQuest)), Times.Once);
    }

    // -----------------------
    // GetQuestByIdAsync Tests
    // -----------------------

    [Test]
    public async Task GetQuestByIdAsync_WithValidId_ReturnsQuest()
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(_testQuest.Id))
            .ReturnsAsync(_testQuest);

        var result = await _questService.GetQuestByIdAsync(_testQuest.Id);

        Assert.That(result, Is.EqualTo(_testQuest));

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(_testQuest.Id), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task GetQuestByIdAsync_WithNonExistingId_ReturnsNull(int id)
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Quest)null!);

        var result = await _questService.GetQuestByIdAsync(id);

        Assert.That(result, Is.Null);

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
    }

    // -----------------------------------
    // BulkInsertQuestsFromJsonAsync Tests
    // -----------------------------------

    [TestCase(null!)]
    [TestCase("")]
    [TestCase("   ")]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithInvalidPath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.BulkInsertQuestsFromJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertQuestsFromJsonAsync_WithNonExistingFilePath_ThrowsFileNotFoundException()
    {
        var nonExistingPath = "non_existing_file.json";

        var ex = Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _questService.BulkInsertQuestsFromJsonAsync(nonExistingPath));

        Assert.That(ex.Message, Does.Contain($"File not found: {nonExistingPath}"));
    }

    [Test]
    public void BulkInsertQuestsFromJsonAsync_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "invalid_quest.json";
        var invalidJsonContent = "{ invalid json }";

        File.WriteAllText(jsonFilePath, invalidJsonContent);

        var ex = Assert.ThrowsAsync<JsonReaderException>(
            async () => await _questService.BulkInsertQuestsFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("Invalid character"));

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertQuestsFromJsonAsync_WithValidJson_InsertsQuests()
    {
        var jsonContent = JsonConvert.SerializeObject(_testQuestList);
        var jsonFilePath = "test_quests.json";

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockQuestRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Quest>>()))
            .Returns(Task.FromResult(_testQuestList.First()));

        await _questService.BulkInsertQuestsFromJsonAsync(jsonFilePath);

        _mockQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Quest>>(q => q.Count() == _testQuestList.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertQuestsFromJsonAsync_WithEmptyJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "empty_quests.json";
        var emptyJsonContent = "[]";
        File.WriteAllText(jsonFilePath, emptyJsonContent);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _questService.BulkInsertQuestsFromJsonAsync(jsonFilePath));
        Assert.That(ex.Message, Does.Contain("No quests found in JSON file."));
        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertQuestsFromJsonAsync_WithEmptyQuestList_ThrowsArgumentException()
    {
        var jsonFilePath = "empty_quests.json";
        var emptyQuestList = new List<Quest>();
        var jsonContent = JsonConvert.SerializeObject(emptyQuestList);

        File.WriteAllText(jsonFilePath, jsonContent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _questService.BulkInsertQuestsFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("No quests found"));

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertQuestsFromJsonAsync_WithLargeQuestList_InsertsAllQuests()
    {
        var jsonFilePath = "large_quest.json";

        var largeQuestsList = Enumerable.Range(1, 1000)
            .Select(i => new Quest
            {
                Id = i,
                Title = $"Quest {i}",
                Description = $"Description for Quest {i}",
                RewardExperience = i * 10,
                RewardGold = i * 5,
                RequiredLevel = i % 50 + 1,
                Difficulty = (i % 4) switch
                {
                    0 => "Easy",
                    1 => "Medium",
                    2 => "Hard",
                    _ => "Expert"
                }
            }).ToList();

        var jsonContent = JsonConvert.SerializeObject(largeQuestsList);

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockQuestRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Quest>>()))
            .Returns(Task.FromResult(largeQuestsList.First()));

        await _questService.BulkInsertQuestsFromJsonAsync(jsonFilePath);

        _mockQuestRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Quest>>(q => q.Count() == largeQuestsList.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }


    // -----------------------
    // GetAllQuestsAsync Tests
    // -----------------------

    [Test]
    public async Task GetAllQuestsAsync_WithQuestsAvailable_ReturnsAllQuests()
    {
        _mockQuestRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(_testQuestList);

        var result = await _questService.GetAllQuestsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(_testQuestList.Count));

        _mockQuestRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllQuestsAsync_WithNoQuestsAvailable_ReturnsEmptyList()
    {
        var emptyQuests = new List<Quest>();

        _mockQuestRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(emptyQuests);

        var result = await _questService.GetAllQuestsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _mockQuestRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllQuestsAsync_WithLargeNumberOfQuests_ReturnsAllQuests()
    {
        var largeQuestsList = Enumerable.Range(1, 1000)
            .Select(i => new Quest
            {
                Id = i,
                Title = $"Quest {i}",
                Description = $"Description for Quest {i}",
                RewardExperience = i * 10,
                RewardGold = i * 5,
                RequiredLevel = i % 50 + 1,
                Difficulty = (i % 4) switch
                {
                    0 => "Easy",
                    1 => "Medium",
                    2 => "Hard",
                    _ => "Expert"
                }
            }).ToList();

        _mockQuestRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(largeQuestsList);

        var result = await _questService.GetAllQuestsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(largeQuestsList.Count));

        _mockQuestRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    // --------------------------------
    // GetQuestsByDifficultyAsync Tests
    // --------------------------------

    [Test]
    public async Task GetQuestsByDifficultyAsync_WithValidDifficulty_ReturnsQuests()
    {
        var testQuests = _testQuestList.Where(q => q.Difficulty == "Medium").ToList();

        _mockQuestRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Quest, bool>>>()))
            .ReturnsAsync(testQuests);

        var result = await _questService.GetQuestsByDifficultyAsync("Medium");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(testQuests.Count));
        Assert.That(result.All(q => q.Difficulty == "Medium"));

        _mockQuestRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Quest, bool>>>()), Times.Once);
    }

    [Test]
    public void GetQuestsByDifficultyAsync_WithNonExistingDifficultyy_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.GetQuestsByDifficultyAsync(_invalidTest));

        var errorMessage = $"Difficulty filter \'{_invalidTest}\' is invalid.";

        Assert.That(ex.ParamName, Is.EqualTo("difficulty"));
        Assert.That(ex.Message, Does.Contain(errorMessage));

        _mockQuestRepository.Verify(repo => repo.GetAllAsync(), Times.Never);
        _mockQuestRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Quest, bool>>>()), Times.Never);
    }

    [TestCase(null!)]
    [TestCase("")]
    public async Task GetQuestsByDifficultyAsync_WithNullOrEmptyDifficulty_ReturnsOnlyNullAndEmptyDifficultyQuests(string difficulty)
    {
        var expectedQuests = _testQuestList.Where(q => q.Difficulty == null || q.Difficulty == _emptyName).ToList();

        _mockQuestRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Quest, bool>>>()))
            .ReturnsAsync(expectedQuests);

        var result = await _questService.GetQuestsByDifficultyAsync(difficulty);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(expectedQuests.Count));
        Assert.That(result.All(q => string.IsNullOrEmpty(q.Difficulty)), Is.True);

        _mockQuestRepository.Verify(repo => repo.FindAsync(It.IsAny<Expression<Func<Quest, bool>>>()), Times.Once);
        _mockQuestRepository.Verify(repo => repo.GetAllAsync(), Times.Never);
    }

    [Test]
    public void GetQuestsByDifficultyAsync_WithWhitespaceDifficulty_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.GetQuestsByDifficultyAsync(_whitespaceName));

        Assert.That(ex.ParamName, Is.EqualTo("difficulty"));
        Assert.That(ex.Message, Does.Contain("Difficulty filter cannot be composed only of whitespace."));

        _mockQuestRepository.Verify(repo => repo.GetAllAsync(), Times.Never);
        _mockQuestRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Quest, bool>>>()), Times.Never);
    }

    // ---------------------
    // DeleteQestAsync Tests
    // ---------------------

    [Test]
    public async Task DeleteQuestAsync_WithValidId_ReturnsTrue()
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testQuest);
        _mockQuestRepository.Setup(repo => repo.DeleteAsync(_testQuest))
            .Returns(Task.CompletedTask);

        var result = await _questService.DeleteQuestAsync(1);

        Assert.That(result, Is.True);

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockQuestRepository.Verify(repo => repo.DeleteAsync(_testQuest), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task DeleteQuestAsync_WithNonExistentId_ReturnsFalse(int id)
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Quest)null!);

        var result = await _questService.DeleteQuestAsync(id);

        Assert.That(result, Is.False);

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        _mockQuestRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Quest>()), Times.Never);
    }

    // -----------------------------
    // ExportQuestsToJsonAsync Tests
    // -----------------------------

    [Test]
    public async Task ExportQuestsToJsonAsync_WithValidFilePath_ExportsQuests()
    {
        _mockQuestRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(_testQuestList);

        var outputFilePath = "test_quests_export.json";
        await _questService.ExportQuestsToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var jsonContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedQuests = JsonConvert.DeserializeObject<List<Quest>>(jsonContent);

        Assert.That(exportedQuests, Is.Not.Null);
        Assert.That(exportedQuests.Count, Is.EqualTo(_testQuestList.Count));

        File.Delete(outputFilePath);
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase("   ")]
    public void ExportQuestsToJsonAsync_WithInvalidFilePath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.ExportQuestsToJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("outputFilePath"));
        Assert.That(ex.Message, Does.Contain("Output file path cannot be empty."));
    }

    [Test]
    public async Task ExportQuestsToJsonAsync_WithNoQuests_ExportsEmptyList()
    {
        var emptyQuests = new List<Quest>();

        _mockQuestRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(emptyQuests);

        var jsonFilePath = "exported_empty_quests.json";
        await _questService.ExportQuestsToJsonAsync(jsonFilePath);

        Assert.That(File.Exists(jsonFilePath), Is.True);

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var exportedQuests = JsonConvert.DeserializeObject<List<Quest>>(jsonContent);

        Assert.That(exportedQuests, Is.Not.Null);
        Assert.That(exportedQuests.Count, Is.EqualTo(0));

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task ExportQuestsToJsonAsync_WithLargeNumberOfQuests_ExportsAllQuests()
    {
        var largeQuestsList = Enumerable.Range(1, 1000)
            .Select(i => new Quest
            {
                Id = i,
                Title = $"Quest {i}",
                Description = $"Description for Quest {i}",
                RewardExperience = i * 10,
                RewardGold = i * 5,
                RequiredLevel = i % 50 + 1,
                Difficulty = (i % 4) switch
                {
                    0 => "Easy",
                    1 => "Medium",
                    2 => "Hard",
                    _ => "Expert"
                }
            }).ToList();

        _mockQuestRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(largeQuestsList);

        var jsonFilePath = "exported_large_quests.json";
        await _questService.ExportQuestsToJsonAsync(jsonFilePath);

        Assert.That(File.Exists(jsonFilePath), Is.True);

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var exportedQuests = JsonConvert.DeserializeObject<List<Quest>>(jsonContent);

        Assert.That(exportedQuests, Is.Not.Null);
        Assert.That(exportedQuests.Count, Is.EqualTo(largeQuestsList.Count));

        File.Delete(jsonFilePath);
    }

    [Test]
    public void ExportQuestsToJsonAsync_WithInvalidFilePath_ThrowsDirectoryNotFoundException()
    {
        var invalidFilePath = "/root/exported_quests.json";

        var ex = Assert.ThrowsAsync<DirectoryNotFoundException>(
            async () => await _questService.ExportQuestsToJsonAsync(invalidFilePath));

        Assert.That(ex.Message, Does.Contain("Could not find"));
    }

    [Test]
    public async Task ExportQuestsToJsonAsync_WithNoMatchingFilters_ExportsEmptyArray()
    {
        _mockQuestRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Quest, bool>>>()))
            .ReturnsAsync(new List<Quest>());

        var outputFilePath = "test_quest_export_empty.json";
        await _questService.ExportQuestsToJsonAsync(outputFilePath, _validDifficulties[1]);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var jsonContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedQuests = JsonConvert.DeserializeObject<List<Quest>>(jsonContent);

        Assert.That(exportedQuests, Is.Not.Null);
        Assert.That(exportedQuests.Count, Is.EqualTo(0));

        File.Delete(outputFilePath);
    }

    [Test]
    public void ExportQuestsToJsonAsync_InvalidFilter_ThrowsArgumentException()
    {
        var outputFilePath = "test_quest_export_invalid_filter.json";

        Assert.ThrowsAsync<ArgumentException>(async () =>
        { await _questService.ExportQuestsToJsonAsync(outputFilePath, _invalidTest); },
        "The service should throw an ArgumentException for invalid difficulty filters.");

        Assert.That(File.Exists(outputFilePath), Is.False, "No file should be created when an ArgumentException is thrown.");
    }

    [Test]
    public async Task ExportQuestsToJsonAsync_WithValidFilter_ExportsFilteredQuests()
    {
        var quests = _testQuestList.Where(q => q.Difficulty == "Easy").ToList();

        _mockQuestRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Quest, bool>>>()))
            .ReturnsAsync(quests);

        var outputFilePath = "test_quests_export_filtered.json";
        await _questService.ExportQuestsToJsonAsync(outputFilePath, "Easy");

        Assert.That(File.Exists(outputFilePath), Is.True);

        var jsonContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedQuests = JsonConvert.DeserializeObject<List<Quest>>(jsonContent);

        Assert.That(exportedQuests, Is.Not.Null);
        Assert.That(exportedQuests.Count, Is.EqualTo(1));

        File.Delete(outputFilePath);
    }

    // -----------------------------
    // UpdateQuestRewardsAsync Tests
    // -----------------------------

    [Test]
    public async Task UpdateQuestRewardsAsync_WithValidIdAndRewards_UpdatesRewards()
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testQuest);
        _mockQuestRepository.Setup(repo => repo.UpdateAsync(_testQuest))
            .Returns(Task.CompletedTask);

        var result = await _questService.UpdateQuestRewardsAsync(1, 1500, 2500);

        Assert.That(result, Is.True);
        Assert.That(_testQuest.RewardExperience, Is.EqualTo(2500));
        Assert.That(_testQuest.RewardGold, Is.EqualTo(1500));

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockQuestRepository.Verify(repo => repo.UpdateAsync(_testQuest), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task UpdateQuestRewardsAsync_WithNonExistentId_ReturnsFalse(int id)
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Quest)null!);

        var result = await _questService.UpdateQuestRewardsAsync(id, 1500, 2500);

        Assert.That(result, Is.False);

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        _mockQuestRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Quest>()), Times.Never);
    }

    [TestCase(-1, 10, "newExperience")]
    [TestCase(10, -1, "newGold")]
    public void UpdateQuestRewardsAsync_WithNegativeRewards_ThrowsArgumentException(int experience, int gold, string paramName)
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testQuest);

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _questService.UpdateQuestRewardsAsync(1, gold, experience));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.ParamName, Is.EqualTo(paramName));
        Assert.That(ex.Message, Does.Contain("cannot be negative."));

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockQuestRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Quest>()), Moq.Times.Never);
    }

    [Test]
    public async Task UpdateQuestRewardsAsync_WithZeroRewards_UpdatesSuccessfully()
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testQuest);
        _mockQuestRepository.Setup(repo => repo.UpdateAsync(_testQuest))
            .Returns(Task.CompletedTask);

        var result = await _questService.UpdateQuestRewardsAsync(1, 0, 0);

        Assert.That(result, Is.True);
        Assert.That(_testQuest.RewardExperience, Is.EqualTo(0));
        Assert.That(_testQuest.RewardGold, Is.EqualTo(0));

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockQuestRepository.Verify(repo => repo.UpdateAsync(_testQuest), Times.Once);
    }

    [Test]
    public async Task UpdateQuestRewardsAsync_WithSameRewards_UpdatesSuccessfully()
    {
        _mockQuestRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testQuest);
        _mockQuestRepository.Setup(repo => repo.UpdateAsync(_testQuest))
            .Returns(Task.CompletedTask);

        var result = await _questService.UpdateQuestRewardsAsync(1, _testQuest.RewardGold, _testQuest.RewardExperience);

        Assert.That(result, Is.True);
        Assert.That(_testQuest.RewardGold, Is.EqualTo(1000));
        Assert.That(_testQuest.RewardExperience, Is.EqualTo(5000));

        _mockQuestRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockQuestRepository.Verify(repo => repo.UpdateAsync(_testQuest), Times.Once);
    }
}