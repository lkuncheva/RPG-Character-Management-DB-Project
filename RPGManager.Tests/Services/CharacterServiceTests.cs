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
public class CharacterServiceTests
{
    private Mock<ICharacterRepository> _mockCharacterRepository = null!;
    private CharacterService _characterService = null!;
    private Character _testCharacter = null!;
    private List<Character> _testCharacterList = null!;

    private string _invalidTest;
    private string _validTest;

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

        _testCharacterList = new List<Character> 
        {
            new Character { Id = 1, Name = "Hero1", Level = 5, CharacterClassId = 1, IsActive = true },
            new Character { Id = 2, Name = "Hero2", Level = 10, CharacterClassId = 2, IsActive = false },
            new Character { Id = 3, Name = "Hero3", Level = 7, CharacterClassId = 1, IsActive = true },
            new Character { Id = 4, Name = "Hero4", Level = 15, CharacterClassId = 1, IsActive = true },
            new Character { Id = 5, Name = "Hero5", Level = 20, CharacterClassId = 3, IsActive = false },
            new Character { Id = 6, Name = "Hero6", Level = 8, CharacterClassId = 1, IsActive = false }
        };

        _mockCharacterRepository.Setup(repo =>
            repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()))
            .Returns<Expression<Func<Character, bool>>>(predicate =>
            {
                var compiledPredicate = predicate.Compile();
                return Task.FromResult(_testCharacterList.Where(compiledPredicate).AsEnumerable());
            });

        _invalidTest = "Invalid";
        _validTest = "Valid";
    }

    //  -----------------
    //  Constructor Tests
    //  -----------------

    [Test]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterService(null));

        Assert.That(ex.ParamName, Is.EqualTo("characterRepository"));
    }

    [Test]
    public void Constructor_WithValidRepository_CreatesInstance()
    {
        var service = new CharacterService(_mockCharacterRepository.Object);

        Assert.That(service, Is.Not.Null);
    }

    //  --------------------------
    //  CreateCharacterAsync Tests
    //  --------------------------

    [Test]
    public async Task CreateCharacterAsync_WithValidCharacter_ReturnsCharacter()
    {
        _mockCharacterRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Character>>()))
            .Returns(Task.FromResult(_testCharacter));

        Character result = await _characterService.CreateCharacterAsync(_testCharacter);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(_testCharacter));

        _mockCharacterRepository.Verify(repo => repo.AddRangeAsync(
            It.Is<IEnumerable<Character>>(c => c.Count() == 1 && c.First() == _testCharacter)), Times.Once);
    }

    [Test]
    public void CreateCharacterAsync_WithNullCharacter_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _characterService.CreateCharacterAsync(null));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase(" ")]
    public void CreateCharacterAsync_WithInvalidName_ThrowsArgumentException(string name)
    {
        var invalidCharacter = new Character { Name = name };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.CreateCharacterAsync(invalidCharacter));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
        Assert.That(ex.Message, Does.Contain("Character name cannot be empty"));
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

        _mockCharacterRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Character>>()), Times.Never);
    }

    [TestCase(-5)]
    [TestCase(0)]
    public void CreateCharacterAsync_WithInvalidLevel_ThrowsArgumentException(int level)
    {
        var invalidCharacter = new Character
        {
            Name = _validTest,
            Level = level
        };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.CreateCharacterAsync(invalidCharacter));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
        Assert.That(ex.Message, Does.Contain("Character level must be at least 1."));
    }

    //  ----------------------------------
    //  GetCharacterWithDetailsAsync Tests
    //  ----------------------------------

    [Test]
    public async Task GetCharacterWithDetailsAsync_WithValidId_ReturnsCharacterWithDetails()
    {
        _mockCharacterRepository.Setup(repo => repo.GetCharacterWithDetailsAsync(1))
            .ReturnsAsync(_testCharacter);

        var result = await _characterService.GetCharacterWithDetailsAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(_testCharacter));

        _mockCharacterRepository.Verify(repo => repo.GetCharacterWithDetailsAsync(1), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task GetCharacterWithDetailsAsync_WithInvalidId_ReturnsNull(int id)
    {
        _mockCharacterRepository.Setup(repo => repo.GetCharacterWithDetailsAsync(id))
            .ReturnsAsync((Character)null!);

        var result = await _characterService.GetCharacterWithDetailsAsync(id);

        Assert.That(result, Is.Null);

        _mockCharacterRepository.Verify(repo => repo.GetCharacterWithDetailsAsync(id), Times.Once);
    }

    //  ---------------------------
    //  GetAllCharactersAsync Tests
    //  ---------------------------

    [Test]
    public async Task GetAllCharactersAsync_ReturnsAllCharacters()
    {
        _mockCharacterRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(_testCharacterList);

        var result = await _characterService.GetAllCharactersAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(_testCharacterList.Count));

        _mockCharacterRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
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
        Assert.That(result, Is.EqualTo(_testCharacter));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task GetCharacterByIdAsync_WithInvalidId_ReturnsNull(int id)
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        var result = await _characterService.GetCharacterByIdAsync(id);

        Assert.That(result, Is.Null);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
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
            Name = _validTest,
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
        Assert.That(result, Is.EqualTo(updatedCharacter));

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

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public void UpdateCharacterAsync_WithInvalidCharacterId_ThrowsInvalidOperationException(int id)
    {
        var nonExistentCharacter = new Character
        {
            Id = id,
            Name = _invalidTest
        };
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterService.UpdateCharacterAsync(nonExistentCharacter));

        Assert.That(ex.Message, Is.EqualTo($"Character with ID {id} not found."));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
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

        var result = await _characterService.UpdateCharacterNameAsync(1, _validTest);

        Assert.That(result, Is.True);
        Assert.That(_testCharacter.Name, Is.EqualTo(_validTest));

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(_testCharacter), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task UpdateCharacterNameAsync_WithInvalidCharacterId_ReturnsFalse(int id)
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        var result = await _characterService.UpdateCharacterNameAsync(id, _invalidTest);

        Assert.That(result, Is.False);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase(" ")]
    public void UpdateCharacterNameAsync_WithEmptyName_ThrowsArgumentException(string name)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.UpdateCharacterNameAsync(1, name));

        Assert.That(ex.ParamName, Is.EqualTo("newName"));
        Assert.That(ex.Message, Does.Contain("New name cannot be empty"));
    }

    [Test]
    public async Task UpdateCharacterNameAsync_WithSameName_ReturnsTrue()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);

        var newName = _testCharacter.Name;

        var result = await _characterService.UpdateCharacterNameAsync(1, newName);

        Assert.That(result, Is.True);
        Assert.That(_testCharacter.Name, Is.EqualTo(newName));

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

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task UpdateCharacterLevelAsync_WithInvalidCharacterId_ReturnsFalse(int id)
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        var result = await _characterService.UpdateCharacterLevelAsync(id, 10);

        Assert.That(result, Is.False);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Character>()), Times.Never);
    }

    [TestCase(-5)]
    [TestCase(0)]
    public void UpdateCharacterLevelAsync_WithInvalidLevel_ThrowsArgumentException(int id)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.UpdateCharacterLevelAsync(1, id));

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

    [Test]
    public async Task UpdateCharacterLevelAsync_WithMinimumValidLevel_ReturnsTrue()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);
        _mockCharacterRepository.Setup(repo => repo.UpdateAsync(_testCharacter))
            .Returns(Task.CompletedTask);

        var result = await _characterService.UpdateCharacterLevelAsync(1, 1);

        Assert.That(result, Is.True);
        Assert.That(_testCharacter.Level, Is.EqualTo(1));
    }

    //  --------------------------
    //  DeleteCharacterAsync Tests
    //  --------------------------

    [Test]
    public async Task DeleteCharacterAsync_WithValidId_ReturnsTrue()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(_testCharacter);
        _mockCharacterRepository.Setup(repo => repo.DeleteAsync(_testCharacter))
            .Returns(Task.CompletedTask);

        var result = await _characterService.DeleteCharacterAsync(1);

        Assert.That(result, Is.True);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.DeleteAsync(_testCharacter), Times.Once);
    }

    [TestCase(999)]
    [TestCase(-5)]
    [TestCase(0)]
    public async Task DeleteCharacterAsync_WithInvalidId_ReturnsFalse(int id)
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Character)null!);

        var result = await _characterService.DeleteCharacterAsync(id);

        Assert.That(result, Is.False);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Character>()), Times.Never);
    }

    //  --------------------------------
    //  GetCharactersByFilterAsync Tests
    //  --------------------------------

    [Test]
    public async Task GetCharactersByFilterAsync_WithNoFilters_ReturnsAllCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(_testCharacterList.Count));

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithNoMatchingFilters_ReturnsEmptyCollection()
    {
        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 99);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetCharactersByFilterAsync_WithIsActiveTrueFilter_ReturnsFilteredCharacters(bool isActiveFilter)
    {
        var result = await _characterService.GetCharactersByFilterAsync(isActive: isActiveFilter);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(3));
        Assert.That(result.All(c => c.IsActive == isActiveFilter), Is.True);

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithNullFilters_ReturnsAllCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync(minLevel: null, maxLevel: null, classId: null, isActive: null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(_testCharacterList.Count));

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithClassIdFilter_ReturnsFilteredCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync(classId: 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(4));
        Assert.That(result.All(c => c.CharacterClassId == 1), Is.True);

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithMinLevelFilter_ReturnsFilteredCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 8);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(4));
        Assert.That(result.All(c => c.Level >= 8), Is.True);

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithMaxLevelFilter_ReturnsFilteredCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync(maxLevel: 7);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(c => c.Level <= 7), Is.True);

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithLevelRangeFilter_ReturnsFilteredCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 8, maxLevel: 12);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(c => c.Level >= 8 && c.Level <= 12), Is.True);
        Assert.That(result.Any(c => c.Level == 10), Is.True);

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithMinLevelHigherThanMaxLevel_ReturnsEmptyCollection()
    {
        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 10, maxLevel: 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithLevelAndClassFilters_ReturnsFilteredCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 6, maxLevel: 10, classId: 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(c => c.CharacterClassId == 1 && c.Level >= 6 && c.Level <= 10), Is.True);
        Assert.That(result.Any(c => c.Name == "Hero3"), Is.True);
        Assert.That(result.Any(c => c.Name == "Hero6"), Is.True);

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithAllFilters_ReturnsFilteredCharacters()
    {
        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 6, maxLevel: 9, classId: 1, isActive: false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));
        var singleResult = result.First();
        Assert.That(singleResult.Name, Is.EqualTo("Hero6"));
        Assert.That(singleResult.Level, Is.EqualTo(8));
        Assert.That(singleResult.IsActive, Is.False);

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithNoCharacters_ReturnsEmptyCollection()
    {
        var characters = new List<Character>();

        _mockCharacterRepository.Setup(repo =>
            repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()))
            .Returns<Expression<Func<Character, bool>>>(predicate =>
            {
                var compiledPredicate = predicate.Compile();
                return Task.FromResult(characters.Where(compiledPredicate).AsEnumerable());
            });

        var result = await _characterService.GetCharactersByFilterAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _mockCharacterRepository.Verify(repo =>
                    repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
    }

    //  ---------------------------------
    //  ExportCharactersToJsonAsync Tests
    //  ---------------------------------

    [Test]
    public async Task ExportCharactersToJsonAsync_WithValidParameters_ExportsSuccessfully()
    {
        var outputFilePath = "test_characters_export.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedCharacters = JsonConvert.DeserializeObject<List<Character>>(fileContent);

        Assert.That(exportedCharacters, Is.Not.Null);
        Assert.That(exportedCharacters.Count, Is.EqualTo(_testCharacterList.Count));

        File.Delete(outputFilePath);
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase(" ")]
    public void ExportCharactersToJsonAsync_WithInvalidFilePath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.ExportCharactersToJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("outputFilePath"));
        Assert.That(ex.Message, Does.Contain("Output file path cannot be empty"));
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithNoMatchingFilters_ExportsEmptyArray()
    {
        var outputFilePath = "test_characters_export_empty.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath, minLevel: 21);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedCharacters = JsonConvert.DeserializeObject<List<Character>>(fileContent);

        Assert.That(exportedCharacters, Is.Not.Null);
        Assert.That(exportedCharacters.Count, Is.EqualTo(0));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithFilters_ExportsFilteredCharacters()
    {
        var outputFilePath = "test_characters_export_filtered.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath, minLevel: 8, classId: 1);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedCharacters = JsonConvert.DeserializeObject<List<Character>>(fileContent);

        Assert.That(exportedCharacters, Is.Not.Null);
        Assert.That(exportedCharacters.Count, Is.EqualTo(2));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithNoCharacters_ExportsEmptyArray()
    {
        var characters = new List<Character>();

        _mockCharacterRepository.Setup(repo =>
            repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()))
            .Returns<Expression<Func<Character, bool>>>(predicate =>
            {
                var compiledPredicate = predicate.Compile();
                return Task.FromResult(characters.Where(compiledPredicate).AsEnumerable());
            });

        var outputFilePath = "test_characters_export_no_characters.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedCharacters = JsonConvert.DeserializeObject<List<Character>>(fileContent);

        Assert.That(exportedCharacters, Is.Not.Null);
        Assert.That(exportedCharacters.Count, Is.EqualTo(0));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithLargeNumberOfCharacters_ExportsSuccessfully()
    {
        var characters = new List<Character>();
        for (int i = 1; i <= 1000; i++)
        {
            characters.Add(new Character
            {
                Id = i,
                Name = $"Hero{i}",
                Level = i % 100,
                CharacterClassId = i % 5,
                IsActive = i % 2 == 0
            });
        }

        _mockCharacterRepository.Setup(repo =>
            repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()))
            .Returns<Expression<Func<Character, bool>>>(predicate =>
            {
                var compiledPredicate = predicate.Compile();
                return Task.FromResult(characters.Where(compiledPredicate).AsEnumerable());
            });

        var outputFilePath = "test_characters_export_large_number.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);
        var exportedCharacters = JsonConvert.DeserializeObject<List<Character>>(fileContent);

        Assert.That(exportedCharacters, Is.Not.Null);
        Assert.That(exportedCharacters.Count, Is.EqualTo(1000));

        File.Delete(outputFilePath);
    }

    //  ---------------------------------------
    //  BulkInsertCharactersFromJsonAsync Tests
    //  ---------------------------------------

    [Test]
    public async Task BulkInsertCharactersFromJsonAsync_WithValidJsonFile_InsertsCharacters()
    {
        var jsonFilePath = "test_characters_bulk_insert.json";

        var jsonContent = JsonConvert.SerializeObject(_testCharacterList, Formatting.Indented);
        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Character>>()))
            .Returns(Task.CompletedTask);

        await _characterService.BulkInsertCharactersFromJsonAsync(jsonFilePath);

        _mockCharacterRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Character>>()), Times.Once);

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertCharactersFromJsonAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var jsonFilePath = "non_existent_file.json";

        var ex = Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _characterService.BulkInsertCharactersFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("File not found"));
    }

    [TestCase(null!)]
    [TestCase("")]
    [TestCase("   ")]
    public void BulkInsertCharacterEquipmentFromJsonAsync_WithInvalidPath_ThrowsArgumentException(string filePath)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.BulkInsertCharactersFromJsonAsync(filePath));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertCharactersFromJsonAsync_WithInvalidJson_ThrowsJsonException()
    {
        var jsonFilePath = "invalid_json_file.json";
        var invalidJsonContent = "{ invalid json ";

        File.WriteAllText(jsonFilePath, invalidJsonContent);

        var ex = Assert.ThrowsAsync<JsonReaderException>(
            async () => await _characterService.BulkInsertCharactersFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("Invalid character"));

        File.Delete(jsonFilePath);
    }

    [Test]
    public void BulkInsertCharactersFromJsonAsync_WithNullJson_ThrowsArgumentException()
    {
        var jsonFilePath = "null_json_file.json";
        var nullJsonContent = "null";

        File.WriteAllText(jsonFilePath, nullJsonContent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _characterService.BulkInsertCharactersFromJsonAsync(jsonFilePath));

        Assert.That(ex.Message, Does.Contain("No characters found in JSON file."));
        File.Delete(jsonFilePath);
    }

    [Test]
    public async Task BulkInsertCharactersFromJsonAsync_WithLargeNumberOfCharacters_InsertsAllCharacters()
    {
        var jsonFilePath = "large_characters_bulk_insert.json";
        var charactersToInsert = new List<Character>();
        for (int i = 1; i <= 1000; i++)
        {
            charactersToInsert.Add(new Character { Name = $"BulkHero{i}", Level = i, Experience = i * 10, Gold = i * 5 });
        }

        var jsonContent = JsonConvert.SerializeObject(charactersToInsert, Formatting.Indented);
        await File.WriteAllTextAsync(jsonFilePath, jsonContent);

        _mockCharacterRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Character>>()))
            .Returns(Task.CompletedTask);

        await _characterService.BulkInsertCharactersFromJsonAsync(jsonFilePath);

        _mockCharacterRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Character>>()), Times.Once);

        File.Delete(jsonFilePath);
    }
}