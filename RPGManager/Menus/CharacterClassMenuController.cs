using RPGManager.Interfaces;
using RPGManager.Models;
using RPGManager.Services;

namespace RPGManager.Menus;

public class CharacterClassMenuController : MenuBase
{
    private readonly ICharacterClassService _characterClassService;

    protected override string MenuTitle => "Character Class Management";

    public CharacterClassMenuController(ICharacterClassService characterClassService)
    {
        _characterClassService = characterClassService ?? throw new ArgumentNullException(nameof(characterClassService));

        MenuActions = new List<MenuAction>
        {
            new("Create New Class", CreateClassAsync),
            new("View All Classes", ViewAllClassesAsync),
            new("View Class Details by ID", ViewClassByIdAsync),
            new("Update Existing Class", UpdateClassAsync),
            new("Delete Class", DeleteClassAsync)
        };
    }

    private async Task ViewAllClassesAsync()
    {
        Console.WriteLine("\n--- All Character Classes ---");
        var classes = await _characterClassService.GetAllClassesAsync();

        if (!classes.Any())
        {
            Console.WriteLine("No character classes have been defined yet.");
            return;
        }

        foreach (var cls in classes)
        {
            Console.WriteLine($"[ID: {cls.Id}] {cls.Name} (HP: {cls.BaseHealth}, Mana: {cls.BaseMana}) - Primary: {cls.PrimaryAttribute}");
        }
    }

    private async Task ViewClassByIdAsync()
    {
        Console.Write("\nEnter Class ID to view: ");
        if (!int.TryParse(Console.ReadLine(), out int classId))
        {
            Console.WriteLine("Invalid Class ID format.");
            return;
        }

        var cls = await _characterClassService.GetClassByIdWithCharactersAsync(classId);

        if (cls == null)
        {
            Console.WriteLine($"\nError: Character Class with ID {classId} not found.");
            return;
        }

        Console.WriteLine("\n--- Class Details ---");
        Console.WriteLine($"ID: {cls.Id}");
        Console.WriteLine($"Name: {cls.Name}");
        Console.WriteLine($"Description: {cls.Description}");
        Console.WriteLine($"Base HP: {cls.BaseHealth} | Base MP: {cls.BaseMana}");
        Console.WriteLine($"Primary Attribute: {cls.PrimaryAttribute}");
        Console.WriteLine($"Total Characters of this class: {cls.Characters.Count}");
    }

    private async Task CreateClassAsync()
    {
        Console.WriteLine("\n--- Create New Character Class ---");

        Console.Write("Name (required, max 50 chars): ");
        var name = Console.ReadLine() ?? string.Empty;

        Console.Write("Description (max 500 chars): ");
        var description = Console.ReadLine() ?? string.Empty;

        Console.Write("Base Health (integer): ");
        if (!int.TryParse(Console.ReadLine(), out int baseHealth))
        {
            Console.WriteLine("Invalid Health value. Defaulting to 0.");
        }

        Console.Write("Base Mana (integer): ");
        if (!int.TryParse(Console.ReadLine(), out int baseMana))
        {
            Console.WriteLine("Invalid Mana value. Defaulting to 0.");
        }

        Console.Write("Primary Attribute (max 50 chars): ");
        var primaryAttribute = Console.ReadLine() ?? string.Empty;

        var newClass = new CharacterClass
        {
            Name = name,
            Description = description,
            BaseHealth = baseHealth,
            BaseMana = baseMana,
            PrimaryAttribute = primaryAttribute
        };

        var createdClass = await _characterClassService.CreateClassAsync(newClass);
        Console.WriteLine($"\nSUCCESS: Character Class '{createdClass.Name}' created with ID {createdClass.Id}.");
    }

    private async Task UpdateClassAsync()
    {
        Console.Write("\nEnter Class ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int classId))
        {
            Console.WriteLine("Invalid Class ID format.");
            return;
        }

        var existingClass = await _characterClassService.GetClassByIdAsync(classId);

        if (existingClass == null)
        {
            Console.WriteLine($"\nError: Character Class with ID {classId} not found.");
            return;
        }

        Console.WriteLine($"\n--- Updating Class: {existingClass.Name} ---");
        Console.WriteLine("Enter new values (leave blank to keep current value):");

        Console.Write($"New Name (Current: {existingClass.Name}): ");
        var newName = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newName))
        {
            existingClass.Name = newName;
        }

        Console.Write($"New Description (Current: {existingClass.Description.Substring(0, Math.Min(30, existingClass.Description.Length))}...): ");
        var newDescription = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newDescription))
        {
            existingClass.Description = newDescription;
        }

        Console.Write($"New Base Health (Current: {existingClass.BaseHealth}): ");
        var healthInput = Console.ReadLine();
        if (int.TryParse(healthInput, out int newHealth))
        {
            existingClass.BaseHealth = newHealth;
        }

        Console.Write($"New Base Mana (Current: {existingClass.BaseMana}): ");
        var manaInput = Console.ReadLine();
        if (int.TryParse(manaInput, out int newMana))
        {
            existingClass.BaseMana = newMana;
        }

        Console.Write($"New Primary Attribute (Current: {existingClass.PrimaryAttribute}): ");
        var newAttribute = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newAttribute))
        {
            existingClass.PrimaryAttribute = newAttribute;
        }

        await _characterClassService.UpdateClassAsync(existingClass);
        Console.WriteLine($"\nSUCCESS: Character Class '{existingClass.Name}' (ID {existingClass.Id}) updated successfully.");
    }

    private async Task DeleteClassAsync()
    {
        Console.Write("\nEnter Class ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int classId))
        {
            Console.WriteLine("Invalid Class ID format.");
            return;
        }

        var cls = await _characterClassService.GetClassByIdWithCharactersAsync(classId);

        if (cls == null)
        {
            Console.WriteLine($"\nError: Character Class with ID {classId} not found.");
            return;
        }

        int defaultClassId = -1;
        bool hasDependents = cls.Characters.Any();

        if (hasDependents)
        {
            Console.WriteLine($"\nWARNING: Deleting class '{cls.Name}' (ID {classId}) will affect {cls.Characters.Count} characters.");

            Console.Write("Enter the TARGET Class ID to reassign these characters to: ");
            if (!int.TryParse(Console.ReadLine(), out defaultClassId))
            {
                Console.WriteLine("Invalid Target Class ID format. Reassignment setup aborted.");
                return;
            }
        }

        Console.Write("\nConfirm deletion and reassignment (YES/no): ");
        var confirmation = Console.ReadLine();

        if (confirmation?.ToUpper() == "YES")
        {
            try
            {
                bool success = await _characterClassService.DeleteClassAsync(classId, defaultClassId);

                if (success)
                {
                    if (hasDependents)
                    {
                        Console.WriteLine($"\nSUCCESS: Class '{cls.Name}' deleted. {cls.Characters.Count} characters successfully reassigned to class ID {defaultClassId}.");
                    }
                    else
                    {
                        Console.WriteLine($"\nSUCCESS: Character Class '{cls.Name}' deleted.");
                    }
                }
                else
                {
                    Console.WriteLine($"\nError: Failed to delete Character Class with ID {classId}.");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"\nOperation Failed: {ex.Message}");
                Console.WriteLine("Deletion cancelled. Please ensure the target reassignment class exists.");
            }
        }
        else
        {
            Console.WriteLine("\nDeletion cancelled.");
        }
    }
}