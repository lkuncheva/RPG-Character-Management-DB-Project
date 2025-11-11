using RPGManager.Data.Models;

namespace RPGManager.Data.Interfaces;

public interface ICharacterClassRepository : IRepository<CharacterClass>
{
    Task<CharacterClass> GetByIdWithCharactersAsync(int id);
}