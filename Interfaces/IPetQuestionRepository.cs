using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IPetQuestionRepository
{
    Task<ICollection<PetQuestion>> GetPetQuestionsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<PetQuestion, bool>>? predicate = null);

    Task<PetQuestion> GetPetQuestionAsync(Guid id);

    Task<PetQuestion?> GetRandomPetQuestionAsync(int position, string language);

    Task<bool> PetQuestionExistsAsync(Guid id);

    Task<bool> CreatePetQuestionAsync(PetQuestion petQuestion);

    Task<bool> UpdatePetQuestionAsync(PetQuestion petQuestion);

    Task<bool> RemovePetQuestionAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountPetQuestionsAsync(Expression<Func<PetQuestion, bool>>? predicate = null);

    Task<ICollection<PetChoice>> GetQuestionChoicesAsync(Guid questionId);
}