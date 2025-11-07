using Newtonsoft.Json;
using RPGManager.Models;
using RPGManager.Interfaces;

namespace RPGManager.Services;

public class EquipmentService : IEquipmentService
{
    private readonly IRepository<Equipment> _equipmentRepository;

    private static readonly string[] ValidTypes = { "Armor", "Weapon", "Accessory" };
    private static readonly string[] ValidRarities = { "Common", "Rare", "Epic", "Legendary", "Uncommon" };

    public EquipmentService(IRepository<Equipment> equipmentRepository)
    {
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
    }

    public async Task<Equipment> CreateEquipmentAsync(Equipment equipment)
    {
        ValidateEquipmentData(equipment);

        await _equipmentRepository.AddRangeAsync([equipment]);
        return equipment;
    }

    public async Task BulkInsertEquipmentFromJsonAsync(string jsonFilePath)
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
        var equipmentList = JsonConvert.DeserializeObject<List<Equipment>>(jsonContent);

        if (equipmentList == null || !equipmentList.Any())
        {
            throw new InvalidOperationException("No equipment found in JSON file.");
        }

        foreach (var equipment in equipmentList)
        {
            ValidateEquipmentData(equipment);
        }

        await _equipmentRepository.AddRangeAsync(equipmentList);
        Console.WriteLine($"Successfully inserted {equipmentList.Count} equipment items from {jsonFilePath}");
    }

    public async Task<Equipment> GetEquipmentByIdAsync(int id)
    {
        return await _equipmentRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Equipment>> GetAllEquipmentAsync()
    {
        return await _equipmentRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Equipment>> GetEquipmentByRarityAsync(string rarity)
    {
        if (string.IsNullOrEmpty(rarity))
        {
            return await _equipmentRepository.FindAsync(e => string.IsNullOrEmpty(e.Rarity));
        }

        if (string.IsNullOrWhiteSpace(rarity))
        {
            throw new ArgumentException("Rarity filter cannot be composed only of whitespace.", nameof(rarity));
        }

        if (!ValidRarities.Contains(rarity))
        {
            throw new ArgumentException($"Rarity filter '{rarity}' is invalid. Must be one of: {string.Join(", ", ValidRarities)}.", nameof(rarity));
        }

        return await _equipmentRepository.FindAsync(e => e.Rarity == rarity);
    }

    public async Task ExportEquipmentToJsonAsync(string outputFilePath, string rarity = null)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            throw new ArgumentException("Output file path cannot be empty.", nameof(outputFilePath));
        }

        IEnumerable<Equipment> equipment;
        if (!string.IsNullOrWhiteSpace(rarity))
        {
            equipment = await GetEquipmentByRarityAsync(rarity);
        }
        else
        {
            equipment = await GetAllEquipmentAsync();
        }

        var jsonContent = JsonConvert.SerializeObject(equipment, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        await File.WriteAllTextAsync(outputFilePath, jsonContent);
        Console.WriteLine($"Successfully exported {equipment.Count()} equipment items to {outputFilePath}");
    }

    public async Task<bool> UpdateEquipmentBonusesAsync(int equipmentId, int newAttackBonus, int newDefenceBonus)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
        if (equipment == null)
        {
            return false;
        }

        ValidateBonus(newAttackBonus, nameof(newAttackBonus), "Attack bonus");
        ValidateBonus(newDefenceBonus, nameof(newDefenceBonus), "Defense bonus");

        equipment.AttackBonus = newAttackBonus;
        equipment.DefenseBonus = newDefenceBonus;

        await _equipmentRepository.UpdateAsync(equipment);
        return true;
    }

    public async Task<bool> DeleteEquipmentAsync(int equipmentId)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
        if (equipment == null)
        {
            return false;
        }

        await _equipmentRepository.DeleteAsync(equipment);
        return true;
    }

    private static void ValidateEquipmentData(Equipment equipment)
    {
        if (equipment == null)
        {
            throw new ArgumentNullException(nameof(equipment));
        }

        if (string.IsNullOrWhiteSpace(equipment.Name))
        {
            throw new ArgumentException("Equipment name cannot be empty.", nameof(equipment));
        }

        if (equipment.Name.Length > 100)
        {
            throw new ArgumentException($"Equipment name cannot exceed 100 characters.", nameof(equipment));
        }

        if (!string.IsNullOrEmpty(equipment.Type) && !ValidTypes.Contains(equipment.Type))
        {
            throw new ArgumentException($"Type must be one of: {string.Join(", ", ValidTypes)}.", nameof(equipment));
        }

        if (!string.IsNullOrEmpty(equipment.Rarity) && !ValidRarities.Contains(equipment.Rarity))
        {
            throw new ArgumentException($"Rarity must be one of: {string.Join(", ", ValidRarities)}.", nameof(equipment));
        }

        ValidateBonus(equipment.AttackBonus, nameof(equipment.AttackBonus), "Attack bonus");
        ValidateBonus(equipment.DefenseBonus, nameof(equipment.DefenseBonus), "Defense bonus");
    }

    private static void ValidateBonus(int bonus, string paramName, string bonusName)
    {
        if (bonus < 0)
        {
            throw new ArgumentException($"{bonusName} cannot be negative.", paramName);
        }
    }
}