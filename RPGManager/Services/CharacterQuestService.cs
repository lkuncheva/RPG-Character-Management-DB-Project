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
        _characterRepository = characterRepository;
        _characterQuestRepository = characterQuestRepository;
        _questRepository = questRepository;
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
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status cannot be empty.", nameof(status));
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
            var character = await _characterRepository.GetByIdAsync(characterId);
            var quest = await _questRepository.GetByIdAsync(questId);

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
            throw new ArgumentException("File path cannot be empty.", nameof(jsonFilePath));
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

        foreach (var quest in characterQuests)
        {
            var character = await _characterRepository.GetByIdAsync(quest.CharacterId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character with ID {quest.CharacterId} not found.");
            }

            var existingAssignment = await _characterQuestRepository.FindAsync(cq => cq.CharacterId == quest.CharacterId && cq.QuestId == quest.QuestId);
            if (existingAssignment.Any())
            {
                throw new InvalidOperationException($"Quest with ID {quest.QuestId} is already assigned to character with ID {quest.CharacterId}.");
            }

            if (string.IsNullOrWhiteSpace(quest.Status))
            {
                quest.Status = "NotStarted";
            }

            if (quest.StartedDate == default && quest.Status != "NotStarted")
            {
                quest.StartedDate = DateTime.UtcNow;
            }
        }

        await _characterQuestRepository.AddRangeAsync(characterQuests);
        Console.WriteLine($"Successfully inserted {characterQuests.Count} character quests from {jsonFilePath}");
    }
}