using RPGManager.Interfaces;
using RPGManager.Models;

namespace RPGManager.Services;

public class CharacterClassService : ICharacterClassService
{
    private static readonly HashSet<string> ValidPrimaryAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(CharacterStats.Strength),
        nameof(CharacterStats.Dexterity),
        nameof(CharacterStats.Intelligence),
        nameof(CharacterStats.Constitution),
        nameof(CharacterStats.Wisdom),
        nameof(CharacterStats.Charisma)
    };

    private readonly ICharacterClassRepository _characterClassRepository;

    public CharacterClassService(ICharacterClassRepository characterClassRepository)
    {
        _characterClassRepository = characterClassRepository ?? throw new ArgumentNullException(nameof(characterClassRepository));
    }

    public Task<IEnumerable<CharacterClass>> GetAllClassesAsync()
    {
        return _characterClassRepository.GetAllAsync();
    }

    public Task<CharacterClass> GetClassByIdAsync(int id)
    {
        return _characterClassRepository.GetByIdAsync(id);
    }

    public async Task<CharacterClass> GetClassByIdWithCharactersAsync(int id)
    {
        return await _characterClassRepository.GetByIdWithCharactersAsync(id);
    }

    public async Task<CharacterClass> CreateClassAsync(CharacterClass newClass)
    {
        ValidateClassProperties(newClass);
        EnsurePrimaryAttributeIsValid(newClass.PrimaryAttribute);

        await _characterClassRepository.AddRangeAsync([newClass]);
        return newClass;
    }

    public async Task UpdateClassAsync(CharacterClass updatedClass)
    {
        if (updatedClass == null)
        {
            throw new ArgumentNullException(nameof(updatedClass), "Updated CharacterClass data cannot be null.");
        }

        ValidateClassProperties(updatedClass);
        EnsurePrimaryAttributeIsValid(updatedClass.PrimaryAttribute);

        if (!ValidPrimaryAttributes.Contains(updatedClass.PrimaryAttribute))
        {
            var validAttributesList = string.Join(", ", ValidPrimaryAttributes);
            throw new ArgumentException(
                $"Invalid Primary Attribute '{updatedClass.PrimaryAttribute}'. Must be one of: {validAttributesList}.",
                nameof(updatedClass.PrimaryAttribute));
        }

        var existingClass = await _characterClassRepository.GetByIdAsync(updatedClass.Id);
        if (existingClass == null)
        {
            throw new InvalidOperationException($"Character class with ID {updatedClass.Id} not found for update.");
        }

        await _characterClassRepository.UpdateAsync(updatedClass);
    }

    public async Task<bool> DeleteClassAsync(int classToDeleteId, int newClassId)
    {
        var classToDelete = await _characterClassRepository.GetByIdWithCharactersAsync(classToDeleteId);

        if (classToDelete == null)
        {
            return false;
        }

        var dependentCharacters = classToDelete.Characters.ToList();

        if (newClassId > 0 && dependentCharacters.Any())
        {
            var targetClassList = await _characterClassRepository.FindAsync(c => c.Id == newClassId);
            var targetClass = targetClassList.FirstOrDefault();

            if (targetClass == null)
            {
                throw new InvalidOperationException($"The target class ID ({newClassId}) for reassignment does not exist.");
            }
        }

        foreach (var character in dependentCharacters)
        {
            character.CharacterClassId = newClassId;
        }

        if (classToDelete != null)
        {
            await _characterClassRepository.DeleteAsync(classToDelete);
        }
        else
        {
            return false;
        }

        return true;
    }

    private static void ValidateClassProperties(CharacterClass classToValidate)
    {
        if (classToValidate == null)
        {
            throw new ArgumentNullException(nameof(classToValidate), "CharacterClass data cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(classToValidate.Name))
        {
            throw new ArgumentException("Character class name is required.", nameof(classToValidate.Name));
        }

        if (classToValidate.Name.Length > 50)
        {
            throw new ArgumentException($"Class name cannot exceed 50 characters.", nameof(classToValidate.Name));
        }

        if (classToValidate.Description.Length > 500)
        {
            throw new ArgumentException($"Class description cannot exceed 500 characters.", nameof(classToValidate.Description));
        }

        if (classToValidate.BaseMana < 0)
        {
            throw new ArgumentException("BaseMana cannot be negative.", nameof(classToValidate.BaseMana));
        }

        if (classToValidate.BaseHealth < 0)
        {
            throw new ArgumentException("BaseHealth cannot be negative.", nameof(classToValidate.BaseHealth));
        }
    }

    private static void EnsurePrimaryAttributeIsValid(string attributeName)
    {
        if (!string.IsNullOrEmpty(attributeName) && !ValidPrimaryAttributes.Contains(attributeName))
        {
            var validAttributesList = string.Join(", ", ValidPrimaryAttributes);
            throw new ArgumentException(
                $"Invalid Primary Attribute '{attributeName}'. Must be one of: {validAttributesList}.",
                nameof(attributeName));
        }
    }
}
