using RPGManager.Data.Models;
using RPGManager.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RPGManager.Data.Repositories;

public class CharacterClassRepository : Repository<CharacterClass>, ICharacterClassRepository
{
    public CharacterClassRepository(RPGManagerContext context) : base(context)
    {
    }

    public async Task<CharacterClass> GetByIdWithCharactersAsync(int id)
    {
        return await _context.CharacterClasses
                         .Include(c => c.Characters)
                         .FirstOrDefaultAsync(c => c.Id == id);
    }
}