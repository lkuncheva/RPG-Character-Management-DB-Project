using Autofac;
using Microsoft.EntityFrameworkCore;
using RPGManager.Configuration;
using RPGManager.Data;
using RPGManager.Menus;

namespace RPGManager;

public class Program
{
    private static IContainer _container;

    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Fantasy RPG Character Manager");
        Console.WriteLine("===========================================\n");

        _container = DependencyConfig.Configure();

        await InitializeDatabaseAsync();

        await RunMainMenuAsync();
    }

    private static async Task InitializeDatabaseAsync()
    {
        using var scope = _container!.BeginLifetimeScope();
        var context = scope.Resolve<RpgDbContext>();

        Console.WriteLine("Initializing database...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Database initialized successfully!\n");
    }

    private static async Task RunMainMenuAsync()
    {
        using var scope = _container!.BeginLifetimeScope();
        var mainMenu = scope.Resolve<MainMenuController>();

        await mainMenu.ShowMenuAsync();

        Console.WriteLine("\nThank you for using Fantasy RPG Character Manager!");
    }
}