using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Design;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Filters;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public partial class Initializer(IServiceProvider serviceProvider, ILogger<Initializer> logger) : IHostedService
{
    private CancellationToken _cancellationToken;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async void Initialize()
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            logger.LogInformation(LogEvents.InitializerInfo, "Generating filter descriptors");
            GenerateSearchOptionsDescriptors(scope.ServiceProvider.GetRequiredService<IPluralizer>());
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            logger.LogInformation(LogEvents.InitializerInfo, "Initializing leaderboards");
            var leaderboardService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();
            await leaderboardService.InitializeAsync(context, _cancellationToken);
            logger.LogInformation(LogEvents.InitializerInfo, "Initializing scripts");
            var scriptService = scope.ServiceProvider.GetRequiredService<IScriptService>();
            await scriptService.InitializeAsync(context, _cancellationToken);
            logger.LogInformation(LogEvents.InitializerInfo, "Initialization completed");
        }
        catch (Exception e)
        {
            logger.LogError(LogEvents.InitializerFailure, e, "An error has occurred during initialization");
        }
    }

    private static void GenerateSearchOptionsDescriptors(IPluralizer pluralizer)
    {
        var filters = GetDerivedTypesFromGenericBase(typeof(FilterDto<>));
        foreach (var filter in filters)
        {
            if (filter == typeof(FilterDto<>) || filter == typeof(PublicResourceFilterDto<>)) continue;
            var filterType = GetGenericTypeArgumentForBase(filter, typeof(FilterDto<>))!;
            var filterDescriptor = new List<SearchOptionsFilterEntry>();
            foreach (var property in filter.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (((List<string>) ["PreviewStart", "PreviewEnd"]).Any(e => property.Name.Contains(e))) continue;
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var label = GetLabel(filterType, property);
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var arg = type.GetGenericArguments()[0];
                    if (!arg.IsEnum) continue;
                    var values = Enum.GetValues(arg).OfType<object>().ToList();
                    if (arg == typeof(Accessibility) &&
                        (filter == typeof(ChartFilterDto) || filter == typeof(ChartSubmissionFilterDto)))
                        values = values.Cast<Accessibility>()
                            .Where(e => e != Accessibility.RequireReview)
                            .Select(e => (object)e)
                            .ToList();
                    var useSelect = values.Count > 4;

                    filterDescriptor.Add(new SearchOptionsFilterEntry
                    {
                        Type = useSelect ? "select" : "radio",
                        Label = label,
                        Value = useSelect ? new List<object>() : "",
                        Param = property.Name,
                        Items = useSelect
                            ? values.Select((e, i) => new
                            {
                                Id = i, Label = $"{pluralizer.Pluralize(label)}.{(int)e}", Value = (int)e
                            })
                            : values.Select(e =>
                                new { Label = $"{pluralizer.Pluralize(label)}.{(int)e}", Value = (int)e }),
                        Options = useSelect ? new EntryOptions { Multiple = true } : null
                    });
                }
                else if (type == typeof(string))
                {
                    if (property.Name.StartsWith("Equals")) continue;
                    filterDescriptor.Add(new SearchOptionsFilterEntry
                    {
                        Type = "input",
                        Label = label,
                        Value = "",
                        Param = property.Name,
                        Options = new EntryOptions { InputType = "text", Placeholder = label }
                    });
                }
                else if (property.Name.StartsWith("Is"))
                {
                    filterDescriptor.Add(new SearchOptionsFilterEntry
                    {
                        Type = "toggle", Label = label, Value = false, Param = property.Name
                    });
                }
                else
                {
                    if (property.Name.StartsWith("Max") || property.Name.StartsWith("Latest") ||
                        property.Name.EndsWith("Id"))
                        continue;
                    var isGroup = property.Name.StartsWith("Min") || property.Name.StartsWith("Earliest");
                    var isRating = property.Name.Contains("Rating");
                    var isDifficulty = property.Name.Contains("Difficulty");
                    var isAccuracy = property.Name.Contains("Accuracy");
                    var isRange = isRating || isDifficulty || isAccuracy;
                    filterDescriptor.Add(isRange
                        ?
                        new SearchOptionsFilterEntry
                        {
                            Type = "slider",
                            Label = label,
                            Value =
                                isRating ? (double[]) [3, 5] :
                                isDifficulty ? (double[]) [12, 16] : (double[]) [0.6, 1],
                            Param =
                                (List<string>)
                                [
                                    property.Name,
                                    EarliestRegex().Replace(MinRegex().Replace(property.Name, "Max"), "Latest")
                                ],
                            Options =
                                new EntryOptions
                                {
                                    IsRange = true,
                                    Range =
                                        isRating ? (double[]) [0, 5] :
                                        isDifficulty ? (double[]) [0, 18] : (double[]) [0, 1],
                                    Step = isRating ? 0.01 : isDifficulty ? 0.1 : 0.01,
                                    PipStep = isRating ? 100 : 10
                                }
                        }
                        : isGroup
                            ? new SearchOptionsFilterEntry
                            {
                                Type = "input_group",
                                Label = label,
                                Items = new List<EntryItem>
                                {
                                    new()
                                    {
                                        Value = "",
                                        Param = property.Name,
                                        Options =
                                            new EntryOptions
                                            {
                                                InputType =
                                                    type == typeof(TimeSpan) ||
                                                    type == typeof(DateTimeOffset)
                                                        ? "text"
                                                        : "number",
                                                Placeholder =
                                                    type == typeof(TimeSpan)
                                                        ? GetLabel(filterType, property, true)
                                                        : MinLabelRegex()
                                                            .Replace(
                                                                GetLabel(filterType, property, true),
                                                                "lowest_")
                                            }
                                    },
                                    new()
                                    {
                                        Value = "",
                                        Param =
                                            EarliestRegex()
                                                .Replace(MinRegex().Replace(property.Name, "Max"),
                                                    "Latest"),
                                        Options = new EntryOptions
                                        {
                                            InputType =
                                                type == typeof(TimeSpan) ||
                                                type == typeof(DateTimeOffset)
                                                    ? "text"
                                                    : "number",
                                            Placeholder =
                                                type == typeof(TimeSpan)
                                                    ? MinLabelRegex()
                                                        .Replace(GetLabel(filterType, property, true),
                                                            "max_")
                                                    : EarliestLabelRegex()
                                                        .Replace(
                                                            MinLabelRegex()
                                                                .Replace(
                                                                    GetLabel(filterType, property,
                                                                        true), "highest_"), "latest_")
                                        }
                                    }
                                }
                            }
                            : new SearchOptionsFilterEntry
                            {
                                Type = "input",
                                Label = label,
                                Value = "",
                                Param = property.Name,
                                Options = new EntryOptions
                                {
                                    InputType = type == typeof(TimeSpan) ? "text" : "number"
                                }
                            });
                }
            }

            var orderDescriptor = filterType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(e => e.PropertyType.IsPrimitive() && !e.PropertyType.IsSubclassOf(typeof(Delegate)) &&
                            !(e.GetMethod != null && e.GetMethod.IsStatic) && !e.Name.StartsWith("File") &&
                            !e.Name.EndsWith("Id"))
                .Select(property =>
                    new SearchOptionsOrderEntry { Label = GetLabel(filterType, property, true), Field = property.Name })
                .ToList();

            filterDescriptor.Sort((a, b) =>
            {
                var typeComparison = GetTypePriority(a.Type) - GetTypePriority(b.Type);
                return typeComparison != 0 ? typeComparison : string.CompareOrdinal(a.Label, b.Label);
            });

            Save(filterDescriptor, $"Resources/Descriptors/Filters/{filter.Name}.json");
            Save(orderDescriptor, $"Resources/Descriptors/Orders/{filterType.Name}.json");
        }
    }

    private static void Save(object? obj, string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        File.WriteAllText(path,
            JsonConvert.SerializeObject(obj, Formatting.Indented,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                }));
    }

    private static int GetTypePriority(string type)
    {
        return type switch
        {
            "toggle" => 1,
            "radio" => 2,
            "slider" => 3,
            _ => 4
        };
    }

    private static Type[] GetDerivedTypesFromGenericBase(Type genericBaseType)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        return assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => IsDerivedFromGenericBase(type, genericBaseType) && !type.IsAbstract)
            .ToArray();
    }

    private static Type? GetGenericTypeArgumentForBase(Type? type, Type genericBaseType)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericBaseType)
                return type.GetGenericArguments()[0];

            type = type.BaseType;
        }

        return null;
    }

    private static bool IsDerivedFromGenericBase(Type? type, Type genericBaseType)
    {
        while (type != null && type != typeof(object))
        {
            var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (cur == genericBaseType) return true;
            type = type.BaseType;
        }

        return false;
    }

    private static string GetLabel(Type filterType, PropertyInfo property, bool preserveAction = false)
    {
        var prefix = PascalToSnake(filterType.Name);
        var name = preserveAction || property.Name.StartsWith("Is")
            ? PascalToSnake(property.Name)
            : string.Join('_', PascalToSnake(property.Name).Split('_')[1..]);
        if (((List<string>)
            [
                "date_created", "date_updated", "accessibility", "illustrator", "owner_id", "description", "name",
                "author_name", "is_hidden", "is_locked", "is_unveiled", "like_count", "play_count"
            ]).Any(e => name.Contains(e)))
            prefix = "common";

        return
            $"{prefix.Replace("event_", "event.").Replace("hostship", "event.hostship")}.{name.Replace("rating_on", "r")}";
    }

    private static string PascalToSnake(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var stringBuilder = new StringBuilder();

        for (var i = 0; i < input.Length; i++)
            if (char.IsUpper(input[i]))
            {
                if (i > 0) stringBuilder.Append('_');

                stringBuilder.Append(char.ToLower(input[i]));
            }
            else
            {
                stringBuilder.Append(input[i]);
            }

        return stringBuilder.ToString();
    }

    [GeneratedRegex("^Min")]
    private static partial Regex MinRegex();

    [GeneratedRegex("^Earliest")]
    private static partial Regex EarliestRegex();

    [GeneratedRegex(@"(?<=\.)min_")]
    private static partial Regex MinLabelRegex();

    [GeneratedRegex(@"(?<=\.)earliest_")]
    private static partial Regex EarliestLabelRegex();
}