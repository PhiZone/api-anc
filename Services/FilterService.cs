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
        var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, Roles.Administrator);

        if (isAdmin && predicate != null)
            return await CSharpScript.EvaluateAsync<Expression<Func<T, bool>>>(predicate,
                ScriptOptions.Default.AddReferences(typeof(T).Assembly));

        if (dto == null) return null;

        var entity = Expression.Parameter(typeof(T), "e");

        Expression expression =
            Expression.OrElse(Expression.Constant(isAdmin || typeof(T).GetProperty("IsHidden") == null),
                IsFalse(Property<T>(entity, "IsHidden")));
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

            if (name.StartsWith(Actions.Is))
                expression = Expression.AndAlso(expression, Expression.Equal(Property<T>(entity, property), condition));

            if (name.StartsWith(Actions.Equals))
                expression = Expression.AndAlso(expression,
                    Expression.Call(ToUpper(Property<T>(entity, property, Actions.Equals)), Method(type, "Equals", 1),
                        ToUpper(condition)));

            if (name.StartsWith(Actions.Contains))
                expression = Expression.AndAlso(expression,
                    Expression.Call(ToUpper(Property<T>(entity, property, Actions.Contains)), Method(type, "Contains"),
                        ToUpper(condition)));

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

    private static Expression IsFalse(Expression? expression)
    {
        var falseExpr = Expression.Constant(false);
        if (expression == null) return falseExpr;
        return Expression.Equal(expression, falseExpr);
    }

    private static MemberExpression? Property<T>(Expression entity, string name)
    {
        var property = typeof(T).GetProperty(name);
        return property != null ? Expression.Property(entity, property) : null;
    }

    private static MemberExpression Property<T>(Expression entity, MemberInfo property, string? action = null)
    {
        return Expression.Property(entity,
            typeof(T).GetProperty(action != null ? property.Name[action.Length..] : property.Name)!);
    }

    private static Expression ToUpper(Expression expression)
    {
        return Expression.Call(expression, typeof(string).GetMethods().Where(m => m.Name == "ToUpper").ToArray()[0]);
    }

    private static Expression CompareDate(Expression property, Expression condition)
    {
        return Expression.Call(null, typeof(DateTimeOffset).GetMethod("Compare")!, property,
            condition.Type == typeof(DateTimeOffset)
                ? condition
                : Expression.Call(condition, Method(typeof(DateTimeOffset?), "GetValueOrDefault")));
    }

    private static MethodInfo Method(Type type, string name, int i = 0)
    {
        return type.GetMethods().Where(m => m.Name == name).ToArray()[i];
    }
}