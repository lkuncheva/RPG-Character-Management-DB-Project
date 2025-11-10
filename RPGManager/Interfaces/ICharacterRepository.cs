using RPGManager.Models;

namespace RPGManager.Interfaces;

public interface ICharacterRepository : IRepository<Character>
{
    Task<Character> GetCharacterWithDetailsAsync(int id);
}