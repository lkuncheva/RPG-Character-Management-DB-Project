using RPGManager.Data.Models;
using RPGManager.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RPGManager.Data.Repositories;

public class CharacterRepository : Repository<Character>, ICharacterRepository
{
    public CharacterRepository(RPGManagerContext context) : base(context)
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