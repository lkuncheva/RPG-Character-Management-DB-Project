using Newtonsoft.Json;
using RPGManager.Interfaces;
using RPGManager.Data.Interfaces;
using RPGManager.Data.Models;

namespace RPGManager.Services;
public class CharacterStatsService : ICharacterStatsService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IRepository<CharacterStats> _characterStatsRepository;

    public CharacterStatsService(
        ICharacterRepository characterRepository,
        IRepository<CharacterStats> characterStatsRepository)
    {
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
        _characterStatsRepository = characterStatsRepository ?? throw new ArgumentNullException(nameof(characterStatsRepository));
    }

    public async Task<CharacterStats> GetCharacterStatsAsync(int characterId)
    {
        await EnsureCharacterExistsAsync(characterId);

        var stats = await _characterStatsRepository.FindAsync(s => s.CharacterId == characterId);
        return stats.FirstOrDefault();
    }

    public async Task<CharacterStats> CreateCharacterStatsAsync(int characterId, CharacterStats stats)
    {
        EnsureStatsIsNotNull(stats);
        await EnsureCharacterExistsAsync(characterId);
        await EnsureStatsDoNotExistAsync(characterId);

        stats.CharacterId = characterId;
        await _characterStatsRepository.AddRangeAsync([stats]);
        return stats;
    }

    public async Task<bool> UpdateCharacterStatsAsync(int characterId, CharacterStats stats)
    {
        await EnsureCharacterExistsAsync(characterId);
        EnsureStatsIsNotNull(stats);

        var statsToUpdate = await GetCharacterStatsByCharacterIdAsync(characterId);

        if (statsToUpdate == null)
        {
            return false;
        }

        statsToUpdate.Strength = stats.Strength;
        statsToUpdate.Dexterity = stats.Dexterity;
        statsToUpdate.Intelligence = stats.Intelligence;
        statsToUpdate.Constitution = stats.Constitution;
        statsToUpdate.Wisdom = stats.Wisdom;
        statsToUpdate.Charisma = stats.Charisma;

        await _characterStatsRepository.UpdateAsync(statsToUpdate);
        return true;
    }

    public async Task<bool> DeleteCharacterStatsAsync(int characterId)
    {
        await EnsureCharacterExistsAsync(characterId);

        var statsToDelete = await GetCharacterStatsByCharacterIdAsync(characterId);

        if (statsToDelete == null)
        {
            return false;
        }

        await _characterStatsRepository.DeleteAsync(statsToDelete);
        return true;
    }

    public async Task BulkInsertCharacterStatsFromJsonAsync(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(jsonFilePath));
        }

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"File not found: {jsonFilePath}");
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var characterStats = JsonConvert.DeserializeObject<List<CharacterStats>>(jsonContent);

        if (characterStats == null || !characterStats.Any())
        {
            throw new InvalidOperationException("No character stats found in JSON file.");
        }

        foreach (var stat in characterStats)
        {
            await EnsureCharacterExistsAsync(stat.CharacterId);
            await EnsureStatsDoNotExistAsync(stat.CharacterId);
        }

        await _characterStatsRepository.AddRangeAsync(characterStats);
        Console.WriteLine($"Successfully inserted {characterStats.Count} character stats from {jsonFilePath}");
    }

    private async Task<Character> EnsureCharacterExistsAsync(int characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }
        return character;
    }

    private async Task EnsureStatsDoNotExistAsync(int characterId)
    {
        var existingStats = await GetCharacterStatsByCharacterIdAsync(characterId);
        if (existingStats != null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} already has stats defined.");
        }
    }

    private static void EnsureStatsIsNotNull(CharacterStats stats)
    {
        if (stats == null)
        {
            throw new ArgumentNullException(nameof(stats));
        }
    }

    private async Task<CharacterStats> GetCharacterStatsByCharacterIdAsync(int characterId)
    {
        var existingStats = await _characterStatsRepository.FindAsync(s => s.CharacterId == characterId);
        return existingStats.FirstOrDefault();
    }
}