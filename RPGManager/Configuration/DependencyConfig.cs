using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RPGManager.Data;
using RPGManager.Interfaces;
using RPGManager.Menus;
using RPGManager.Repositories;
using RPGManager.Services;

namespace RPGManager.Configuration;

public class DependencyConfig
{
    public static IContainer Configure()
    {
        var builder = new ContainerBuilder();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

        builder.Register(c =>
        {
            var config = c.Resolve<IConfiguration>();
            string connectionString = config.GetConnectionString("RpgDbContext");

            var optionsBuilder = new DbContextOptionsBuilder<RPGManagerContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new RPGManagerContext(optionsBuilder.Options);
        }).AsSelf().InstancePerLifetimeScope();

        builder.RegisterGeneric(typeof(Repository<>))
            .As(typeof(IRepository<>))
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterRepository>()
            .As<ICharacterRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterClassRepository>()
            .As<ICharacterClassRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterService>()
            .As<ICharacterService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterClassService>()
            .As<ICharacterClassService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterStatsService>()
            .As<ICharacterStatsService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterQuestService>()
            .As<ICharacterQuestService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterEquipmentService>()
            .As<ICharacterEquipmentService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<QuestService>()
            .As<IQuestService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EquipmentService>()
            .As<IEquipmentService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<DataSeederService>()
            .As<IDataSeederService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CharacterMenuController>()
            .AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<CharacterClassMenuController>()
            .AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<QuestMenuController>()
            .AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<EquipmentMenuController>()
            .AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<CharacterStatsMenuController>()
            .AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<CharacterQuestsMenuController>()
            .AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<CharacterEquipmentMenuController>()
            .AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<MainMenuController>()
            .AsSelf().InstancePerLifetimeScope();

        return builder.Build();
    }
}