using System.Reflection;
using System.Linq.Expressions;

namespace MyApp.Shared.Domain.Extensions;

/// <summary>
/// Dynamic LINQ extensions for runtime sorting and filtering.
/// </summary>
public static class DynamicLinqExtensions
{
    /// <summary>
    /// Order a queryable by a property name, optionally in descending order.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The source queryable</param>
    /// <param name="propertyName">Name of the property to sort by</param>
    /// <param name="descending">If true, sort descending; else ascending</param>
    /// <returns>An ordered queryable</returns>
    public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string propertyName, bool descending = false)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return query;

        try
        {
            // Validate that the property exists
            var property = GetProperty<T>(propertyName);
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.");

            // Build the expression: x => x.PropertyName
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, property);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            // Create the OrderBy or OrderByDescending call
            var methodName = descending ? "OrderByDescending" : "OrderBy";
            var orderByExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), property.PropertyType },
                query.Expression,
                Expression.Quote(lambda));

            return query.Provider.CreateQuery<T>(orderByExpression);
        }
        catch
        {
            // If anything fails, return the query unchanged
            return query;
        }
    }

    /// <summary>
    /// Filter a queryable by a property name and value using a simple equality predicate.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The source queryable</param>
    /// <param name="propertyName">Name of the property to filter by</param>
    /// <param name="value">Value to compare (converted to string)</param>
    /// <returns>A filtered queryable</returns>
    public static IQueryable<T> FilterByProperty<T>(this IQueryable<T> query, string propertyName, string value)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(value))
            return query;

        try
        {
            var property = GetProperty<T>(propertyName);
            if (property == null)
                return query;

            // Build the expression: x => x.PropertyName == value
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, property);

            // Convert value to the property's type
            object? convertedValue = Convert.ChangeType(value, property.PropertyType);
            var constant = Expression.Constant(convertedValue);
            var equality = Expression.Equal(propertyAccess, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

            return query.Where(lambda);
        }
        catch
        {
            return query;
        }
    }

    /// <summary>
    /// Filter by multiple properties (OR condition).
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The source queryable</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="propertyNames">Names of properties to search in (OR combined)</param>
    /// <returns>A filtered queryable</returns>
    public static IQueryable<T> FilterByMultipleProperties<T>(
        this IQueryable<T> query,
        string searchTerm,
        params string[] propertyNames)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || propertyNames.Length == 0)
            return query;

        try
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? orExpression = null;

            foreach (var propName in propertyNames)
            {
                var property = GetProperty<T>(propName);
                if (property == null || property.PropertyType != typeof(string))
                    continue;

                // Build: x.PropertyName.Contains(searchTerm)
                var propertyAccess = Expression.Property(parameter, property);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                var searchTermConstant = Expression.Constant(searchTerm);
                var containsCall = Expression.Call(propertyAccess, containsMethod!, searchTermConstant);

                // OR together all property conditions
                if (orExpression == null)
                    orExpression = containsCall;
                else
                    orExpression = Expression.OrElse(orExpression, containsCall);
            }

            if (orExpression == null)
                return query;

            var lambda = Expression.Lambda<Func<T, bool>>(orExpression, parameter);
            return query.Where(lambda);
        }
        catch
        {
            return query;
        }
    }

    /// <summary>
    /// Get a property info by name, supporting nested properties (e.g., "Address.City").
    /// </summary>
    private static PropertyInfo? GetProperty<T>(string propertyName) where T : class
    {
        var properties = propertyName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        PropertyInfo? property = null;

        foreach (var prop in properties)
        {
            property = typeof(T).GetProperty(prop, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property == null)
                return null;
        }

        return property;
    }
}
