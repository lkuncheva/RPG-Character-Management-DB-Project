using Newtonsoft.Json;
using RPGManager.Models;
using RPGManager.Interfaces;

namespace RPGManager.Services;

public class QuestService : IQuestService
{
    private readonly IRepository<Quest> _questRepository;

    public QuestService(IRepository<Quest> questRepository)
    {
        _questRepository = questRepository ?? throw new ArgumentNullException(nameof(questRepository));
    }

    public async Task<Quest> CreateQuestAsync(Quest quest)
    {
        if (quest == null)
        {
            throw new ArgumentNullException(nameof(quest));
        }

        if (string.IsNullOrWhiteSpace(quest.Title))
        {
            throw new ArgumentException("Quest title cannot be empty.", nameof(quest));
        }

        if (quest.Title.Length > 200)
        {
            throw new ArgumentException($"Quest title cannot exceed 200 characters.", nameof(quest));
        }

        if (quest.Description.Length > 1000)
        {
            throw new ArgumentException($"Quest description cannot exceed 1000 characters.", nameof(quest));
        }

        if (quest.RewardGold < 0)
        {
            throw new ArgumentException("Reward gold cannot be negative.", nameof(quest));
        }

        if (quest.RewardExperience < 0)
        {
            throw new ArgumentException("Reward experience cannot be negative.", nameof(quest));
        }

        if (quest.RequiredLevel < 1)
        {
            throw new ArgumentException("Required level must be at least 1.", nameof(quest));
        }

        if (!string.IsNullOrEmpty(quest.Difficulty) &&
            quest.Difficulty != "Easy" && quest.Difficulty != "Medium" &&
            quest.Difficulty != "Hard" && quest.Difficulty != "Expert")
        {
            throw new ArgumentException("Difficulty must be one of: Easy, Medium, Hard, Expert.", nameof(quest));
        }

        await _questRepository.AddRangeAsync([quest]);
        return quest;
    }

    public async Task BulkInsertQuestsFromJsonAsync(string jsonFilePath)
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
        var quests = JsonConvert.DeserializeObject<List<Quest>>(jsonContent);

        if (quests == null || !quests.Any())
        {
            throw new InvalidOperationException("No quests found in JSON file.");
        }

        await _questRepository.AddRangeAsync(quests);
        Console.WriteLine($"Successfully inserted {quests.Count} quests from {jsonFilePath}");
    }

    public async Task<Quest> GetQuestByIdAsync(int id)
    {
        return await _questRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Quest>> GetAllQuestsAsync()
    {
        return await _questRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Quest>> GetQuestsByDifficultyAsync(string difficulty)
    {
        if (difficulty == null || difficulty == string.Empty)
        {
            return await _questRepository.FindAsync(q => string.IsNullOrEmpty(q.Difficulty));
        }

        if (string.IsNullOrWhiteSpace(difficulty))
        {
            throw new ArgumentException("Difficulty filter cannot be composed only of whitespace.", nameof(difficulty));
        }

        return await _questRepository.FindAsync(q => q.Difficulty == difficulty);
    }

    public async Task ExportQuestsToJsonAsync(string outputFilePath, string difficulty = null)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            throw new ArgumentException("Output file path cannot be empty.", nameof(outputFilePath));
        }

        IEnumerable<Quest> quests;
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            quests = await GetQuestsByDifficultyAsync(difficulty);
        }
        else
        {
            quests = await GetAllQuestsAsync();
        }

        var jsonContent = JsonConvert.SerializeObject(quests, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        await File.WriteAllTextAsync(outputFilePath, jsonContent);
        Console.WriteLine($"Successfully exported {quests.Count()} quests to {outputFilePath}");
    }

    public async Task<bool> UpdateQuestRewardsAsync(int questId, int newGold, int newExperience)
    {
        var quest = await _questRepository.GetByIdAsync(questId);
        if (quest == null)
        {
            return false;
        }

        if (newGold < 0)
        {
            throw new ArgumentException("Reward gold cannot be negative.", nameof(newGold));
        }

        if (newExperience < 0)
        {
            throw new ArgumentException("Reward experience cannot be negative.", nameof(newExperience));
        }

        quest.RewardGold = newGold;
        quest.RewardExperience = newExperience;

        await _questRepository.UpdateAsync(quest);
        return true;
    }

    public async Task<bool> DeleteQuestAsync(int questId)
    {
        var quest = await _questRepository.GetByIdAsync(questId);
        if (quest == null)
        {
            return false;
        }

        await _questRepository.DeleteAsync(quest);
        return true;
    }
}