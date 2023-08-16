using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace PhiZoneApi.Utils;

public static class LinqUtil
{
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, string field, bool desc)
    {
        if (string.IsNullOrWhiteSpace(field)) return query;
        var propInfo = GetPropertyInfo(typeof(T), field);
        var expr = GetOrderExpression(typeof(T), propInfo);
        if (desc)
        {
            var method = typeof(Queryable).GetMethods()
                .FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
            var genericMethod = method!.MakeGenericMethod(typeof(T), propInfo.PropertyType);
            return (IQueryable<T>)genericMethod.Invoke(null, new object[] { query, expr })!;
        }
        else
        {
            var method = typeof(Queryable).GetMethods()
                .FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
            var genericMethod = method!.MakeGenericMethod(typeof(T), propInfo.PropertyType);
            return (IQueryable<T>)genericMethod.Invoke(null, new object[] { query, expr })!;
        }
    }

    public static bool Like(this string property, string pattern)
    {
        return EF.Functions.Like(property, $"%{pattern}%");
    }

    private static PropertyInfo GetPropertyInfo(Type objType, string name)
    {
        var properties = objType.GetProperties();
        var matchedProperty = properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (matchedProperty == null) throw new ArgumentException("No such property");

        return matchedProperty;
    }

    private static LambdaExpression GetOrderExpression(Type objType, MemberInfo pi)
    {
        var paramExpr = Expression.Parameter(objType);
        var propAccess = Expression.PropertyOrField(paramExpr, pi.Name);
        var expr = Expression.Lambda(propAccess, paramExpr);
        return expr;
    }
}