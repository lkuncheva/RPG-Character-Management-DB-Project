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
public class EquipmentServiceTests
{
    private Mock<IRepository<Equipment>> _mockEquipmentRepository = null!;
    private EquipmentService _equipmentService = null!;
    private Equipment _testEquipment = null!;
    private List<Equipment> _testEquipmentList = null!;

    private string[] _validTypes = null!;
    private string[] _validRarities = null!;

    private string _emptyName;
    private string _whitespaceName;
    private string _invalidTest;
    private string _validTest;

    [SetUp]
    public void Setup()
    {
        _mockEquipmentRepository = new Mock<IRepository<Equipment>>();
        _equipmentService = new EquipmentService(_mockEquipmentRepository.Object);

        _testEquipment = new Equipment
        {
            Id = 1,
            Name = "Excalibur",
            Type = "Weapon",
            Rarity = "Rare",
            AttackBonus = 20,
            DefenseBonus = 10
        };

        _testEquipmentList = new List<Equipment> 
        {
            new Equipment { Id = 1, Name = "Equipment1", Type = "Armor", Rarity = "Common", AttackBonus = 0, DefenseBonus = 1},
            new Equipment { Id = 2, Name = "Equipment2", Type = "Weapon", Rarity = "Rare", AttackBonus = 5, DefenseBonus = 0},
            new Equipment { Id = 3, Name = "Equipment3", Type = "Accessory", Rarity = "Epic", AttackBonus = 15, DefenseBonus = 13},
            new Equipment { Id = 4, Name = "Equipment4", Type = "Armor", Rarity = "Legendary", AttackBonus = 19, DefenseBonus = 16},
            new Equipment { Id = 5, Name = "Equipment5", Type = "Weapon", Rarity = "Uncommon", AttackBonus = 7, DefenseBonus = 3},
            new Equipment { Id = 6, Name = "Equipment6", Type = "Armor", Rarity = "Rare", AttackBonus = 11, DefenseBonus = 8},
            new Equipment { Id = 7, Name = "NoRarityItemNull", Rarity = null!, AttackBonus = 0, DefenseBonus = 0 },
            new Equipment { Id = 8, Name = "NoRarityItemEmpty", Rarity = "", AttackBonus = 0, DefenseBonus = 0 }
        };

        _validTypes = new[] { "Armor", "Weapon", "Accessory" };
        _validRarities = new[] { "Common", "Rare", "Epic", "Legendary", "Uncommon" };

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
            () => new EquipmentService(null));

        Assert.That(ex.ParamName, Is.EqualTo("equipmentRepository"));
    }

    [Test]
    public void Constructor_WithValidRepository_CreatesInstance()
    {
        var service = new EquipmentService(_mockEquipmentRepository.Object);

        Assert.That(service, Is.Not.Null);
    }

    // --------------------------
    // CreateEquipmentAsync Tests
    // --------------------------

    [Test]
    public async Task CreateEquipmentAsync_WithValidEquipment_CallsAddRangeAsync()
    {
        _mockEquipmentRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(_testEquipment));

        var result = await _equipmentService.CreateEquipmentAsync(_testEquipment);

        Assert.That(result, Is.EqualTo(_testEquipment));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == 1 && e.First() == _testEquipment)), Times.Once);
    }

    [Test]
    public void CreateEquipmentAsync_WithNullEquipment_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _equipmentService.CreateEquipmentAsync(null));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
    }

    
    [TestCase(null!)]
    [TestCase("")]
    [TestCase(" ")]
    public void CreateEquipmentAsync_WithInvalidName_ThrowsArgumentException(string name)
    {
        var invalidEquipment = new Equipment { Name = name };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Equipment name cannot be empty."));
    }

    [Test]
    public async Task CreateEquipmentAsync_WithMaxLengthName_Succeeds()
    {
        var maxLengthName = new string('A', 100);
        var validEquipment = new Equipment { Name = maxLengthName };

        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(validEquipment));

        var result = await _equipmentService.CreateEquipmentAsync(validEquipment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(maxLengthName));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == 1 && e.First() == validEquipment)), Times.Once);
    }

    [Test]
    public void CreateEquipmentAsync_WithExceedingMaxLengthName_ThrowsArgumentException()
    {
        var exceedingLengthName = new string('A', 101);
        var invalidEquipment = new Equipment { Name = exceedingLengthName };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Equipment name cannot exceed 100 characters."));
    }

    [TestCase(null!)]
    [TestCase("")]
    public async Task CreateEquipmentAsync_WithNullOrEmptyType_Succeeds(string type)
    {
        var validEquipment = new Equipment 
        {
            Name = _validTest,
            Type = type
        };

        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.CompletedTask);

        var result = await _equipmentService.CreateEquipmentAsync(validEquipment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Type, Is.EqualTo(type));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == 1 && e.First() == validEquipment)), Times.Once);
    }

    [TestCase("   ")]
    [TestCase("Invalid")]
    public void CreateEquipmentAsync_WithWhitespaceType_ThrowsArgumentException(string type)
    {
        var invalidEquipment = new Equipment 
        {
            Name = _validTest,
            Type = type
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Type must be one of: Armor, Weapon, Accessory."));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()), Times.Never);
    }

    [TestCase(null!)]
    [TestCase("")]
    public async Task CreateEquipmentAsync_WithNullOrEmptyRarity_Succeeds(string rarity)
    {
        var validEquipment = new Equipment
        {
            Name = _validTest,
            Rarity = rarity
        };

        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.CompletedTask);

        var result = await _equipmentService.CreateEquipmentAsync(validEquipment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Rarity, Is.EqualTo(rarity));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == 1 && e.First() == validEquipment)), Times.Once);
    }

    [TestCase("   ")]
    [TestCase("Invalid")]
    public void CreateEquipmentAsync_WithInvalidRarity_ThrowsArgumentException(string rarity)
    {
        var invalidEquipment = new Equipment
        {
            Name = _validTest,
            Rarity = rarity
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Rarity must be one of: Common, Rare, Epic, Legendary, Uncommon."));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()), Times.Never);
    }

    [TestCase(-1, 10, "AttackBonus")]
    [TestCase(10, -1, "DefenseBonus")]
    public void CreateEquipmentAsync_WithNegativeBonuses_ThrowsArgumentException(int attackBonus, int defenceBonus, string paramName)
    {
        var invalidEquipment = new Equipment
        {
            Name = _validTest, 
            AttackBonus = attackBonus,
            DefenseBonus = defenceBonus
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo(paramName));
        Assert.That(ex.Message, Does.Contain("bonus cannot be negative."));
    }

    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public async Task CreateEquipmentAsync_WithZeroOrExtremelyHighBonuses_Succeeds(int bonus)
    {
        var validEquipment = new Equipment
        {
            Name = _validTest,
            AttackBonus = bonus,
            DefenseBonus = bonus
        };

        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(validEquipment));

        var result = await _equipmentService.CreateEquipmentAsync(validEquipment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(_validTest));
        Assert.That(result.AttackBonus, Is.EqualTo(bonus));
        Assert.That(result.DefenseBonus, Is.EqualTo(bonus));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == 1 && e.First() == validEquipment)), Times.Once);
    }

    // --------------------------------------
    // BulkInsertEquipmentFromJsonAsync Tests
    // --------------------------------------

    [Test]
    public async Task BulkInsertEquipmentFromJsonAsync_WithValidFile_InsertsEquipment()
    {
        var jsonFilePath = "test_equipment.json";
        var jsonContent = JsonConvert.SerializeObject(_testEquipmentList);

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockEquipmentRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(_testEquipmentList.First()));

        await _equipmentService.BulkInsertEquipmentFromJsonAsync(jsonFilePath);

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == _testEquipmentList.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertEquipmentFromJsonAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var jsonFilePath = "non_existent_file.json";

        var ex = Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _equipmentService.BulkInsertEquipmentFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("File not found"));
    }

    [Test]
    public void BulkInsertEquipmentFromJsonAsync_WithInvalidJson_ThrowsJsonException()
    {
        var jsonFilePath = "invalid_equipment.json";
        var invalidJsonContent = "{ invalid json }";

        File.WriteAllText(jsonFilePath, invalidJsonContent);

        var ex = Assert.ThrowsAsync<JsonReaderException>(
            async () => await _equipmentService.BulkInsertEquipmentFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("Invalid character"));

        File.Delete(jsonFilePath);
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase("   ")]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithInvalidPath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.BulkInsertEquipmentFromJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertEquipmentFromJsonAsync_WithEmptyEquipmentList_ThrowsArgumentException()
    {
        var jsonFilePath = "empty_equipment.json";
        var emptyEquipmentList = new List<Equipment>();
        var jsonContent = JsonConvert.SerializeObject(emptyEquipmentList);

        File.WriteAllText(jsonFilePath, jsonContent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _equipmentService.BulkInsertEquipmentFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("No equipment found"));
        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertEquipmentFromJsonAsync_WithLargeEquipmentList_InsertsAllEquipment()
    {
        var jsonFilePath = "large_equipment.json";

        var largeEquipmentList = Enumerable.Range(1, 1000)
            .Select(i => new Equipment
            {
                Id = i,
                Name = $"Equipment{i}",
                Type = _validTypes[i % _validTypes.Length],
                Rarity = _validRarities[i % _validRarities.Length],
                AttackBonus = i * 2,
                DefenseBonus = i * 3
            }).ToList();

        var jsonContent = JsonConvert.SerializeObject(largeEquipmentList);

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(largeEquipmentList.First()));

        await _equipmentService.BulkInsertEquipmentFromJsonAsync(jsonFilePath);

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == largeEquipmentList.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    // ---------------------------
    // GetEquipmentByIdAsync Tests
    // ---------------------------

    [Test]
    public async Task GetEquipmentByIdAsync_WithValidId_ReturnsEquipment()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(_testEquipment.Id))
            .ReturnsAsync(_testEquipment);

        var result = await _equipmentService.GetEquipmentByIdAsync(_testEquipment.Id);

        Assert.That(result, Is.EqualTo(_testEquipment));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
    }

    [TestCase(0)]
    public async Task GetEquipmentByIdAsync_WithNonExistingId_ReturnsNull(int id)
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Equipment)null!);

        var result = await _equipmentService.GetEquipmentByIdAsync(id);

        Assert.That(result, Is.Null);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
    }

    // --------------------------
    // GetAllEquipmentAsync Tests
    // --------------------------

    [Test]
    public async Task GetAllEquipmentAsync_WithEquipmentAvailable_ReturnsAllEquipment()
    {

        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(_testEquipmentList);

        var result = await _equipmentService.GetAllEquipmentAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(_testEquipmentList.Count));

        _mockEquipmentRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllEquipmentAsync_WithNoEquipmentAvailable_ReturnsEmptyList()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Equipment>());

        var result = await _equipmentService.GetAllEquipmentAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _mockEquipmentRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllEquipmentAsync_WithLargeNumberOfEquipment_ReturnsAllEquipment()
    {
        var largeEquipmentList = Enumerable.Range(1, 1000)
            .Select(i => new Equipment
            {
                Id = i,
                Name = $"Equipment{i}",
                Type = "Type" + (i % 5),
                Rarity = "Rarity" + (i % 4),
                AttackBonus = i * 2,
                DefenseBonus = i * 3
            }).ToList();

        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(largeEquipmentList);

        var result = await _equipmentService.GetAllEquipmentAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1000));

        _mockEquipmentRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    // -------------------------------
    // GetEquipmentByRarityAsync Tests
    // -------------------------------

    [Test]
    public async Task GetEquipmentByRarityAsync_WithExistingRarity_ReturnsEquipmentList()
    {
        var equipment = _testEquipmentList.Where(e => e.Rarity == "Rare").ToList();

        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment, bool>>>()))
            .ReturnsAsync(equipment);

        var result = await _equipmentService.GetEquipmentByRarityAsync("Rare");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(e => e.Rarity == "Rare"));

        _mockEquipmentRepository.Verify(repo => repo.FindAsync(It.IsAny<Expression<Func<Equipment, bool>>>()), Times.Once);
    }

    [Test]
    public void GetEquipmentByRarityAsync_WithNonExistingRarity_ThrowsArgumentException()
    {

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.GetEquipmentByRarityAsync(_invalidTest));

        var errorMessage = $"Rarity filter \'{_invalidTest}\' is invalid.";

        Assert.That(ex.ParamName, Is.EqualTo("rarity"));
        Assert.That(ex.Message, Does.Contain(errorMessage));

        _mockEquipmentRepository.Verify(repo => repo.GetAllAsync(), Times.Never);
        _mockEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment, bool>>>()), Times.Never);
    }

    [TestCase(null!)]
    [TestCase("")]
    public async Task GetEquipmentByRarityAsync_WithNullOrEmptyRarity_ReturnsOnlyNullAndEmptyRarityEquipment(string rarity)
    {
        var expectedEquipment = _testEquipmentList.Where(e => e.Rarity == null || e.Rarity == _emptyName).ToList();

        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment, bool>>>()))
            .ReturnsAsync(expectedEquipment);

        var result = await _equipmentService.GetEquipmentByRarityAsync(rarity);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(expectedEquipment.Count));
        Assert.That(result.All(e => string.IsNullOrEmpty(e.Rarity)), Is.True);

        _mockEquipmentRepository.Verify(repo => repo.FindAsync(It.IsAny<Expression<Func<Equipment, bool>>>()), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.GetAllAsync(), Times.Never);
    }

    [Test]
    public void GetEquipmentByRarityAsync_WithWhitespaceRarity_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.GetEquipmentByRarityAsync(_whitespaceName));

        Assert.That(ex.ParamName, Is.EqualTo("rarity"));
        Assert.That(ex.Message, Does.Contain("Rarity filter cannot be composed only of whitespace."));

        _mockEquipmentRepository.Verify(repo => repo.GetAllAsync(), Times.Never);
        _mockEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment, bool>>>()), Times.Never);
    }

    // --------------------------
    // DeleteEquipmentAsync Tests
    // --------------------------

    [Test]
    public async Task DeleteEquipmentAsync_WithValidId_ReturnsTrue()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testEquipment);
        _mockEquipmentRepository.Setup(repo => repo.DeleteAsync(_testEquipment))
            .Returns(Task.CompletedTask);

        var result = await _equipmentService.DeleteEquipmentAsync(1);

        Assert.That(result, Is.True);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.DeleteAsync(_testEquipment), Times.Once);
    }

    [TestCase(0)]
    public async Task DeleteEquipmentAsync_WithNonExistentId_ReturnsFalse(int id)
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Equipment)null!);

        var result = await _equipmentService.DeleteEquipmentAsync(id);

        Assert.That(result, Is.False);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Equipment>()), Times.Never);
    }

    // --------------------------------
    // ExportEquipmentToJsonAsync Tests
    // --------------------------------

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithValidParameters_ExportsSuccessfully()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(_testEquipmentList);

        var outputFilePath = "test_equipment_export.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedEquipment = JsonConvert.DeserializeObject<List<Quest>>(fileContent);

        Assert.That(exportedEquipment, Is.Not.Null);
        Assert.That(exportedEquipment.Count, Is.EqualTo(_testEquipmentList.Count));

        File.Delete(outputFilePath);
    }

    [Test]
    public void ExportQuestsToJsonAsync_WithInvalidFilePath_ThrowsDirectoryNotFoundException()
    {
        var invalidFilePath = "/root/exported_equipment.json";

        var ex = Assert.ThrowsAsync<DirectoryNotFoundException>(
            async () => await _equipmentService.ExportEquipmentToJsonAsync(invalidFilePath));

        Assert.That(ex.Message, Does.Contain("Could not find"));
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase("   ")]
    public void ExportCharactersToJsonAsync_WithInvalidFilePath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.ExportEquipmentToJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("outputFilePath"));
        Assert.That(ex.Message, Does.Contain("Output file path cannot be empty"));
    }

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithNoEquipment_ReturnsEmptyJsonArray()
    {
        var equipment = new List<Equipment>();

        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(equipment);

        var outputFilePath = "test_equipment_export_no_equipment.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedEquipment = JsonConvert.DeserializeObject<List<Quest>>(fileContent);

        Assert.That(exportedEquipment, Is.Not.Null);
        Assert.That(exportedEquipment.Count, Is.EqualTo(0));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithLargeNumberOfEquipment_ExportsSuccessfully()
    {
        var largeEquipmentList = Enumerable.Range(1, 1000)
            .Select(i => new Equipment
            {
                Id = i,
                Name = $"Equipment{i}",
                Type = "Type" + (i % 5),
                Rarity = "Rarity" + (i % 4),
                AttackBonus = i * 2,
                DefenseBonus = i * 3
            }).ToList();

        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(largeEquipmentList);

        var outputFilePath = "test_equipment_export_large_number.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedEquipment = JsonConvert.DeserializeObject<List<Quest>>(fileContent);

        Assert.That(exportedEquipment, Is.Not.Null);
        Assert.That(exportedEquipment.Count, Is.EqualTo(largeEquipmentList.Count));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithNoMatchingFilters_ExportsEmptyArray()
    {
        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment, bool>>>()))
            .ReturnsAsync(new List<Equipment>());

        var outputFilePath = "test_equipment_export_empty.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath, _validRarities[1]);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedEquipment = JsonConvert.DeserializeObject<List<Quest>>(fileContent);

        Assert.That(exportedEquipment, Is.Not.Null);
        Assert.That(exportedEquipment.Count, Is.EqualTo(0));

        File.Delete(outputFilePath);
    }

    [Test]
    public void ExportEquipmentToJsonAsync_InvalidRarityFilter_ThrowsArgumentException()
    {
        var outputFilePath = "test_equipment_export_invalid_filter.json";

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath, _invalidTest);
        }, "The service should throw an ArgumentException for invalid rarity filters.");

        Assert.That(File.Exists(outputFilePath), Is.False, "No file should be created when an ArgumentException is thrown.");
    }

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithValidFilter_ExportsFilteredEquipment()
    {
        var equipment = _testEquipmentList.Where(e => e.Rarity == "Rare").ToList();


        _mockEquipmentRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Equipment, bool>>>()))
            .ReturnsAsync(equipment);

        var outputFilePath = "test_equipment_export_filtered.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath, "Rare");

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedEquipment = JsonConvert.DeserializeObject<List<Quest>>(fileContent);

        Assert.That(exportedEquipment, Is.Not.Null);
        Assert.That(exportedEquipment.Count, Is.EqualTo(2));

        File.Delete(outputFilePath);
    }

    // ---------------------------------
    // UpdateEquipmentBonusesAsync Tests
    // ---------------------------------

    [Test]
    public async Task UpdateEquipmentBonusesAsync_WithValidIdAndBonuses_UpdatesSuccessfully()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testEquipment);
        _mockEquipmentRepository.Setup(repo => repo.UpdateAsync(_testEquipment))
            .Returns(Task.CompletedTask);

        var result = await _equipmentService.UpdateEquipmentBonusesAsync(1, 25, 15);

        Assert.That(result, Is.True);
        Assert.That(_testEquipment.AttackBonus, Is.EqualTo(25));
        Assert.That(_testEquipment.DefenseBonus, Is.EqualTo(15));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.UpdateAsync(_testEquipment), Times.Once);
    }

    [TestCase(0)]
    public async Task UpdateEquipmentBonusesAsync_WithNonExistentId_ReturnsFalse(int id)
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Equipment)null!);

        var result = await _equipmentService.UpdateEquipmentBonusesAsync(id, 10, 5);

        Assert.That(result, Is.False);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Equipment>()), Times.Never);
    }

    [TestCase(-1, 10, "newAttackBonus")]
    [TestCase(10, -1, "newDefenceBonus")]
    public void UpdateEquipmentBonusesAsync_WithNegativeBonuses_ThrowsArgumentException(int attackBonus, int defenceBonus, string paramName)
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testEquipment);

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.UpdateEquipmentBonusesAsync(1, attackBonus, defenceBonus));

        Assert.That(ex.ParamName, Is.EqualTo(paramName));
        Assert.That(ex.Message, Does.Contain("bonus cannot be negative."));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Equipment>()), Times.Never);
    }

    [Test]
    public async Task UpdateEquipmentBonusesAsync_WithZeroBonuses_UpdatesSuccessfully()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testEquipment);
        _mockEquipmentRepository.Setup(repo => repo.UpdateAsync(_testEquipment))
            .Returns(Task.CompletedTask);

        var result = await _equipmentService.UpdateEquipmentBonusesAsync(1, 0, 0);

        Assert.That(result, Is.True);
        Assert.That(_testEquipment.AttackBonus, Is.EqualTo(0));
        Assert.That(_testEquipment.DefenseBonus, Is.EqualTo(0));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.UpdateAsync(_testEquipment), Times.Once);
    }

    [Test]
    public async Task UpdateEquipmentBonusesAsync_WithSameBonuses_UpdatesSuccessfully()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testEquipment);
        _mockEquipmentRepository.Setup(repo => repo.UpdateAsync(_testEquipment))
            .Returns(Task.CompletedTask);

        var result = await _equipmentService.UpdateEquipmentBonusesAsync(1, _testEquipment.AttackBonus, _testEquipment.DefenseBonus);

        Assert.That(result, Is.True);
        Assert.That(_testEquipment.AttackBonus, Is.EqualTo(20));
        Assert.That(_testEquipment.DefenseBonus, Is.EqualTo(10));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.UpdateAsync(_testEquipment), Times.Once);
    }
}