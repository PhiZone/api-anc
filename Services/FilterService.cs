using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using Actions = PhiZoneApi.Constants.FilterActions;

namespace PhiZoneApi.Services;

public class FilterService : IFilterService
{
    private readonly UserManager<User> _userManager;

    public FilterService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Expression<Func<T, bool>>?> Parse<T>(FilterDto<T>? dto, string? predicate = null,
        User? currentUser = null)
    {
        if (predicate != null && currentUser != null &&
            !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return await CSharpScript.EvaluateAsync<Expression<Func<T, bool>>>(predicate,
                ScriptOptions.Default.AddReferences(typeof(T).Assembly));

        if (dto == null) return null;

        var entity = Expression.Parameter(typeof(T), "e");

        Expression expression = Expression.Constant(true);
        foreach (var property in dto.GetType().GetProperties())
        {
            var name = property.Name;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            var value = property.GetValue(dto);
            if (value == null) continue;

            var condition = Expression.Constant(value, type);

            if (name.StartsWith(Actions.Min))
                expression = Expression.AndAlso(expression,
                    Expression.GreaterThanOrEqual(Property<T>(entity, property, Actions.Min), condition));

            if (name.StartsWith(Actions.Max))
                expression = Expression.AndAlso(expression,
                    Expression.LessThanOrEqual(Property<T>(entity, property, Actions.Max), condition));

            if (name.StartsWith(Actions.Range))
                expression = Expression.AndAlso(expression,
                    Expression.Call(condition, Method(type, "Contains"), Property<T>(entity, property, Actions.Range)));

            if (name.StartsWith(Actions.Equals))
                expression = Expression.AndAlso(expression,
                    Expression.Call(ToLower(Property<T>(entity, property, Actions.Equals)), Method(type, "Equals", 1),
                        ToLower(condition)));

            if (name.StartsWith(Actions.Contains))
                expression = Expression.AndAlso(expression,
                    Expression.Call(ToLower(Property<T>(entity, property, Actions.Contains)), Method(type, "Contains"),
                        ToLower(condition)));

            if (name.StartsWith(Actions.Earliest))
                expression = Expression.AndAlso(expression,
                    Expression.GreaterThanOrEqual(
                        CompareDate(Property<T>(entity, property, Actions.Earliest), condition),
                        Expression.Constant(0, typeof(int))));

            if (name.StartsWith(Actions.Latest))
                expression = Expression.AndAlso(expression,
                    Expression.LessThanOrEqual(CompareDate(Property<T>(entity, property, Actions.Latest), condition),
                        Expression.Constant(0, typeof(int))));
        }

        return Expression.Lambda<Func<T, bool>>(expression, entity);
    }

    private static MemberExpression Property<T>(Expression entity, MemberInfo property, string action)
    {
        return Expression.Property(entity, typeof(T).GetProperty(property.Name[action.Length..])!);
    }

    private static Expression ToLower(Expression expression)
    {
        return Expression.Call(expression, typeof(string).GetMethods().Where(m => m.Name == "ToLower").ToArray()[0]);
    }

    private static Expression CompareDate(Expression property, Expression condition)
    {
        return Expression.Call(null, typeof(DateTimeOffset).GetMethod("Compare")!, property,
            Expression.Call(condition, Method(typeof(DateTimeOffset?), "GetValueOrDefault")));
    }

    private static MethodInfo Method(Type type, string name, int i = 0)
    {
        return type.GetMethods().Where(m => m.Name == name).ToArray()[i];
    }
}