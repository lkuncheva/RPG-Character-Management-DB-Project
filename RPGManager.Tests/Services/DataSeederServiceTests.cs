using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RPGManager.Data.Interfaces;
using RPGManager.Data.Models;
using RPGManager.Interfaces;
using RPGManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RPGManager.Tests.Services;

[TestFixture]
public class DataSeederServiceTests
{
    private Mock<IRepository<CharacterClass>> _mockCharacterClassRepository = null!;
    private Mock<ICharacterService> _mockCharacterService = null!;
    private Mock<IQuestService> _mockQuestService = null!;
    private Mock<IEquipmentService> _mockEquipmentService = null!;
    private Mock<ICharacterStatsService> _mockCharacterStatsService = null!;
    private Mock<ICharacterQuestService> _mockCharacterQuestService = null!;
    private Mock<ICharacterEquipmentService> _mockCharacterEquipmentService = null!;

    private DataSeederService _dataSeederService = null!;

    private string _sampleDataDir = null!;
    private const string ClassFileName = "character_classes.json";
    private const string CharacterFileName = "characters.json";
    private const string QuestFileName = "quests.json";
    private const string EquipmentFileName = "equipment.json";
    private const string CharacterStatsFileName = "character_stats.json";
    private const string CharacterQuestFileName = "character_quests.json";
    private const string CharacterEquipmentFileName = "character_equipment.json";

    [SetUp]
    public void Setup()
    {
        _mockCharacterClassRepository = new Mock<IRepository<CharacterClass>>();
        _mockCharacterService = new Mock<ICharacterService>();
        _mockQuestService = new Mock<IQuestService>();
        _mockEquipmentService = new Mock<IEquipmentService>();
        _mockCharacterStatsService = new Mock<ICharacterStatsService>();
        _mockCharacterQuestService = new Mock<ICharacterQuestService>();
        _mockCharacterEquipmentService = new Mock<ICharacterEquipmentService>();

        _dataSeederService = new DataSeederService(
            _mockCharacterClassRepository.Object,
            _mockCharacterService.Object,
            _mockQuestService.Object,
            _mockEquipmentService.Object,
            _mockCharacterEquipmentService.Object,
            _mockCharacterQuestService.Object,
            _mockCharacterStatsService.Object
            );

        _sampleDataDir = Path.Combine(Directory.GetCurrentDirectory(), "SampleData");

        Directory.CreateDirectory(_sampleDataDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_sampleDataDir))
        {
            Directory.Delete(_sampleDataDir, true);
        }
    }

    // -----------------
    // Constructor Tests
    // -----------------

    [Test]
    public void Constructor_NullDependency_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DataSeederService(null!, _mockCharacterService.Object, _mockQuestService.Object, _mockEquipmentService.Object, _mockCharacterEquipmentService.Object, _mockCharacterQuestService.Object,_mockCharacterStatsService.Object));
        Assert.Throws<ArgumentNullException>(() => new DataSeederService(_mockCharacterClassRepository.Object, null!, _mockQuestService.Object, _mockEquipmentService.Object, _mockCharacterEquipmentService.Object, _mockCharacterQuestService.Object, _mockCharacterStatsService.Object));
        Assert.Throws<ArgumentNullException>(() => new DataSeederService(_mockCharacterClassRepository.Object, _mockCharacterService.Object, null!, _mockEquipmentService.Object, _mockCharacterEquipmentService.Object, _mockCharacterQuestService.Object, _mockCharacterStatsService.Object));
        Assert.Throws<ArgumentNullException>(() => new DataSeederService(_mockCharacterClassRepository.Object, _mockCharacterService.Object, _mockQuestService.Object, null!, _mockCharacterEquipmentService.Object, _mockCharacterQuestService.Object, _mockCharacterStatsService.Object));
        Assert.Throws<ArgumentNullException>(() => new DataSeederService(_mockCharacterClassRepository.Object, _mockCharacterService.Object, _mockQuestService.Object, _mockEquipmentService.Object, null!, _mockCharacterQuestService.Object, _mockCharacterStatsService.Object));
        Assert.Throws<ArgumentNullException>(() => new DataSeederService(_mockCharacterClassRepository.Object, _mockCharacterService.Object, _mockQuestService.Object, _mockEquipmentService.Object, _mockCharacterEquipmentService.Object, null!, _mockCharacterStatsService.Object));
        Assert.Throws<ArgumentNullException>(() => new DataSeederService(_mockCharacterClassRepository.Object, _mockCharacterService.Object, _mockQuestService.Object, _mockEquipmentService.Object, _mockCharacterEquipmentService.Object, _mockCharacterQuestService.Object, null!));
    }

    // -------------------------------
    // SeedCharacterClassesAsync Tests
    // -------------------------------

    [Test]
    public async Task SeedCharacterClassesAsync_NoExistingData_InsertsClasses()
    {
        var classesToSeed = new List<CharacterClass>
        {
            new CharacterClass { Id = 1, Name = "Warrior" },
            new CharacterClass { Id = 2, Name = "Mage" }
        };
        var jsonContent = JsonConvert.SerializeObject(classesToSeed);
        var filePath = Path.Combine(_sampleDataDir, ClassFileName);

        await File.WriteAllTextAsync(filePath, jsonContent);

        _mockCharacterClassRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<CharacterClass>());

        await _dataSeederService.SeedCharacterClassesAsync();

        _mockCharacterClassRepository.Verify(
            repo => repo.AddRangeAsync(It.Is<IEnumerable<CharacterClass>>(c => c.Count() == 2)),
            Times.Once);
    }

    [Test]
    public async Task SeedCharacterClassesAsync_ExistingData_SkipsInsert()
    {
        _mockCharacterClassRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<CharacterClass> { new CharacterClass { Id = 1, Name = "Existing" } });

        await _dataSeederService.SeedCharacterClassesAsync();

        _mockCharacterClassRepository.Verify(
            repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterClass>>()),
            Times.Never);
    }

    [Test]
    public async Task SeedCharacterClassesAsync_FileDoesNotExist_SkipsInsertGracefully()
    {
        _mockCharacterClassRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<CharacterClass>());

        await _dataSeederService.SeedCharacterClassesAsync();

        _mockCharacterClassRepository.Verify(
            repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterClass>>()),
            Times.Never);
    }

    [Test]
    public async Task SeedCharacterClassesAsync_EmptyJsonFile_SkipsInsertGracefully()
    {
        var filePath = Path.Combine(_sampleDataDir, ClassFileName);
        await File.WriteAllTextAsync(filePath, "[]");

        _mockCharacterClassRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<CharacterClass>());

        await _dataSeederService.SeedCharacterClassesAsync();

        _mockCharacterClassRepository.Verify(
            repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterClass>>()),
            Times.Never);
    }

    // ----------------------------
    // SeedAllSampleDataAsync Tests
    // ----------------------------

    [Test]
    public async Task SeedAllSampleDataAsync_AllFilesExist_CallsAllBulkInserts()
    {
        _mockCharacterClassRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<CharacterClass>());
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, ClassFileName), "[{\"Id\": 1, \"Name\": \"Test\"}]");

        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, CharacterFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, QuestFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, EquipmentFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, CharacterStatsFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, CharacterQuestFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, CharacterEquipmentFileName), "[]");

        _mockCharacterService.Setup(s => s.BulkInsertCharactersFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockQuestService.Setup(s => s.BulkInsertQuestsFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEquipmentService.Setup(s => s.BulkInsertEquipmentFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockCharacterStatsService.Setup(s => s.BulkInsertCharacterStatsFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockCharacterQuestService.Setup(s => s.BulkInsertCharacterQuestsFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockCharacterEquipmentService.Setup(s => s.BulkInsertCharacterEquipmentFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await _dataSeederService.SeedAllSampleDataAsync();

        _mockCharacterClassRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterClass>>()), Times.Once);

        _mockCharacterService.Verify(s => s.BulkInsertCharactersFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockQuestService.Verify(s => s.BulkInsertQuestsFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockEquipmentService.Verify(s => s.BulkInsertEquipmentFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockCharacterStatsService.Verify(s => s.BulkInsertCharacterStatsFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockCharacterQuestService.Verify(s => s.BulkInsertCharacterQuestsFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockCharacterEquipmentService.Verify(s => s.BulkInsertCharacterEquipmentFromJsonAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SeedAllSampleDataAsync_FilesMissing_ContinuesSeedingOtherFiles()
    {
        _mockCharacterClassRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<CharacterClass>());
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, ClassFileName), "[]");

        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, QuestFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, EquipmentFileName), "[]");

        _mockCharacterService.Setup(s => s.BulkInsertCharactersFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockQuestService.Setup(s => s.BulkInsertQuestsFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEquipmentService.Setup(s => s.BulkInsertEquipmentFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await _dataSeederService.SeedAllSampleDataAsync();

        _mockCharacterService.Verify(s => s.BulkInsertCharactersFromJsonAsync(It.IsAny<string>()), Times.Never);
        _mockQuestService.Verify(s => s.BulkInsertQuestsFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockEquipmentService.Verify(s => s.BulkInsertEquipmentFromJsonAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SeedAllSampleDataAsync_CharacterSeedingFails_ContinuesToQuestSeeding()
    {
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, ClassFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, CharacterFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, QuestFileName), "[]");
        await File.WriteAllTextAsync(Path.Combine(_sampleDataDir, EquipmentFileName), "[]");

        _mockCharacterService
            .Setup(s => s.BulkInsertCharactersFromJsonAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Character Service Failed."));

        _mockQuestService.Setup(s => s.BulkInsertQuestsFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEquipmentService.Setup(s => s.BulkInsertEquipmentFromJsonAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        Assert.DoesNotThrowAsync(async () => await _dataSeederService.SeedAllSampleDataAsync());

        _mockCharacterService.Verify(s => s.BulkInsertCharactersFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockQuestService.Verify(s => s.BulkInsertQuestsFromJsonAsync(It.IsAny<string>()), Times.Once);
        _mockEquipmentService.Verify(s => s.BulkInsertEquipmentFromJsonAsync(It.IsAny<string>()), Times.Once);
    }
}