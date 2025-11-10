using RPGManager.Interfaces;

namespace RPGManager.Menus;

public class CharacterEquipmentMenuController : MenuBase
{
    private readonly ICharacterEquipmentService _characterEquipmentService;

    protected override string MenuTitle => "Character Equipment Management";

    public CharacterEquipmentMenuController(ICharacterEquipmentService characterEquipmentService)
    {
        _characterEquipmentService = characterEquipmentService ?? throw new ArgumentNullException(nameof(characterEquipmentService));

        MenuActions = new List<MenuAction>
        {
            new("View Character Equipment", ViewCharacterEquipmentAsync),
            new("Assign Equipment to Character", AssignEquipmentToCharacterAsync),
            new("Toggle Equipment Status (Equip/Unequip)", ToggleEquipmentStatusAsync),
            new("Remove Equipment from Character", RemoveEquipmentFromCharacterAsync),
            new("Bulk Insert Character Equipment from JSON", BulkInsertCharacterEquipmentAsync)
        };
    }

    private async Task ViewCharacterEquipmentAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        try
        {
            var equipment = await _characterEquipmentService.GetCharacterEquipmentAsync(characterId);
            if (!equipment.Any())
            {
                Console.WriteLine("No equipment found for this character.");
                return;
            }

            Console.WriteLine($"\n=== Character Equipment ===");
            Console.WriteLine($"Character ID: {characterId}");
            foreach (var eq in equipment)
            {
                var status = eq.IsEquipped ? "[EQUIPPED]" : "[IN BAG]";
                Console.WriteLine($"- Equipment ID: {eq.EquipmentId} {status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task AssignEquipmentToCharacterAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        Console.Write("Enter equipment ID: ");
        if (!int.TryParse(Console.ReadLine(), out int equipmentId))
        {
            Console.WriteLine("Invalid equipment ID.");
            return;
        }

        try
        {
            var assignment = await _characterEquipmentService.AssignEquipmentToCharacterAsync(characterId, equipmentId);
            Console.WriteLine($"\nEquipment assigned successfully! Assignment ID: {assignment.CharacterId}-{assignment.EquipmentId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task ToggleEquipmentStatusAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        Console.Write("Enter equipment ID: ");
        if (!int.TryParse(Console.ReadLine(), out int equipmentId))
        {
            Console.WriteLine("Invalid equipment ID.");
            return;
        }

        try
        {
            var success = await _characterEquipmentService.ToggleEquipmentStatusAsync(characterId, equipmentId);
            if (success)
            {
                Console.WriteLine("\nEquipment status toggled successfully!");
            }
            else
            {
                Console.WriteLine("\nFailed to toggle equipment status. Check character and equipment IDs.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task RemoveEquipmentFromCharacterAsync()
    {
        Console.Write("\nEnter character ID: ");
        if (!int.TryParse(Console.ReadLine(), out int characterId))
        {
            Console.WriteLine("Invalid character ID.");
            return;
        }

        Console.Write("Enter equipment ID: ");
        if (!int.TryParse(Console.ReadLine(), out int equipmentId))
        {
            Console.WriteLine("Invalid equipment ID.");
            return;
        }

        Console.Write("Are you sure you want to remove this equipment from the character? (yes/no): ");
        var confirmation = Console.ReadLine();

        if (confirmation?.ToLower() == "yes")
        {
            try
            {
                var success = await _characterEquipmentService.RemoveEquipmentFromCharacterAsync(characterId, equipmentId);
                Console.WriteLine(success ? "\nEquipment removed successfully!" : "\nFailed to remove equipment. Check character and equipment IDs.");
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

    private async Task BulkInsertCharacterEquipmentAsync()
    {
        Console.Write("\nEnter JSON file path for character equipment: ");
        Console.WriteLine("(../../../SampleData/character_equipment.json)");
        var filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Invalid file path.");
            return;
        }

        try
        {
            await _characterEquipmentService.BulkInsertCharacterEquipmentFromJsonAsync(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}