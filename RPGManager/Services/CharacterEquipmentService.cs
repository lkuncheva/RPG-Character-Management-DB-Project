using Newtonsoft.Json;
using RPGManager.Interfaces;
using RPGManager.Models;

namespace RPGManager.Services;
public class CharacterEquipmentService : ICharacterEquipmentService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IRepository<CharacterEquipment> _characterEquipmentRepository;
    private readonly IRepository<Equipment> _equipmentRepository;

    public CharacterEquipmentService(
        ICharacterRepository characterRepository,
        IRepository<CharacterEquipment> characterEquipmentRepository,
        IRepository<Equipment> equipmentRepository)
    {
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
        _characterEquipmentRepository = characterEquipmentRepository ?? throw new ArgumentNullException(nameof(characterEquipmentRepository));
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
    }

    public async Task<IEnumerable<CharacterEquipment>> GetCharacterEquipmentAsync(int characterId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        return await _characterEquipmentRepository.FindAsync(ce => ce.CharacterId == characterId);
    }

    public async Task<CharacterEquipment> AssignEquipmentToCharacterAsync(int characterId, int equipmentId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
        if (equipment == null)
        {
            throw new ArgumentException($"Equipment with ID {equipmentId} not found.");
        }

        var existingAssignment = await _characterEquipmentRepository.FindAsync(ce => ce.CharacterId == characterId && ce.EquipmentId == equipmentId);
        if (existingAssignment.Any())
        {
            throw new InvalidOperationException($"Equipment with ID {equipmentId} is already assigned to character with ID {characterId}.");
        }

        var characterEquipment = new CharacterEquipment
        {
            CharacterId = characterId,
            EquipmentId = equipmentId,
            IsEquipped = false
        };

        await _characterEquipmentRepository.AddRangeAsync([characterEquipment]);
        return characterEquipment;
    }

    public async Task<bool> ToggleEquipmentStatusAsync(int characterId, int equipmentId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
        if (equipment == null)
        {
            throw new InvalidOperationException($"Equipment with ID {equipmentId} not found.");
        }

        var characterEquipment = await _characterEquipmentRepository.FindAsync(ce => ce.CharacterId == characterId && ce.EquipmentId == equipmentId);
        var equipmentToUpdate = characterEquipment.FirstOrDefault();

        if (equipmentToUpdate == null)
        {
            return false;
        }

        equipmentToUpdate.IsEquipped = !equipmentToUpdate.IsEquipped;
        await _characterEquipmentRepository.UpdateAsync(equipmentToUpdate);
        return true;
    }

    public async Task<bool> RemoveEquipmentFromCharacterAsync(int characterId, int equipmentId)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {characterId} not found.");
        }

        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
        if (equipment == null)
        {
            throw new InvalidOperationException($"Equipment with ID {equipmentId} not found.");
        }

        var characterEquipment = await _characterEquipmentRepository.FindAsync(ce => ce.CharacterId == characterId && ce.EquipmentId == equipmentId);
        var equipmentToDelete = characterEquipment.FirstOrDefault();

        if (equipmentToDelete == null)
        {
            return false;
        }

        await _characterEquipmentRepository.DeleteAsync(equipmentToDelete);
        return true;
    }

    public async Task BulkInsertCharacterEquipmentFromJsonAsync(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException("File path cannot be empty or whitespace.", nameof(jsonFilePath));
        }

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"File not found: {jsonFilePath}");
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var characterEquipment = JsonConvert.DeserializeObject<List<CharacterEquipment>>(jsonContent);

        if (characterEquipment == null || !characterEquipment.Any())
        {
            throw new InvalidOperationException("No character equipment found in JSON file.");
        }

        foreach (var charEquipment in characterEquipment)
        {
            var character = await _characterRepository.GetByIdAsync(charEquipment.CharacterId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character with ID {charEquipment.CharacterId} not found.");
            }

            var equipment = await _equipmentRepository.GetByIdAsync(charEquipment.EquipmentId);
            if (equipment == null)
            {
                throw new ArgumentException($"Equipment with ID {charEquipment} not found.");
            }

            var existingAssignment = await _characterEquipmentRepository.FindAsync(ce => ce.CharacterId == charEquipment.CharacterId && ce.EquipmentId == charEquipment.EquipmentId);
            if (existingAssignment.Any())
            {
                throw new InvalidOperationException($"Equipment with ID {charEquipment.EquipmentId} is already assigned to character with ID {charEquipment.CharacterId}.");
            }
        }

        await _characterEquipmentRepository.AddRangeAsync(characterEquipment);
        Console.WriteLine($"Successfully inserted {characterEquipment.Count} character equipment from {jsonFilePath}");
    }
}