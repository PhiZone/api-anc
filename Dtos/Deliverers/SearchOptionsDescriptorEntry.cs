using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class SearchOptionsDescriptorEntry
{
    public string Type { get; set; } = null!;

    public string Label { get; set; } = null!;

    public object? Value { get; set; }

    public object? Param { get; set; }

    public EntryOptions? Options { get; set; }

    public object? Items { get; set; }
}

public class EntryItem
{
    public object? Value { get; set; }

    public object Param { get; set; } = null!;

    public EntryOptions? Options { get; set; }
}

public class EntryOptions
{
    public string? InputType { get; set; }

    public string? Placeholder { get; set; }

    public bool? IsRange { get; set; }

    public double[]? Range { get; set; }

    public double? Step { get; set; }

    [JsonProperty("pipstep")] public double? PipStep { get; set; }
}