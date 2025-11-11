using RPGManager.Data.Models;

namespace RPGManager.Data.Interfaces;

public interface ICharacterRepository : IRepository<Character>
{
    Task<Character> GetCharacterWithDetailsAsync(int id);
}