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
    private CharacterStats _testCharacterStats = null!;

    private string _emptyName;
    private string _whitespaceName;
    private string _invalidTest;
    private string _validTest;

    [SetUp]
    public void Setup()
    {
        _mockCharacterStatsRepository = new Mock<IRepository<CharacterStats>>();
        _mockCharacterRepository = new Mock<ICharacterRepository>();

        _characterStatsService = new CharacterStatsService(
            _mockCharacterRepository.Object,
            _mockCharacterStatsRepository.Object);

        _testCharacter = new Character { Id = 10, Name = "TestHero" };
        _testCharacterStats = new CharacterStats
        {
            Id = 1,
            CharacterId = 10,
            Strength = 16,
            Dexterity = 12,
            Intelligence = 10,
            Constitution = 15,
            Wisdom = 12,
            Charisma = 14
        };

        _emptyName = "";
        _whitespaceName = "   ";
        _invalidTest = "Invalid";
        _validTest = "Valid";
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

}