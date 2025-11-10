using RPGManager.Interfaces;

namespace RPGManager.Menus;

public class MainMenuController : MenuBase
{
    private readonly CharacterMenuController _characterMenu;
    private readonly CharacterClassMenuController _characterClassMenu;
    private readonly QuestMenuController _questMenu;
    private readonly EquipmentMenuController _equipmentMenu;
    private readonly CharacterStatsMenuController _characterStatsMenu;
    private readonly CharacterQuestsMenuController _characterQuestsMenu;
    private readonly CharacterEquipmentMenuController _characterEquipmentMenu;
    private readonly IDataSeederService _seederService;

    protected override string MenuTitle => "Main Menu";
    protected override string ExitOption => "Exit";


    public MainMenuController(
        CharacterMenuController characterMenu,
        CharacterClassMenuController characterClassMenu,
        QuestMenuController questMenu,
        EquipmentMenuController equipmentMenu,
        CharacterStatsMenuController characterStatsMenu,
        CharacterQuestsMenuController characterQuestsMenu,
        CharacterEquipmentMenuController characterEquipmentMenu,
        IDataSeederService seederService)
    {
        _characterMenu = characterMenu;
        _characterClassMenu = characterClassMenu;
        _questMenu = questMenu;
        _equipmentMenu = equipmentMenu;
        _characterStatsMenu = characterStatsMenu;
        _characterQuestsMenu = characterQuestsMenu;
        _characterEquipmentMenu = characterEquipmentMenu;
        _seederService = seederService;

        MenuActions = new List<MenuAction>
        {
            new("Character Management", () => _characterMenu.ShowMenuAsync()),
            new("Character Class Management", () => _characterClassMenu.ShowMenuAsync()),
            new("Quest Management", () => _questMenu.ShowMenuAsync()),
            new("Equipment Management", () => _equipmentMenu.ShowMenuAsync()),
            new("Character Stats Management", () => _characterStatsMenu.ShowMenuAsync()),
            new("Character Quests Management", () => _characterQuestsMenu.ShowMenuAsync()),
            new("Character Equipment Management", () => _characterEquipmentMenu.ShowMenuAsync()),
            new("Seed Sample Data", SeedSampleDataAsync)
        };
    }

    private async Task SeedSampleDataAsync()
    {
        try
        {
            Console.WriteLine("\n--- Seeding Data ---");
            await _seederService.SeedAllSampleDataAsync();
            Console.WriteLine("Sample data seeded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError seeding data: {ex.Message}");
        }
    }
}