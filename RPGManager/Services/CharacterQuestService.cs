using Newtonsoft.Json;
using RPGManager.Interfaces;
using RPGManager.Models;

namespace RPGManager.Services;
public class CharacterQuestService : ICharacterQuestService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IRepository<CharacterQuest> _characterQuestRepository;
    private readonly IRepository<Quest> _questRepository;

    public CharacterQuestService(
        ICharacterRepository characterRepository,
        IRepository<CharacterQuest> characterQuestRepository,
        IRepository<Quest> questRepository)
    {
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
        _characterQuestRepository = characterQuestRepository ?? throw new ArgumentNullException(nameof(characterQuestRepository));
        _questRepository = questRepository ?? throw new ArgumentNullException(nameof(questRepository));
    }

    public async Task<IEnumerable<CharacterQuest>> GetCharacterQuestsAsync(int characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        return await _characterQuestRepository.FindAsync(cq => cq.CharacterId == characterId);
    }

    public async Task<CharacterQuest> AssignQuestToCharacterAsync(int characterId, int questId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        var quest = await _questRepository.GetByIdAsync(questId);
        if (quest == null)
        {
            throw new ArgumentException($"Quest with ID {questId} not found.");
        }

        var existingAssignment = await _characterQuestRepository.FindAsync(
            cq => cq.CharacterId == characterId && cq.QuestId == questId);
        if (existingAssignment.Any())
        {
            throw new InvalidOperationException(
                $"Quest with ID {questId} is already assigned to character with ID {characterId}.");
        }

        if (character.Level < quest.RequiredLevel)
        {
            throw new InvalidOperationException(
                $"Cannot assign quest: Character level ({character.Level}) is too low. Required Level: {quest.RequiredLevel}.");
        }

        var characterQuest = new CharacterQuest
        {
            CharacterId = characterId,
            QuestId = questId,
            Status = "NotStarted",
            StartedDate = DateTime.UtcNow
        };

        await _characterQuestRepository.AddRangeAsync([characterQuest]);
        return characterQuest;
    }

    public async Task<bool> UpdateQuestStatusAsync(int characterId, int questId, string status)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        var quest = await _questRepository.GetByIdAsync(questId);
        if (quest == null)
        {
            throw new InvalidOperationException($"Quest with ID {questId} not found.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status cannot be empty or whitespace.", nameof(status));
        }

        var validStatuses = new[] { "NotStarted", "InProgress", "Completed", "Failed" };
        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");
        }

        var characterQuest = await _characterQuestRepository.FindAsync(
            cq => cq.CharacterId == characterId && cq.QuestId == questId);

        var questToUpdate = characterQuest.FirstOrDefault();
        if (questToUpdate == null)
        {
            return false;
        }

        if (questToUpdate.Status == status)
        {
            return true;
        }

        if (status == "Failed")
        {
            questToUpdate.CompletedDate = DateTime.UtcNow;
        }

        if (status == "Completed")
        {
            if (character != null && quest != null)
            {
                character.Gold += quest.RewardGold;
                character.Experience += quest.RewardExperience;

                await _characterRepository.UpdateAsync(character);
            }

            questToUpdate.CompletedDate = DateTime.UtcNow;
        }

        questToUpdate.Status = status;
        await _characterQuestRepository.UpdateAsync(questToUpdate);

        return true;
    }

    public async Task<bool> RemoveQuestFromCharacterAsync(int characterId, int questId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        var quest = await _questRepository.GetByIdAsync(questId);
        if (quest == null)
        {
            throw new InvalidOperationException($"Quest with ID {questId} not found.");
        }

        var characterQuest = await _characterQuestRepository.FindAsync(cq => cq.CharacterId == characterId && cq.QuestId == questId);
        var questToDelete = characterQuest.FirstOrDefault();

        if (questToDelete == null)
        {
            return false;
        }

        await _characterQuestRepository.DeleteAsync(questToDelete);
        return true;
    }

    public async Task BulkInsertCharacterQuestsFromJsonAsync(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException("File path cannot be empty or whitespace.", nameof(jsonFilePath));
        }

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"File not found: {jsonFilePath}");
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var characterQuests = JsonConvert.DeserializeObject<List<CharacterQuest>>(jsonContent);

        if (characterQuests == null || !characterQuests.Any())
        {
            throw new InvalidOperationException("No character quests found in JSON file.");
        }

        foreach (var charQuest in characterQuests)
        {
            var character = await _characterRepository.GetByIdAsync(charQuest.CharacterId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character with ID {charQuest.CharacterId} not found.");
            }

            var quest = await _questRepository.GetByIdAsync(charQuest.QuestId);
            if (quest == null)
            {
                throw new InvalidOperationException($"Quest with ID {charQuest.QuestId} not found.");
            }

            var existingAssignment = await _characterQuestRepository.FindAsync(cq => cq.CharacterId == charQuest.CharacterId && cq.QuestId == charQuest.QuestId);
            if (existingAssignment.Any())
            {
                throw new InvalidOperationException($"Quest with ID {charQuest.QuestId} is already assigned to character with ID {charQuest.CharacterId}.");
            }

            if (string.IsNullOrWhiteSpace(charQuest.Status))
            {
                charQuest.Status = "NotStarted";
            }

            if (charQuest.StartedDate == default && charQuest.Status != "NotStarted")
            {
                charQuest.StartedDate = DateTime.UtcNow;
            }
        }

        await _characterQuestRepository.AddRangeAsync(characterQuests);
        Console.WriteLine($"Successfully inserted {characterQuests.Count} character quests from {jsonFilePath}");
    }
}