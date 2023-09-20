using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IPetChoiceRepository
{
    Task<ICollection<PetChoice>> GetPetChoicesAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null, Expression<Func<PetChoice, bool>>? predicate = null);

    Task<PetChoice> GetPetChoiceAsync(Guid id);

    Task<bool> PetChoiceExistsAsync(Guid id);

    Task<bool> CreatePetChoiceAsync(PetChoice petChoice);

    Task<bool> UpdatePetChoiceAsync(PetChoice petChoice);

    Task<bool> RemovePetChoiceAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountPetChoicesAsync(string? search = null, Expression<Func<PetChoice, bool>>? predicate = null);
}