using RPGManager.Data;
using RPGManager.Models;
using RPGManager.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RPGManager.Repositories;

public class CharacterClassRepository : Repository<CharacterClass>, ICharacterClassRepository
{
    public CharacterClassRepository(RpgDbContext context) : base(context)
    {
    }

    public async Task<CharacterClass> GetByIdWithCharactersAsync(int id)
    {
        return await _context.CharacterClasses
                         .Include(c => c.Characters)
                         .FirstOrDefaultAsync(c => c.Id == id);
    }
}