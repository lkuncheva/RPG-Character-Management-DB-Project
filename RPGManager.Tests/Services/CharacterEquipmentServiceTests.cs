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
public class CharacterEquipmentServiceTests
{
    private Mock<ICharacterRepository> _mockCharacterRepository = null!;
    private Mock<IRepository<CharacterEquipment>> _mockCharacterEquipmentRepository = null!;
    private Mock<IRepository<Equipment>> _mockEquipmentRepository = null!;
    private CharacterEquipmentService _characterEquipmentService = null!;

    private Character _testCharacter = null!;
    private Equipment _testEquipment = null!;
    private CharacterEquipment _testAssignment = null!;

    private const int ValidCharacterId = 10;
    private const int ValidEquipmentId = 20;
    private const int NonExistentId = 99;

    private string _emptyName;
    private string _whitespaceName;

    [SetUp]
    public void Setup()
    {
        _mockCharacterRepository = new Mock<ICharacterRepository>();
        _mockCharacterEquipmentRepository = new Mock<IRepository<CharacterEquipment>>();
        _mockEquipmentRepository = new Mock<IRepository<Equipment>>();

        _characterEquipmentService = new CharacterEquipmentService(
            _mockCharacterRepository.Object,
            _mockCharacterEquipmentRepository.Object,
            _mockEquipmentRepository.Object);

        _testCharacter = new Character
        {
            Id = ValidCharacterId,
            Name = "Anya",
            Level = 15,
            Gold = 100,
            Experience = 500
        };

        _testEquipment = new Equipment
        {
            Id = ValidEquipmentId,
            Name = "Sword of Testing",
            Type = "Weapon",
            Rarity = "Epic",
            AttackBonus = 25,
            DefenseBonus = 5
        };

        _testAssignment = new CharacterEquipment
        {
            CharacterId = ValidCharacterId,
            EquipmentId = ValidEquipmentId,
            IsEquipped = false
        };

        _emptyName = "";
        _whitespaceName = "   ";

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(ValidCharacterId))
            .ReturnsAsync(_testCharacter);
        _mockEquipmentRepository
            .Setup(repo => repo.GetByIdAsync(ValidEquipmentId))
            .ReturnsAsync(_testEquipment);
        _mockCharacterEquipmentRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterEquipment, bool>>>()))
            .ReturnsAsync(new List<CharacterEquipment>());
    }

    //  -----------------
    //  Constructor Tests
    //  -----------------

    [Test]
    public void Constructor_WithNullCharacterRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterEquipmentService(null, _mockCharacterEquipmentRepository.Object, _mockEquipmentRepository.Object));

        Assert.That(ex.ParamName, Is.EqualTo("characterRepository"));
    }

    [Test]
    public void Constructor_WithNullCharacterEquipmentRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterEquipmentService(_mockCharacterRepository.Object, null, _mockEquipmentRepository.Object));

        Assert.That(ex.ParamName, Is.EqualTo("characterEquipmentRepository"));
    }

    [Test]
    public void Constructor_WithNullEquipmentRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterEquipmentService(_mockCharacterRepository.Object, _mockCharacterEquipmentRepository.Object, null));

        Assert.That(ex.ParamName, Is.EqualTo("equipmentRepository"));
    }

    [Test]
    public void Constructor_WithValidRepository_CreatesInstance()
    {
        var service = new CharacterEquipmentService(
            _mockCharacterRepository.Object,
            _mockCharacterEquipmentRepository.Object,
            _mockEquipmentRepository.Object);

        Assert.That(service, Is.Not.Null);
    }

    // ------------------------------
    //  GetCharacterEquipmentAsync Tests
    // ------------------------------

    [Test]
    public async Task GetCharacterEquipmentAsync_WithExistingCharacterAndEquipment_ReturnsEquipment()
    {
        var expectedEquipment = new List<CharacterEquipment> { _testAssignment };

        _mockCharacterEquipmentRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterEquipment, bool>>>()))
            .ReturnsAsync(expectedEquipment);

        var result = await _characterEquipmentService.GetCharacterEquipmentAsync(ValidCharacterId);

        Assert.That(result, Is.EqualTo(expectedEquipment));
        _mockCharacterEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterEquipment, bool>>>()), Times.Once);
    }

    [Test]
    public void GetCharacterEquipmentAsync_WithNonExistingCharacter_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(NonExistentId))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.GetCharacterEquipmentAsync(NonExistentId),
            $"Character with ID {NonExistentId} not found.");
    }

    [Test]
    public async Task GetCharacterEquipmentAsync_WhenNoEquipmentExist_ReturnsNull()
    {
        var result = await _characterEquipmentService.GetCharacterEquipmentAsync(ValidCharacterId);

        Assert.That(result, Is.Empty);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(ValidCharacterId), Times.Once);
        _mockCharacterEquipmentRepository.Verify(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterEquipment, bool>>>()), Times.Once);
    }

    // ----------------------------------
    //  AssignEquipmentToCharacterAsync Tests
    // ----------------------------------

    [Test]
    public async Task AssignEquipmentToCharacterAsync_ValidInput_AssignsEquipment()
    {
        _mockCharacterEquipmentRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterEquipment>>()))
            .Returns(Task.CompletedTask);

        var result = await _characterEquipmentService.AssignEquipmentToCharacterAsync(ValidCharacterId, ValidEquipmentId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CharacterId, Is.EqualTo(ValidCharacterId));
        Assert.That(result.EquipmentId, Is.EqualTo(ValidEquipmentId));
        Assert.That(result.IsEquipped, Is.EqualTo(false));

        _mockCharacterEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterEquipment>>(list => list.Count() == 1)), Times.Once);
    }

    [Test]
    public void AssignEquipmentToCharacterAsync_CharacterNotFound_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(ValidCharacterId))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.AssignEquipmentToCharacterAsync(ValidCharacterId, ValidEquipmentId),
            $"Character with ID {ValidCharacterId} not found.");
    }

    [Test]
    public void AssignEquipmentToCharacterAsync_EquipmentNotFound_ThrowsArgumentException()
    {
        _mockEquipmentRepository
            .Setup(repo => repo.GetByIdAsync(NonExistentId))
            .ReturnsAsync((Equipment)null!);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterEquipmentService.AssignEquipmentToCharacterAsync(ValidCharacterId, NonExistentId),
            $"Equipment with ID {NonExistentId} not found.");
    }

    [Test]
    public void AssignEquipmentToCharacterAsync_EquipmentAlreadyAssigned_ThrowsInvalidOperationException()
    {
        _mockCharacterEquipmentRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterEquipment, bool>>>()))
            .ReturnsAsync(new List<CharacterEquipment> { _testAssignment });

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.AssignEquipmentToCharacterAsync(ValidCharacterId, ValidEquipmentId),
            "Equipment with ID 20 is already assigned to character with ID 10.");
    }

    // -----------------------------
    //  ToggleEquipmentStatusAsync Tests
    // -----------------------------

    [Test]
    public void ToggleEquipmentStatusAsync_CharacterNotFound_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(NonExistentId))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.ToggleEquipmentStatusAsync(NonExistentId, ValidEquipmentId),
            $"Character with ID {NonExistentId} not found.");
    }

    [Test]
    public void ToggleEquipmentStatusAsync_EquipmentNotFound_ThrowsInvalidOperationException()
    {
        _mockEquipmentRepository
            .Setup(repo => repo.GetByIdAsync(NonExistentId))
            .ReturnsAsync((Equipment)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.ToggleEquipmentStatusAsync(ValidCharacterId, NonExistentId),
            $"Equipment with ID {NonExistentId} not found.");
    }

    // ------------------------------------
    //  RemoveEquipmentFromCharacterAsync Tests
    // ------------------------------------

    [Test]
    public void RemoveEquipmentFromCharacterAsync_CharacterNotFound_ThrowsInvalidOperationException()
    {
        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(NonExistentId))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.RemoveEquipmentFromCharacterAsync(NonExistentId, ValidEquipmentId),
            $"Character with ID {NonExistentId} not found.");
    }

    [Test]
    public void RemoveEquipmentFromCharacterAsync_EquipmentNotFound_ThrowsInvalidOperationException()
    {
        _mockEquipmentRepository
            .Setup(repo => repo.GetByIdAsync(NonExistentId))
            .ReturnsAsync((Equipment)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.RemoveEquipmentFromCharacterAsync(ValidCharacterId, NonExistentId),
            $"Equipment with ID {NonExistentId} not found.");
    }

    [Test]
    public async Task RemoveEquipmentFromCharacterAsync_AssignmentExists_DeletesAndReturnsTrue()
    {
        _mockCharacterEquipmentRepository
            .Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<CharacterEquipment, bool>>>()))
            .ReturnsAsync(new List<CharacterEquipment> { _testAssignment });
        _mockCharacterEquipmentRepository.Setup(repo => repo.DeleteAsync(It.IsAny<CharacterEquipment>()))
            .Returns(Task.CompletedTask);

        var result = await _characterEquipmentService.RemoveEquipmentFromCharacterAsync(ValidCharacterId, ValidEquipmentId);

        Assert.That(result, Is.True);

        _mockCharacterEquipmentRepository.Verify(repo => repo.DeleteAsync(_testAssignment), Times.Once);
    }

    [Test]
    public async Task RemoveEquipmentFromCharacterAsync_AssignmentNotFound_ReturnsFalse()
    {
        var result = await _characterEquipmentService.RemoveEquipmentFromCharacterAsync(ValidCharacterId, ValidEquipmentId);

        Assert.That(result, Is.False);
        _mockCharacterEquipmentRepository.Verify(repo => repo.DeleteAsync(It.IsAny<CharacterEquipment>()), Times.Never);
    }

    // ---------------------------------------------
    //  BulkInsertCharacterEquipmentFromJsonAsync Tests
    // ---------------------------------------------

    [Test]
    public async Task BulkInsertCharacterEquipmentFromJsonAsync_WithValidJson_InsertsAllEquipment()
    {
        var equipmentToInsert = new List<CharacterEquipment>
        {
            _testAssignment,
            new CharacterEquipment { CharacterId = ValidCharacterId + 1, EquipmentId = ValidEquipmentId + 1 }
        };

        var characters = new List<Character>
        {
            _testCharacter,
            new Character { Id = ValidCharacterId + 1, Name = "Bob", Level = 5 }
        };

        var equipment = new List<Equipment>
        {
            _testEquipment,
            new Equipment { 
                Id = ValidEquipmentId + 1, 
                Name = "Equipment2",
                Type = "Armor",
                Rarity = "Epic",
                AttackBonus = 25,
                DefenseBonus = 5
            }
        };

        var jsonContent = JsonConvert.SerializeObject(equipmentToInsert);
        var jsonFilePath = "bulk_character_equipment.json";

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => characters.FirstOrDefault(c => c.Id == id));
        _mockEquipmentRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => equipment.FirstOrDefault(e => e.Id == id)!);
        _mockCharacterEquipmentRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterEquipment>>()))
            .Returns(Task.CompletedTask);

        await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(jsonFilePath);

        _mockCharacterEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterEquipment>>(cq => cq.Count() == equipmentToInsert.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharacterEquipmentFromJsonAsync_WithLargeCharacterEquipmentList_InsertsAllEquipment()
    {
        var characters = new List<Character>();
        var equipment = new List<Equipment>();
        var equipmentToInsert = new List<CharacterEquipment>();

        for (int i = 1; i <= 1000; i++)
        {
            characters.Add(new Character { Id = i, Name = $"BulkChar{i}", Level = 10 });
            equipment.Add(new Equipment { Id = i * 100, Name = $"BulkEquipment{i}" });
            equipmentToInsert.Add(new CharacterEquipment { CharacterId = i, EquipmentId = i * 100 });
        }

        var jsonContent = JsonConvert.SerializeObject(equipmentToInsert);
        var jsonFilePath = "bulk_character_equipment.json";

        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => characters.FirstOrDefault(c => c.Id == id));
        _mockEquipmentRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => equipment.FirstOrDefault(q => q.Id == id)!);
        _mockCharacterEquipmentRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterEquipment>>()))
            .Returns(Task.CompletedTask);

        await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(jsonFilePath);

        _mockCharacterEquipmentRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<CharacterEquipment>>(ce => ce.Count() == equipmentToInsert.Count)), Times.Once);

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(_emptyName));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty or whitespace."));
    }

    [Test]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithNullFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(null));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty or whitespace."));
    }

    [Test]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(_whitespaceName));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty or whitespace."));
    }

    [Test]
    public void BulkInsertCharacterEquipmentFromJsonAsync_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        var nonExistentPath = "does_not_exist.json";

        var ex = Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(nonExistentPath));

        Assert.That(ex.Message, Does.Contain("File not found"));
    }

    [Test]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "invalid_character_equipment.json";
        var invalidJsonContent = "{ invalid json }";

        File.WriteAllText(jsonFilePath, invalidJsonContent);

        var ex = Assert.ThrowsAsync<JsonReaderException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("Invalid character"));

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithEmptyJson_ThrowsInvalidOperationException()
    {
        var jsonFilePath = "empty_character_equipment.json";
        var emptyJsonContent = "[]";

        File.WriteAllText(jsonFilePath, emptyJsonContent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("No character equipment found in JSON file."));

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharacterEquipmentFromJsonAsync_CharacterNotFound_ThrowsInvalidOperationException()
    {
        var equipmentToInsert = new List<CharacterEquipment>
        {
            new CharacterEquipment { CharacterId = NonExistentId, EquipmentId = ValidEquipmentId }
        };

        var jsonContent = JsonConvert.SerializeObject(equipmentToInsert);
        var jsonFilePath = "missing_char_bulk.json";
        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository
            .Setup(repo => repo.GetByIdAsync(NonExistentId))
            .ReturnsAsync((Character)null!);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(jsonFilePath),
            $"Character with ID {NonExistentId} not found.");

        _mockCharacterEquipmentRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterEquipment>>()), Times.Never);

        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharacterEquipmentFromJsonAsync_ExistingAssignment_ThrowsInvalidOperationException()
    {
        var equipmentToInsert = new List<CharacterEquipment>
        {
            _testAssignment
        };

        var jsonContent = JsonConvert.SerializeObject(equipmentToInsert);
        var jsonFilePath = "existing_assignment_bulk.json";
        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterEquipmentRepository.Setup(repo => repo.FindAsync(
            It.IsAny<Expression<Func<CharacterEquipment, bool>>>()))
            .ReturnsAsync(new List<CharacterEquipment> { _testAssignment });

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(jsonFilePath),
            $"Equipment with ID {ValidEquipmentId} is already assigned to character with ID {ValidCharacterId}.");

        _mockCharacterEquipmentRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<CharacterEquipment>>()), Times.Never);

        File.Delete(jsonFilePath);
    }
}