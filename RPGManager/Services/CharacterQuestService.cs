using Newtonsoft.Json;
using RPGManager.Interfaces;
using RPGManager.Data.Interfaces;
using RPGManager.Data.Models;

namespace RPGManager.Services;

public enum QuestStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

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
        await EnsureCharacterExistsAsync(characterId);

        return await _characterQuestRepository.FindAsync(cq => cq.CharacterId == characterId);
    }

    public async Task<CharacterQuest> AssignQuestToCharacterAsync(int characterId, int questId)
    {
        var character = await EnsureCharacterExistsAsync(characterId);
        var quest = await EnsureQuestExistsAsync(questId);

        await EnsureAssignmentDoesNotExistAsync(characterId, questId);
        EnsureCharacterMeetsQuestLevel(character, quest);

        var characterQuest = new CharacterQuest
        {
            CharacterId = characterId,
            QuestId = questId,
            Status = QuestStatus.NotStarted.ToString(),
            StartedDate = DateTime.UtcNow
        };

        await _characterQuestRepository.AddRangeAsync([characterQuest]);
        return characterQuest;
    }

    public async Task<bool> UpdateQuestStatusAsync(int characterId, int questId, int statusNumber)
    {
        if (!Enum.IsDefined(typeof(QuestStatus), statusNumber))
        {
            var validValues = string.Join(", ", Enum.GetValues<QuestStatus>().Cast<int>());
            throw new ArgumentException($"Invalid status number '{statusNumber}'. Must be one of the valid integer enum values: {validValues}");
        }

        var newStatus = (QuestStatus)statusNumber;

        return await UpdateQuestStatusInternalAsync(characterId, questId, newStatus);
    }

    private async Task<bool> UpdateQuestStatusInternalAsync(int characterId, int questId, QuestStatus newStatus)
    {
        var character = await EnsureCharacterExistsAsync(characterId);
        var quest = await EnsureQuestExistsAsync(questId);

        var characterQuest = await _characterQuestRepository.FindAsync(
            cq => cq.CharacterId == characterId && cq.QuestId == questId);

        var questToUpdate = characterQuest.FirstOrDefault();
        if (questToUpdate == null)
        {
            return false;
        }

        if (Enum.TryParse<QuestStatus>(questToUpdate.Status, out var currentStatus) && currentStatus == newStatus)
        {
            return true;
        }

        if (newStatus == QuestStatus.Failed)
        {
            questToUpdate.CompletedDate = DateTime.UtcNow;
        }

        if (newStatus == QuestStatus.Completed && currentStatus != QuestStatus.Completed)
        {
            if (character != null && quest != null)
            {
                character.Gold += quest.RewardGold;
                character.Experience += quest.RewardExperience;
                await _characterRepository.UpdateAsync(character);
            }
        }

        if (newStatus == QuestStatus.Completed || newStatus == QuestStatus.Failed)
        {
            questToUpdate.CompletedDate = DateTime.UtcNow;
        }
        else if (newStatus == QuestStatus.InProgress || newStatus == QuestStatus.NotStarted)
        {
            questToUpdate.CompletedDate = null;
        }

        questToUpdate.Status = newStatus.ToString();
        await _characterQuestRepository.UpdateAsync(questToUpdate);

        return true;
    }

    public async Task<bool> RemoveQuestFromCharacterAsync(int characterId, int questId)
    {
        await EnsureCharacterExistsAsync(characterId);
        await EnsureQuestExistsAsync(questId);

        var characterQuest = await _characterQuestRepository.FindAsync(
            cq => cq.CharacterId == characterId && cq.QuestId == questId);

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
            var character = await EnsureCharacterExistsAsync(charQuest.CharacterId);
            var quest = await EnsureQuestExistsAsync(charQuest.QuestId);

            await EnsureAssignmentDoesNotExistAsync(charQuest.CharacterId, charQuest.QuestId);
            EnsureCharacterMeetsQuestLevel(character, quest);

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

    private async Task<Character> EnsureCharacterExistsAsync(int characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }
        return character;
    }

    private async Task<Quest> EnsureQuestExistsAsync(int questId)
    {
        var quest = await _questRepository.GetByIdAsync(questId);
        if (quest == null)
        {
            throw new InvalidOperationException($"Quest with ID {questId} not found.");
        }

        return quest;
    }

    private async Task EnsureAssignmentDoesNotExistAsync(int characterId, int questId)
    {
        var existingAssignment = await _characterQuestRepository.FindAsync(
            cq => cq.CharacterId == characterId && cq.QuestId == questId);

        if (existingAssignment.Any())
        {
            throw new InvalidOperationException(
                $"Quest with ID {questId} is already assigned to character with ID {characterId}.");
        }
    }

    private static void EnsureCharacterMeetsQuestLevel(Character character, Quest quest)
    {
        if (character.Level < quest.RequiredLevel)
        {
            throw new InvalidOperationException(
                $"Cannot assign quest: Character level ({character.Level}) is too low. Required Level: {quest.RequiredLevel}.");
        }
    }
}