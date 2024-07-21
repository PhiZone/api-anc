using System.Linq.Expressions;
using PhiZoneApi.Dtos.Filters;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IFilterService
{
    Task<Expression<Func<T, bool>>?> Parse<T>(FilterDto<T>? dto, string? predicate = null, User? currentUser = null,
        Expression<Func<T, bool>>? requirement = null, bool? isAdmin = null);
}