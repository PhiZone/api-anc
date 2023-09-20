using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IPetAnswerRepository
{
    Task<ICollection<PetAnswer>> GetPetAnswersAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null, Expression<Func<PetAnswer, bool>>? predicate = null);

    Task<PetAnswer> GetPetAnswerAsync(Guid id);

    Task<bool> PetAnswerExistsAsync(Guid id);

    Task<bool> CreatePetAnswerAsync(PetAnswer petAnswer);

    Task<bool> UpdatePetAnswerAsync(PetAnswer petAnswer);

    Task<bool> RemovePetAnswerAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountPetAnswersAsync(string? search = null, Expression<Func<PetAnswer, bool>>? predicate = null);
}