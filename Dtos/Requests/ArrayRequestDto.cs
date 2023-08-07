using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class ArrayRequestDto
{
    /// <summary>
    ///     The field by which the result is sorted. Defaults to <c>Id</c>.
    /// </summary>
    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Order { get; set; } = "Id";

    /// <summary>
    ///     Whether or not the result is sorted in descending order. Defaults to <c>false</c>.
    /// </summary>
    public bool Desc { get; set; } = false;

    /// <summary>
    ///     The page number. Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    ///     How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.
    /// </summary>
    public int PerPage { get; set; }

    /// <summary>
    ///     A string that filters the query result in multiple fields. Optional.
    /// </summary>
    [MaxLength(30000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Search { get; set; } = null;

    /// <summary>
    ///     A string that will be evaluated into a <c>Func</c>, which filters the query result.
    ///     Optional. Administrators only.
    /// </summary>
    [MaxLength(50000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Predicate { get; set; } = null;
}