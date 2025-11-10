namespace RPGManager.Menus;

public record MenuAction(string Description, Func<Task> Action);

public abstract class MenuBase
{
    protected abstract string MenuTitle { get; }
    protected virtual string ExitOption => "Back to Main Menu";

    protected List<MenuAction> MenuActions { get; set; } = new List<MenuAction>();

    public async Task ShowMenuAsync()
    {
        bool exit = false;
        while (!exit)
        {
            Console.WriteLine($"\n=== {MenuTitle} ===");

            for (int i = 0; i < MenuActions.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {MenuActions[i].Description}");
            }

            Console.WriteLine($"0. {ExitOption}");
            Console.Write("\nSelect an option: ");

            var choice = Console.ReadLine();

            try
            {
                if (int.TryParse(choice, out int selectedIndex))
                {
                    if (selectedIndex == 0)
                    {
                        exit = true;
                    }
                    else if (selectedIndex >= 1 && selectedIndex <= MenuActions.Count)
                    {
                        await MenuActions[selectedIndex - 1].Action.Invoke();
                    }
                    else
                    {
                        Console.WriteLine("\nInvalid option.");
                    }
                }
                else
                {
                    Console.WriteLine("\nInvalid input. Please enter a number.");
                }
            }
            catch (ArgumentException aex)
            {
                Console.WriteLine($"\nValidation Error: {aex.Message}");
            }
            catch (InvalidOperationException ioex)
            {
                Console.WriteLine($"\nOperation Failed: {ioex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nInternal Error: An unexpected error occurred: {ex.Message}");
            }
        }
    }
}