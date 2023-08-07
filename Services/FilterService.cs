using System.Linq.Expressions;
using System.Reflection;
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
    private readonly IResourceService _resourceService;

    public FilterService(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<Expression<Func<T, bool>>?> Parse<T>(FilterDto<T>? dto, string? predicate = null,
        User? currentUser = null, Expression<Func<T, bool>>? requirement = null)
    {
        var isAdmin = currentUser != null && await _resourceService.HasPermission(currentUser, Roles.Administrator);

        if (isAdmin && predicate != null)
            return await CSharpScript.EvaluateAsync<Expression<Func<T, bool>>>(predicate,
                ScriptOptions.Default.AddReferences(typeof(T).Assembly));

        if (dto == null) return null;

        var entity = Expression.Parameter(typeof(T), "e");

        Expression expression =
            Expression.OrElse(Expression.Constant(isAdmin || typeof(T).GetProperty("IsHidden") == null),
                IsFalse(Property<T>(entity, "IsHidden")));

        if (requirement != null) expression = Expression.AndAlso(expression, Expression.Invoke(requirement, entity));

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
                    Call(condition, type, Method(type, "Contains"), Property<T>(entity, property, Actions.Range)));

            if (name.StartsWith(Actions.Is))
                expression = Expression.AndAlso(expression, Expression.Equal(Property<T>(entity, property), condition));

            if (name.StartsWith(Actions.Has))
                expression = Expression.AndAlso(expression,
                    Expression.Equal(Property<T>(entity, property, Actions.Has), condition));

            if (name.StartsWith(Actions.Equals))
                expression = Expression.AndAlso(expression,
                    Call(ToUpper(Property<T>(entity, property, Actions.Equals)), type, Method(type, "Equals", 1),
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

    private static Expression Call(Expression? instance, Type type, MethodInfo method, Expression? argument)
    {
        if (argument == null) return Expression.Call(instance, method);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            type = type.GetGenericArguments()[0];
        var underlyingType = Nullable.GetUnderlyingType(argument.Type);

        return Expression.Call(instance, method,
            !type.IsEnum
                ? underlyingType == null ? argument : Expression.Convert(argument, underlyingType)
                : Expression.Convert(argument, type));
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

    private static Expression Property<T>(Expression entity, MemberInfo property, string? action = null)
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
        return Expression.Call(null, typeof(DateTimeOffset).GetMethod("Compare")!, GetValue(property),
            GetValue(condition));
    }

    private static Expression GetValue(Expression argument)
    {
        var underlyingType = Nullable.GetUnderlyingType(argument.Type);
        return underlyingType == null ? argument : Expression.Convert(argument, underlyingType);
    }

    private static MethodInfo Method(Type type, string name, int i = 0)
    {
        return type.GetMethods().Where(m => m.Name == name).ToArray()[i];
    }
}