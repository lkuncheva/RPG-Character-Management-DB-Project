using RPGManager.Models;

namespace RPGManager.Interfaces;

public interface ICharacterClassRepository : IRepository<CharacterClass>
{
    Task<CharacterClass> GetByIdWithCharactersAsync(int id);
}