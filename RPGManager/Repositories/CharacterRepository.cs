using RPGManager.Data;
using RPGManager.Models;
using RPGManager.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RPGManager.Repositories;

public class CharacterRepository : Repository<Character>, ICharacterRepository
{
    public CharacterRepository(RpgDbContext context) : base(context)
    {
    }

    public async Task<Character> GetCharacterWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(c => c.CharacterClass)
            .Include(c => c.CharacterStats)
            .Include(c => c.CharacterEquipment)
                .ThenInclude(ce => ce.Equipment)
            .Include(c => c.CharacterQuests)
                .ThenInclude(cq => cq.Quest)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}