using Moq;
using NUnit.Framework;
using RPGManager.Data.Interfaces;
using RPGManager.Data.Models;
using RPGManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGManager.Tests.Services;

[TestFixture]
public class CharacterClassServiceTests
{
    private Mock<ICharacterClassRepository> _mockCharacterClassRepository = null!;
    private CharacterClassService _characterClassService = null!;

    private CharacterClass _testCharacterClass = null!;


    [SetUp]
    public void Setup()
    {
        _mockCharacterClassRepository = new Mock<ICharacterClassRepository>();

        _characterClassService = new CharacterClassService(_mockCharacterClassRepository.Object);

        _testCharacterClass = new()
        {
            Id = 1,
            Name = "Warrior",
            Description = "Tanky front-liner",
            PrimaryAttribute = nameof(CharacterStats.Strength),
            BaseHealth = 100,
            BaseMana = 10
        };
    }

    //  -----------------
    //  Constructor Tests
    //  -----------------

    [Test]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new CharacterClassService(null));

        Assert.That(ex.ParamName, Is.EqualTo("characterClassRepository"));
    }

    [Test]
    public void Constructor_WithValidRepository_CreatesInstance()
    {
        var service = new CharacterClassService(_mockCharacterClassRepository.Object);

        Assert.That(service, Is.Not.Null);
    }

    // -----------------------
    // GetAllClassesAsync Tests
    // -----------------------

    [Test]
    public async Task GetAllClassesAsync_WithClassesAvailable_ReturnsAllClasses()
    {
        var expected = new List<CharacterClass> { _testCharacterClass };

        _mockCharacterClassRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(expected);

        var result = await _characterClassService.GetAllClassesAsync();

        Assert.That(result, Is.EqualTo(expected));

        _mockCharacterClassRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllClassesAsync_WithNoClassesAvailable_ReturnsEmptyList()
    {
        _mockCharacterClassRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<CharacterClass>());

        var result = await _characterClassService.GetAllClassesAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _mockCharacterClassRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllClassesAsync_WithLargeNumberOfClasses_ReturnsAllClasses()
    {
        var largeClassesList = Enumerable.Range(1, 1000)
            .Select(i => new CharacterClass
            {
                Id = i,
                Name = $"Class{i}",
                Description = $"Description for Class{i}",
                PrimaryAttribute = (i % 3) switch
                {
                    0 => nameof(CharacterStats.Strength),
                    1 => nameof(CharacterStats.Dexterity),
                    _ => nameof(CharacterStats.Intelligence)
                },
                BaseHealth = 50 + i,
                BaseMana = 20 + i
                }).ToList();

        _mockCharacterClassRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(largeClassesList);

        var result = await _characterClassService.GetAllClassesAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(largeClassesList.Count));

        _mockCharacterClassRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    // -----------------------
    // GetClassByIdAsync Tests
    // -----------------------

    [Test]
    public async Task GetClassByIdAsync_WithValidId_ReturnsClass()
    {
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdAsync(_testCharacterClass.Id))
            .ReturnsAsync(_testCharacterClass);

        var result = await _characterClassService.GetClassByIdAsync(_testCharacterClass.Id);

        Assert.That(result, Is.EqualTo(_testCharacterClass));

        _mockCharacterClassRepository.Verify(repo => repo.GetByIdAsync(_testCharacterClass.Id), Times.Once);
    }

    [TestCase(0)]
    public async Task GetClassByIdAsync_WithNonExistingId_ReturnsNull(int id)
    {
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((CharacterClass)null!);

        var result = await _characterClassService.GetClassByIdAsync(id);

        Assert.That(result, Is.Null);

        _mockCharacterClassRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
    }

    // -------------------------------------
    // GetClassByIdWithCharactersAsync Tests
    // -------------------------------------

    [Test]
    public async Task GetClassByIdWithCharactersAsync_WithValidId_ReturnsClass()
    {
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdWithCharactersAsync(_testCharacterClass.Id))
            .ReturnsAsync(_testCharacterClass);

        var result = await _characterClassService.GetClassByIdWithCharactersAsync(_testCharacterClass.Id);

        Assert.That(result, Is.EqualTo(_testCharacterClass));

        _mockCharacterClassRepository.Verify(repo => repo.GetByIdWithCharactersAsync(_testCharacterClass.Id), Times.Once);
    }

    [TestCase(0)]
    public async Task GetClassByIdWithCharactersAsync_WithNonExistingId_ReturnsNull(int id)
    {
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdWithCharactersAsync(id))
            .ReturnsAsync((CharacterClass)null!);

        var result = await _characterClassService.GetClassByIdWithCharactersAsync(id);

        Assert.That(result, Is.Null);

        _mockCharacterClassRepository.Verify(repo => repo.GetByIdWithCharactersAsync(id), Times.Once);
    }

    // ----------------------
    // CreateClassAsync Tests
    // ----------------------

    [Test]
    public async Task CreateClassAsync_ValidClass_CallsAddAndReturnsClass()
    {
        var newClass = new CharacterClass
        {
            Name = "Mage",
            PrimaryAttribute = nameof(CharacterStats.Intelligence)
        };

        var result = await _characterClassService.CreateClassAsync(newClass);

        _mockCharacterClassRepository
            .Verify(repo => repo.AddRangeAsync(It.Is<IEnumerable<CharacterClass>>(c => c.Contains(newClass))), Times.Once);

        Assert.That(result, Is.EqualTo(newClass));
    }

    [TestCase(null!, "Name")]
    [TestCase("", "Name")]
    [TestCase(" ", "Name")]
    public void CreateClassAsync_InvalidName_ThrowsArgumentException(string name, string paramName)
    {
        var invalidClass = new CharacterClass { Name = name, PrimaryAttribute = "Strength" };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _characterClassService.CreateClassAsync(invalidClass));
        Assert.That(ex.ParamName, Is.EqualTo(paramName));
    }

    [Test]
    public void CreateClassAsync_NameTooLong_ThrowsArgumentException()
    {
        var longName = new string('A', 51);
        var invalidClass = new CharacterClass { Name = longName, PrimaryAttribute = "Strength" };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _characterClassService.CreateClassAsync(invalidClass));
        Assert.That(ex.Message, Does.Contain("cannot exceed 50 characters"));
    }

    [Test]
    public void CreateClassAsync_DescriptionTooLong_ThrowsArgumentException()
    {
        var longDescription = new string('A', 501);
        var invalidClass = new CharacterClass { Name = "Test", PrimaryAttribute = "Strength", Description = longDescription };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _characterClassService.CreateClassAsync(invalidClass));
        Assert.That(ex.Message, Does.Contain("cannot exceed 500 characters"));
    }

    [TestCase(-1, 10)]
    [TestCase(10, -1)]
    public void CreateClassAsync_NegativeBaseStats_ThrowsArgumentException(int baseHealth, int baseMana)
    {
        var invalidClass = new CharacterClass
        {
            Name = "Test",
            PrimaryAttribute = "Strength",
            BaseHealth = baseHealth,
            BaseMana = baseMana
        };

        Assert.ThrowsAsync<ArgumentException>(async () => await _characterClassService.CreateClassAsync(invalidClass));
    }

    [Test]
    public void CreateClassAsync_InvalidPrimaryAttribute_ThrowsArgumentException()
    {
        var invalidClass = new CharacterClass { Name = "Mage", PrimaryAttribute = "Luck" };

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _characterClassService.CreateClassAsync(invalidClass));
        Assert.That(ex.Message, Does.Contain("Invalid Primary Attribute 'Luck'"));
    }

    // ----------------------
    // UpdateClassAsync Tests
    // ----------------------

    [Test]
    public void UpdateClassAsync_NullClass_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _characterClassService.UpdateClassAsync(null));
    }

    [Test]
    public void UpdateClassAsync_NonExistentClass_ThrowsInvalidOperationException()
    {
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((CharacterClass)null!);
        var nonExistentClass = new CharacterClass { Id = 99, Name = "Ghost", PrimaryAttribute = "Strength" };

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _characterClassService.UpdateClassAsync(nonExistentClass));
    }

    [Test]
    public async Task UpdateClassAsync_ValidClass_CallsUpdate()
    {
        var updatedClass = new CharacterClass { Id = 1, Name = "Paladin", PrimaryAttribute = "Charisma" };
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(_testCharacterClass);

        await _characterClassService.UpdateClassAsync(updatedClass);

        _mockCharacterClassRepository.Verify(repo => repo.UpdateAsync(updatedClass), Times.Once);
    }

    // ----------------------
    // DeleteClassAsync Tests
    // ----------------------

    [Test]
    public async Task DeleteClassAsync_ClassNotFound_ReturnsFalse()
    {
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdWithCharactersAsync(1)).ReturnsAsync((CharacterClass)null!);

        var result = await _characterClassService.DeleteClassAsync(1, 0);

        Assert.That(result, Is.False);
        _mockCharacterClassRepository.Verify(repo => repo.DeleteAsync(It.IsAny<CharacterClass>()), Times.Never);
    }

    [Test]
    public async Task DeleteClassAsync_NoDependentCharacters_CallsDeleteAndReturnsTrue()
    {
        var classToDelete = new CharacterClass { Id = 1, Name = "Rogue" };
        _mockCharacterClassRepository.Setup(repo => repo.GetByIdWithCharactersAsync(1)).ReturnsAsync(classToDelete);

        var result = await _characterClassService.DeleteClassAsync(1, 0);

        Assert.That(result, Is.True);
        _mockCharacterClassRepository.Verify(repo => repo.DeleteAsync(classToDelete), Times.Once);
    }

    [Test]
    public async Task DeleteClassAsync_WithDependentsAndNoNewClassId_ReassignsToZeroDeletesAndReturnsTrue()
    {
        var character1 = new Character { Id = 101, CharacterClassId = 1 };
        var character2 = new Character { Id = 102, CharacterClassId = 1 };
        var classToDelete = new CharacterClass { Id = 1, Name = "Fighter", Characters = new List<Character> { character1, character2 } };

        _mockCharacterClassRepository.Setup(repo => repo.GetByIdWithCharactersAsync(1)).ReturnsAsync(classToDelete);

        var result = await _characterClassService.DeleteClassAsync(1, 0);

        Assert.That(result, Is.True);
        Assert.That(character1.CharacterClassId, Is.EqualTo(0));
        Assert.That(character2.CharacterClassId, Is.EqualTo(0));

        _mockCharacterClassRepository.Verify(repo => repo.DeleteAsync(classToDelete), Times.Once);
        _mockCharacterClassRepository.Verify(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CharacterClass, bool>>>()), Times.Never);
    }

    [Test]
    public async Task DeleteClassAsync_WithDependentsAndValidNewClassId_ReassignsDeletesAndReturnsTrue()
    {
        const int newClassId = 5;
        var character = new Character { Id = 201, CharacterClassId = 1 };
        var classToDelete = new CharacterClass { Id = 1, Name = "Monk", Characters = new List<Character> { character } };
        var targetClass = new CharacterClass { Id = newClassId, Name = "Brawler" };

        _mockCharacterClassRepository.Setup(repo => repo.GetByIdWithCharactersAsync(1)).ReturnsAsync(classToDelete);
        _mockCharacterClassRepository.Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CharacterClass, bool>>>()))
                       .ReturnsAsync(new List<CharacterClass> { targetClass });

        var result = await _characterClassService.DeleteClassAsync(1, newClassId);

        Assert.That(result, Is.True);
        Assert.That(character.CharacterClassId, Is.EqualTo(newClassId));
        _mockCharacterClassRepository.Verify(repo => repo.DeleteAsync(classToDelete), Times.Once);
        _mockCharacterClassRepository.Verify(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CharacterClass, bool>>>()), Times.Once);
    }

    [Test]
    public void DeleteClassAsync_WithDependentsAndInvalidNewClassId_ThrowsInvalidOperationException()
    {
        const int newClassId = 99;
        var character = new Character { Id = 301, CharacterClassId = 1 };
        var classToDelete = new CharacterClass { Id = 1, Name = "Hunter", Characters = new List<Character> { character } };

        _mockCharacterClassRepository.Setup(repo => repo.GetByIdWithCharactersAsync(1)).ReturnsAsync(classToDelete);
        _mockCharacterClassRepository.Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CharacterClass, bool>>>()))
                       .ReturnsAsync(new List<CharacterClass>());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _characterClassService.DeleteClassAsync(1, newClassId));
        Assert.That(ex.Message, Does.Contain($"The target class ID ({newClassId}) for reassignment does not exist."));

        _mockCharacterClassRepository.Verify(repo => repo.DeleteAsync(It.IsAny<CharacterClass>()), Times.Never);
    }
}