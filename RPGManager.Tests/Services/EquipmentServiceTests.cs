using Moq;
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
using Newtonsoft.Json;

namespace RPGManager.Tests.Services;

[TestFixture]
public class EquipmentServiceTests
{
    private Mock<IRepository<Equipment?>> _mockEquipmentRepository;
    private EquipmentService _equipmentService;
    private Equipment _testEquipment;

    [SetUp]
    public void Setup()
    {
        _mockEquipmentRepository = new Mock<IRepository<Equipment?>>();
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
    }

    private List<Equipment> GetTestEquipment() => new List<Equipment>
    {
        new Equipment { Id = 1, Name = "Equipment1", Type = "Armor", Rarity = "Common", AttackBonus = 0, DefenseBonus = 1},
        new Equipment { Id = 2, Name = "Equipment2", Type = "Weapon", Rarity = "Rare", AttackBonus = 5, DefenseBonus = 0},
        new Equipment { Id = 3, Name = "Equipment3", Type = "Accessory", Rarity = "Epic", AttackBonus = 15, DefenseBonus = 13},
        new Equipment { Id = 4, Name = "Equipment4", Type = "Armor", Rarity = "Legendary", AttackBonus = 19, DefenseBonus = 16},
        new Equipment { Id = 5, Name = "Equipment5", Type = "Weapon", Rarity = "Uncommon", AttackBonus = 7, DefenseBonus = 3},
        new Equipment { Id = 6, Name = "Equipment6", Type = "Armor", Rarity = "Rare", AttackBonus = 11, DefenseBonus = 8}
    };

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
        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(_testEquipment));

        var result = await _equipmentService.CreateEquipmentAsync(_testEquipment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Excalibur"));
        Assert.That(result.Type, Is.EqualTo("Weapon"));

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

    [Test]
    public void CreateEquipmentAsync_WithNullName_ThrowsArgumentException()
    {
        var invalidEquipment = new Equipment { Name = null };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Equipment name cannot be empty."));
    }

    [Test]
    public void CreateEquipmentAsync_WithEmptyName_ThrowsArgumentException()
    {
        var invalidEquipment = new Equipment{ Name = "" };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Equipment name cannot be empty."));
    }

    [Test]
    public void CreateEquipmentAsync_WithWhitespaceName_ThrowsArgumentException()
    {
        var invalidEquipment = new Equipment { Name = "   " };

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

    [Test]
    public void CreateEquipmentAsync_WithNegativeAttackBonus_ThrowsArgumentException()
    {
        var invalidEquipment = new Equipment{ Name = "Sword", AttackBonus = -5};

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Attack bonus cannot be negative."));
    }

    [Test]
    public void CreateEquipmentAsync_WithNegativeDefenceBonus_ThrowsArgumentException()
    {
        var invalidEquipment = new Equipment { Name = "Sword", DefenseBonus = -2 };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.CreateEquipmentAsync(invalidEquipment));

        Assert.That(ex.ParamName, Is.EqualTo("equipment"));
        Assert.That(ex.Message, Does.Contain("Defense bonus cannot be negative."));
    }

    [Test]
    public async Task CreateEquipmentAsync_WithZeroAttackAndDefenseBonus_Succeeds()
    {
        var validEquipment = new Equipment { Name = "Basic Sword", AttackBonus = 0, DefenseBonus = 0 };

        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(validEquipment));

        var result = await _equipmentService.CreateEquipmentAsync(validEquipment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Basic Sword"));
        Assert.That(result.AttackBonus, Is.EqualTo(0));
        Assert.That(result.DefenseBonus, Is.EqualTo(0));

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == 1 && e.First() == validEquipment)), Times.Once);
    }

    [Test]
    public async Task CreateEquipmentAsync_WithExtremelyHighAttackAndDefenseBonus_Succeeds()
    {
        var validEquipment = new Equipment { Name = "Ultimate Sword", AttackBonus = int.MaxValue, DefenseBonus = int.MaxValue };
        
        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(validEquipment));

        var result = await _equipmentService.CreateEquipmentAsync(validEquipment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Ultimate Sword"));
        Assert.That(result.AttackBonus, Is.EqualTo(int.MaxValue));
        Assert.That(result.DefenseBonus, Is.EqualTo(int.MaxValue));

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
        var equipmentList = GetTestEquipment();
        var jsonContent = System.Text.Json.JsonSerializer.Serialize(equipmentList);

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockEquipmentRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Equipment>>()))
            .Returns(Task.FromResult(equipmentList.First()));

        await _equipmentService.BulkInsertEquipmentFromJsonAsync(jsonFilePath);

        _mockEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Equipment>>(e => e.Count() == equipmentList.Count)), Times.Once);

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

    [Test]
    public void BulkInsertEquipmentFromJsonAsync_WithNullFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.BulkInsertEquipmentFromJsonAsync(null!));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertEquipmentFromJsonAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.BulkInsertEquipmentFromJsonAsync(""));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertEquipmentFromJsonAsync_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.BulkInsertEquipmentFromJsonAsync("   "));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertEquipmentFromJsonAsync_WithEmptyEquipmentList_ThrowsArgumentException()
    {
        var jsonFilePath = "empty_equipment.json";
        var emptyEquipmentList = new List<Equipment>();
        var jsonContent = System.Text.Json.JsonSerializer.Serialize(emptyEquipmentList);

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
                Type = "Type" + (i % 5),
                Rarity = "Rarity" + (i % 4),
                AttackBonus = i * 2,
                DefenseBonus = i * 3
            }).ToList();

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(largeEquipmentList);

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
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testEquipment);

        var result = await _equipmentService.GetEquipmentByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("Excalibur"));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByIdAsync_WithExistingId_ReturnsEquipment()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testEquipment);

        var result = await _equipmentService.GetEquipmentByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("Excalibur"));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByIdAsync_WithNonExistingId_ReturnsNull()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Equipment?)null);

        var result = await _equipmentService.GetEquipmentByIdAsync(999);

        Assert.That(result, Is.Null);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByIdAsync_WithNegativeId_ReturnsNull()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(-5))
            .ReturnsAsync((Equipment?)null);

        var result = await _equipmentService.GetEquipmentByIdAsync(-5);

        Assert.That(result, Is.Null);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(-5), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByIdAsync_WithZeroId_ReturnsNul()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(0))
            .ReturnsAsync((Equipment?)null);

        var result = await _equipmentService.GetEquipmentByIdAsync(0);

        Assert.That(result, Is.Null);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(0), Times.Once);
    }

    // --------------------------
    // GetAllEquipmentAsync Tests
    // --------------------------

    [Test]
    public async Task GetAllEquipmentAsync_WithEquipmentAvailable_ReturnsAllEquipment()
    {
        var equipment = GetTestEquipment();

        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(equipment);

        var result = await _equipmentService.GetAllEquipmentAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(6));

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
        var equipment = GetTestEquipment().Where(e => e.Rarity == "Rare").ToList();

        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()))
            .ReturnsAsync(equipment);

        var result = await _equipmentService.GetEquipmentByRarityAsync("Rare");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(e => e.Rarity == "Rare"));

        _mockEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByRarityAsync_WithNonExistingRarity_ReturnsEmptyList()
    {
        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()))
            .ReturnsAsync(new List<Equipment>());

        var result = await _equipmentService.GetEquipmentByRarityAsync("Mythic");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _mockEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByRarityAsync_WithNullRarity_ReturnsNull()
    {
        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()))
            .Returns(Task.FromResult<IEnumerable<Equipment?>>(null!));


        var result = await _equipmentService.GetEquipmentByRarityAsync(null);

        Assert.That(result, Is.Null);

        _mockEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByRarityAsync_WithEmptyRarity_ReturnsNull()
    {
        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()))
            .Returns(Task.FromResult<IEnumerable<Equipment?>>(null!));

        var result = await _equipmentService.GetEquipmentByRarityAsync("");

        Assert.That(result, Is.Null);

        _mockEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetEquipmentByRarityAsync_WithWhitespaceRarity_ThrowsArgumentException()
    {
        _mockEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()))
            .Returns(Task.FromResult<IEnumerable<Equipment?>>(null!));

        var result = await _equipmentService.GetEquipmentByRarityAsync("   ");

        Assert.That(result, Is.Null);

        _mockEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<Equipment?, bool>>>()), Times.Once);
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

    [Test]
    public async Task DeleteEquipmentAsync_WithNonExistentId_ReturnsFalse()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Equipment?)null);

        var result = await _equipmentService.DeleteEquipmentAsync(999);

        Assert.That(result, Is.False);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Equipment>()), Times.Never);
    }

    // --------------------------------
    // ExportEquipmentToJsonAsync Tests
    // --------------------------------

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithValidParameters_ExportsSuccessfully()
    {
        var equipment = GetTestEquipment();

        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(equipment);

        var outputFilePath = "test_equipment_export.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent, Does.Contain("Equipment1"));
        Assert.That(fileContent, Does.Contain("Equipment2"));

        File.Delete(outputFilePath);
    }

    [Test]
    public void ExportCharactersToJsonAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.ExportEquipmentToJsonAsync(""));

        Assert.That(ex.ParamName, Is.EqualTo("outputFilePath"));
        Assert.That(ex.Message, Does.Contain("Output file path cannot be empty"));
    }

    [Test]
    public void ExportCharactersToJsonAsync_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.ExportEquipmentToJsonAsync("   "));

        Assert.That(ex.ParamName, Is.EqualTo("outputFilePath"));
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

        Assert.That(fileContent, Is.EqualTo("[]"));

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

        Assert.That(fileContent.Length, Is.GreaterThan(0));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithNoMatchingFilters_ExportsEmptyArray()
    {
        var equipment = GetTestEquipment();

        _mockEquipmentRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(equipment);

        var outputFilePath = "test_characters_export_empty.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath, "Mythic");

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent, Is.EqualTo("[]"));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportEquipmentToJsonAsync_WithFilter_ExportsFilteredCharacters()
    {
        var equipment = GetTestEquipment().Where(e => e.Rarity == "Rare").ToList();


        _mockEquipmentRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Equipment?, bool>>>()))
            .ReturnsAsync(equipment);

        var outputFilePath = "test_characters_export_filtered.json";
        await _equipmentService.ExportEquipmentToJsonAsync(outputFilePath, "Rare");

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent, Does.Contain("Equipment2"));
        Assert.That(fileContent, Does.Contain("Equipment6"));
        Assert.That(fileContent, Does.Not.Contain("Equipment1"));
        Assert.That(fileContent, Does.Not.Contain("Equipment3"));
        Assert.That(fileContent, Does.Not.Contain("Equipment4"));
        Assert.That(fileContent, Does.Not.Contain("Equipment5"));

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

    [Test]
    public async Task UpdateEquipmentBonusesAsync_WithNonExistentId_ReturnsFalse()
    {
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Equipment?)null);

        var result = await _equipmentService.UpdateEquipmentBonusesAsync(999, 10, 5);

        Assert.That(result, Is.False);

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Equipment>()), Times.Never);
    }

    [Test]
    public void UpdateEquipmentBonusesAsync_WithNegativeAttackBonus_ThrowsArgumentException()
    {
        var existingEquipment = GetTestEquipment().First();
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingEquipment);

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.UpdateEquipmentBonusesAsync(1, -10, 5));

        Assert.That(ex.ParamName, Is.EqualTo("newAttackBonus"));
        Assert.That(ex.Message, Does.Contain("Attack bonus cannot be negative."));

        _mockEquipmentRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockEquipmentRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Equipment>()), Times.Never);
    }

    [Test]
    public void UpdateEquipmentBonusesAsync_WithNegativeDefenseBonus_ThrowsArgumentException()
    {
        var existingEquipment = GetTestEquipment().First();
        _mockEquipmentRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(existingEquipment);

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _equipmentService.UpdateEquipmentBonusesAsync(1, 10, -5));

        Assert.That(ex.ParamName, Is.EqualTo("newDefenceBonus"));
        Assert.That(ex.Message, Does.Contain("Defense bonus cannot be negative."));

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