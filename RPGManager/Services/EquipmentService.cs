using Newtonsoft.Json;
using RPGManager.Models;
using RPGManager.Interfaces;

namespace RPGManager.Services;

public class EquipmentService : IEquipmentService
{
    private readonly IRepository<Equipment> _equipmentRepository;

    public EquipmentService(IRepository<Equipment> equipmentRepository)
    {
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
    }

    public async Task<Equipment> CreateEquipmentAsync(Equipment equipment)
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

        if (equipment.Type.Length > 50)
        {
            throw new ArgumentException($"Equipment type cannot exceed 50 characters.", nameof(equipment));
        }

        if (equipment.Rarity.Length > 50)
        {
            throw new ArgumentException($"Equipment rarity cannot exceed 50 characters.", nameof(equipment));
        }

        if (equipment.AttackBonus < 0)
        {
            throw new ArgumentException("Attack bonus cannot be negative.", nameof(equipment));
        }

        if (equipment.DefenseBonus < 0)
        {
            throw new ArgumentException("Defense bonus cannot be negative.", nameof(equipment));
        }

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
        var equipment = JsonConvert.DeserializeObject<List<Equipment>>(jsonContent);

        if (equipment == null || !equipment.Any())
        {
            throw new InvalidOperationException("No equipment found in JSON file.");
        }

        await _equipmentRepository.AddRangeAsync(equipment);
        Console.WriteLine($"Successfully inserted {equipment.Count} equipment items from {jsonFilePath}");
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
        return await _equipmentRepository.FindAsync(q => q.Rarity == rarity);
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

        if (newAttackBonus < 0)
        {
            throw new ArgumentException("Attack bonus cannot be negative.", nameof(newAttackBonus));
        }

        if (newDefenceBonus < 0)
        {
            throw new ArgumentException("Defense bonus cannot be negative.", nameof(newDefenceBonus));
        }

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
}