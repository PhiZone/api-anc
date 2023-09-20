using System.Linq.Expressions;
using System.Reflection;

namespace PhiZoneApi.Utils;

public static class LinqUtil
{
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, List<string> fields, List<bool> desc)
    {
        if (fields.Count == 0) fields = new List<string> { "DateCreated" };

        var sourceType = typeof(T);
        var parameter = Expression.Parameter(sourceType, "x");
        var orderedQuery = query;

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var isDescending = desc.Count > i && desc[i];

            if (string.IsNullOrWhiteSpace(field)) continue;

            var propInfo = GetPropertyInfo(sourceType, field);
            var propertyExpr = Expression.Property(parameter, propInfo);
            var lambdaExpr = Expression.Lambda(propertyExpr, parameter);

            var orderByMethod = typeof(Queryable).GetMethods()
                .FirstOrDefault(m =>
                    m.Name == $"{(i == 0 ? "Order" : "Then")}{(isDescending ? "ByDescending" : "By")}" &&
                    m.GetParameters().Length == 2)!.MakeGenericMethod(typeof(T), propInfo.PropertyType);

            orderedQuery = (IQueryable<T>)orderByMethod.Invoke(null, new object[] { orderedQuery, lambdaExpr })!;
        }

        return orderedQuery;
    }

    private static PropertyInfo GetPropertyInfo(Type objType, string name)
    {
        var properties = objType.GetProperties();
        var matchedProperty = properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (matchedProperty == null)
        {
            matchedProperty = properties.FirstOrDefault(p => p.Name == "Id");
        }
        return matchedProperty != null ? matchedProperty : properties[0];
    }
}