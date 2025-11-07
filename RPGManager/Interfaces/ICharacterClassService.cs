using RPGManager.Models;

namespace RPGManager.Interfaces;

public interface ICharacterClassService
{
    Task<IEnumerable<CharacterClass>> GetAllClassesAsync();
    Task<CharacterClass> GetClassByIdAsync(int id);
    Task<CharacterClass> GetClassByIdWithCharactersAsync(int id);
    Task<CharacterClass> CreateClassAsync(CharacterClass newClass);
    Task UpdateClassAsync(CharacterClass updatedClass);
    Task<bool> DeleteClassAsync(int id, int defaultClassId);
}