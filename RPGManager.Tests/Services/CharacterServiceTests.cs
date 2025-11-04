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

    private List<Character> GetTestCharacters() => new List<Character>
    {
        new Character { Id = 1, Name = "Hero1", Level = 5, CharacterClassId = 1, IsActive = true },
        new Character { Id = 2, Name = "Hero2", Level = 10, CharacterClassId = 2, IsActive = false },
        new Character { Id = 3, Name = "Hero3", Level = 7, CharacterClassId = 1, IsActive = true },
        new Character { Id = 4, Name = "Hero4", Level = 15, CharacterClassId = 1, IsActive = true },
        new Character { Id = 5, Name = "Hero5", Level = 20, CharacterClassId = 3, IsActive = false },
        new Character { Id = 6, Name = "Hero6", Level = 8, CharacterClassId = 1, IsActive = false }
    };

    private void SetupFilteringMock(List<Character> sourceData)
    {
        _mockCharacterRepository.Setup(repo =>
            repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()))
            .Returns<Expression<Func<Character, bool>>>(predicate =>
            {
                var compiledPredicate = predicate.Compile();
                return Task.FromResult(sourceData.Where(compiledPredicate).AsEnumerable());
            });
    }

    private void VerifyFindAsyncCalledOnce()
    {
        _mockCharacterRepository.Verify(repo =>
            repo.FindAsync(It.IsAny<Expression<Func<Character, bool>>>()), Times.Once);
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

        var result = await _characterService.CreateCharacterAsync(_testCharacter);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("TestHero"));
        Assert.That(result.CreatedDate, Is.Not.EqualTo(default(DateTime)));

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

        _mockCharacterRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Character>>()), Times.Never);
    }

    [Test]
    public void CreateCharacterAsync_WithNegativeLevel_ThrowsArgumentException()
    {
        var invalidCharacter = new Character { Name = "ValidName", Level = -5 };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.CreateCharacterAsync(invalidCharacter));

        Assert.That(ex.ParamName, Is.EqualTo("character"));
        Assert.That(ex.Message, Does.Contain("Character level must be at least 1."));
    }

    [Test]
    public void CreateCharacterAsync_WithZeroLevel_ThrowsArgumentException()
    {
        var invalidCharacter = new Character { Name = "ValidName", Level = 0 };

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
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("TestHero"));

        _mockCharacterRepository.Verify(repo => repo.GetCharacterWithDetailsAsync(1), Times.Once);
    }

    [Test]
    public async Task GetCharacterWithDetailsAsync_WithNonExistentId_ReturnsNull()
    {
        _mockCharacterRepository.Setup(repo => repo.GetCharacterWithDetailsAsync(999))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.GetCharacterWithDetailsAsync(999);

        Assert.That(result, Is.Null);

        _mockCharacterRepository.Verify(repo => repo.GetCharacterWithDetailsAsync(999), Times.Once);
    }

    //  ---------------------------
    //  GetAllCharactersAsync Tests
    //  ---------------------------

    [Test]
    public async Task GetAllCharactersAsync_ReturnsAllCharacters()
    {
        var characters = new List<Character>
        {
            new Character { Id = 1, Name = "Hero1", Level = 5 },
            new Character { Id = 2, Name = "Hero2", Level = 10 }
        };

        _mockCharacterRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(characters);

        var result = await _characterService.GetAllCharactersAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));

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

    [Test]
    public async Task DeleteCharacterAsync_WithNonExistentId_ReturnsFalse()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.DeleteCharacterAsync(999);

        Assert.That(result, Is.False);

        _mockCharacterRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockCharacterRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Character>()), Times.Never);
    }

    [Test]
    public async Task DeleteCharacterAsync_WithNegativeId_ReturnsFalse()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(-1))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.DeleteCharacterAsync(-1);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteCharacterAsync_WithZeroId_ReturnsFalse()
    {
        _mockCharacterRepository.Setup(repo => repo.GetByIdAsync(0))
            .ReturnsAsync((Character?)null);

        var result = await _characterService.DeleteCharacterAsync(0);

        Assert.That(result, Is.False);
    }

    //  --------------------------------
    //  GetCharactersByFilterAsync Tests
    //  --------------------------------

    [Test]
    public async Task GetCharactersByFilterAsync_WithNoFilters_ReturnsAllCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(characters.Count));

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithNoMatchingFilters_ReturnsEmptyCollection()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 99);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithIsActiveTrueFilter_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(isActive: true);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(3));
        Assert.That(result.All(c => c.IsActive), Is.True);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithIsActiveFalseFilter_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(isActive: false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(3));
        Assert.That(result.All(c => c.IsActive == false), Is.True);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithNegativeLevelFilters_ReturnsAllCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: -5, maxLevel: -1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithNullFilters_ReturnsAllCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: null, maxLevel: null, classId: null, isActive: null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(characters.Count));

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithClassIdFilter_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(classId: 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(4));
        Assert.That(result.All(c => c.CharacterClassId == 1), Is.True);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithMinLevelFilter_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 8);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(4));
        Assert.That(result.All(c => c.Level >= 8), Is.True);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithMaxLevelFilter_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(maxLevel: 7);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(c => c.Level <= 7), Is.True);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithLevelRangeFilter_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 8, maxLevel: 12);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(c => c.Level >= 8 && c.Level <= 12), Is.True);
        Assert.That(result.Any(c => c.Level == 10), Is.True);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithMinLevelHigherThanMaxLevel_ReturnsEmptyCollection()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 10, maxLevel: 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithLevelAndClassFilters_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 6, maxLevel: 10, classId: 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(c => c.CharacterClassId == 1 && c.Level >= 6 && c.Level <= 10), Is.True);
        Assert.That(result.Any(c => c.Name == "Hero3"), Is.True);
        Assert.That(result.Any(c => c.Name == "Hero6"), Is.True);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithAllFilters_ReturnsFilteredCharacters()
    {
        var characters = GetTestCharacters();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync(minLevel: 6, maxLevel: 9, classId: 1, isActive: false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));
        var singleResult = result.First();
        Assert.That(singleResult.Name, Is.EqualTo("Hero6"));
        Assert.That(singleResult.Level, Is.EqualTo(8));
        Assert.That(singleResult.IsActive, Is.False);

        VerifyFindAsyncCalledOnce();
    }

    [Test]
    public async Task GetCharactersByFilterAsync_WithNoCharacters_ReturnsEmptyCollection()
    {
        var characters = new List<Character>();
        SetupFilteringMock(characters);

        var result = await _characterService.GetCharactersByFilterAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        VerifyFindAsyncCalledOnce();
    }

    //  ---------------------------------
    //  ExportCharactersToJsonAsync Tests
    //  ---------------------------------

    [Test]
    public async Task ExportCharactersToJsonAsync_WithValidParameters_ExportsSuccessfully()
    {
        var characters = GetTestCharacters();

        SetupFilteringMock(characters);

        var outputFilePath = "test_characters_export.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent, Does.Contain("Hero1"));
        Assert.That(fileContent, Does.Contain("Hero2"));

        File.Delete(outputFilePath);
    }

    [Test]
    public void ExportCharactersToJsonAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.ExportCharactersToJsonAsync(""));

        Assert.That(ex.ParamName, Is.EqualTo("outputFilePath"));
        Assert.That(ex.Message, Does.Contain("Output file path cannot be empty"));
    }

    [Test]
    public void ExportCharactersToJsonAsync_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.ExportCharactersToJsonAsync("   "));

        Assert.That(ex.ParamName, Is.EqualTo("outputFilePath"));
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithNoMatchingFilters_ExportsEmptyArray()
    {
        var characters = GetTestCharacters();

        SetupFilteringMock(characters);

        var outputFilePath = "test_characters_export_empty.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath, minLevel: 21);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent, Is.EqualTo("[]"));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithFilters_ExportsFilteredCharacters()
    {
        var characters = GetTestCharacters();

        SetupFilteringMock(characters);

        var outputFilePath = "test_characters_export_filtered.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath, minLevel: 8, classId: 1);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent, Does.Contain("Hero4"));
        Assert.That(fileContent, Does.Contain("Hero6"));
        Assert.That(fileContent, Does.Not.Contain("Hero1"));
        Assert.That(fileContent, Does.Not.Contain("Hero2"));
        Assert.That(fileContent, Does.Not.Contain("Hero3"));
        Assert.That(fileContent, Does.Not.Contain("Hero5"));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithNoCharacters_ExportsEmptyArray()
    {
        var characters = new List<Character>();

        SetupFilteringMock(characters);

        var outputFilePath = "test_characters_export_no_characters.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent, Is.EqualTo("[]"));

        File.Delete(outputFilePath);
    }

    [Test]
    public async Task ExportCharactersToJsonAsync_WithLargeNumberOfCharacters_ExportsSuccessfully()
    {
        var characters = new List<Character>();
        for (int i = 1; i <= 1000; i++)
        {
            characters.Add(new Character { Id = i, Name = $"Hero{i}", Level = i % 100, CharacterClassId = i % 5, IsActive = i % 2 == 0 });
        }

        SetupFilteringMock(characters);

        var outputFilePath = "test_characters_export_large_number.json";
        await _characterService.ExportCharactersToJsonAsync(outputFilePath);

        Assert.That(File.Exists(outputFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(outputFilePath);

        Assert.That(fileContent.Length, Is.GreaterThan(0));

        File.Delete(outputFilePath);
    }

    //  ---------------------------------------
    //  BulkInsertCharactersFromJsonAsync Tests
    //  ---------------------------------------

    [Test]
    public async Task BulkInsertCharactersFromJsonAsync_WithValidJsonFile_InsertsCharacters()
    {
        var jsonFilePath = "test_characters_bulk_insert.json";

        var charactersToInsert = GetTestCharacters();

        var jsonContent = JsonConvert.SerializeObject(charactersToInsert, Formatting.Indented);
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

    [Test]
    public void BulkInsertCharactersFromJsonAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.BulkInsertCharactersFromJsonAsync(""));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
        Assert.That(ex.Message, Does.Contain("File path cannot be empty."));
    }

    [Test]
    public void BulkInsertCharactersFromJsonAsync_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _characterService.BulkInsertCharactersFromJsonAsync("   "));

        Assert.That(ex.ParamName, Is.EqualTo("jsonFilePath"));
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
            charactersToInsert.Add(new Character { Name = $"BulkHero{i}", Level = i % 100, Experience = i * 10, Gold = i * 5 });
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