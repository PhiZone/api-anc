namespace PhiZoneApi.Configurations;

/// <summary>
///     Determines how the data is presented.
/// </summary>
public class DataSettings
{
    /// <summary>
    ///     Determines the default number of entries presented in each page.
    /// </summary>
    public int PaginationPerPage { get; set; }
    
    /// <summary>
    ///     Determines the maximum number of entries presented in each page.
    /// </summary>
    public int PaginationMaxPerPage { get; set; }

    /// <summary>
    ///     Determines the mode for pagination.
    ///     0 = Offset Pagination (https://learn.microsoft.com/en-us/ef/core/querying/pagination#offset-pagination);
    ///     not 0 = Keyset Pagination (https://learn.microsoft.com/en-us/ef/core/querying/pagination#keyset-pagination).
    /// </summary>
    /// TODO implement this (at the moment this value is constantly regarded as 0)
    public int PaginationMode { get; set; }
}