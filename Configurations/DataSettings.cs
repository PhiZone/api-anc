using PhiZoneApi.Enums;

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
    ///     Determines the pagination mode.
    ///     Options:
    ///     Offset Pagination (https://learn.microsoft.com/en-us/ef/core/querying/pagination#offset-pagination);
    ///     Keyset Pagination (https://learn.microsoft.com/en-us/ef/core/querying/pagination#keyset-pagination).
    /// </summary>
    /// TODO implement this
    public PaginationMode PaginationMode { get; set; }
}