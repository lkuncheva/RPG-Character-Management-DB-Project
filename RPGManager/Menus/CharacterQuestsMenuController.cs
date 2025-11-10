using RPGManager.Interfaces;

namespace RPGManager.Menus;

public class CharacterQuestsMenuController : MenuBase
{
    private readonly ICharacterQuestService _characterQuestService;

    protected override string MenuTitle => "Character Quests Management";

    public CharacterQuestsMenuController(ICharacterQuestService characterQuestService)
    {
        _characterQuestService = characterQuestService ?? throw new ArgumentNullException(nameof(characterQuestService));

        MenuActions = new List<MenuAction>
        {
            new("View Character Quests", ViewCharacterQuestsAsync),
            new("Assign Quest to Character", AssignQuestToCharacterAsync),
            new("Update Quest Status", UpdateQuestStatusAsync),
            new("Remove Quest from Character", RemoveQuestFromCharacterAsync),
            new("Bulk Insert Character Quests from JSON", BulkInsertCharacterQuestsAsync)
        };
    }

    private async Task ViewCharacterQuestsAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        try
        {
            var quests = await _characterQuestService.GetCharacterQuestsAsync(characterId);
            if (!quests.Any())
            {
                Console.WriteLine("No quests found for this character.");
                return;
            }

            Console.WriteLine($"\n=== Character Quests ===");
            Console.WriteLine($"Character ID: {characterId}");
            foreach (var quest in quests)
            {
                Console.WriteLine($"- Quest ID: {quest.QuestId}, Status: {quest.Status}, Started: {quest.StartedDate:yyyy-MM-dd HH:mm}, Completed: {(quest.CompletedDate.HasValue ? quest.CompletedDate.Value.ToString("yyyy-MM-dd HH:mm") : "Not completed")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task AssignQuestToCharacterAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        Console.Write("Enter quest ID: ");
        if (!int.TryParse(Console.ReadLine(), out int questId))
        {
            Console.WriteLine("Invalid quest ID.");
            return;
        }

        try
        {
            var assignment = await _characterQuestService.AssignQuestToCharacterAsync(characterId, questId);
            Console.WriteLine($"\nQuest assigned successfully! Assignment ID: {assignment.CharacterId}-{assignment.QuestId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task UpdateQuestStatusAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        Console.Write("Enter quest ID: ");
        if (!int.TryParse(Console.ReadLine(), out int questId))
        {
            Console.WriteLine("Invalid quest ID.");
            return;
        }

        Console.WriteLine("Available statuses: NotStarted, InProgress, Completed, Failed");
        Console.Write("Enter new status: ");
        var status = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(status))
        {
            Console.WriteLine("Invalid status.");
            return;
        }

        try
        {
            var success = await _characterQuestService.UpdateQuestStatusAsync(characterId, questId, status);
            Console.WriteLine(success ? "\nQuest status updated successfully!" : "\nFailed to update quest status. Check character and quest IDs.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task RemoveQuestFromCharacterAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        Console.Write("Enter quest ID: ");
        if (!int.TryParse(Console.ReadLine(), out int questId))
        {
            Console.WriteLine("Invalid quest ID.");
            return;
        }

        Console.Write("Are you sure you want to remove this quest from the character? (yes/no): ");
        var confirmation = Console.ReadLine();

        if (confirmation?.ToLower() == "yes")
        {
            try
            {
                var success = await _characterQuestService.RemoveQuestFromCharacterAsync(characterId, questId);
                Console.WriteLine(success ? "\nQuest removed successfully!" : "\nFailed to remove quest. Check character and quest IDs.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("\nOperation cancelled.");
        }
    }

    private async Task BulkInsertCharacterQuestsAsync()
    {
        Console.Write("\nEnter JSON file path for character quests: ");
        Console.WriteLine("(../../../SampleData/character_quests.json)");
        var filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Invalid file path.");
            return;
        }

        try
        {
            await _characterQuestService.BulkInsertCharacterQuestsFromJsonAsync(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}