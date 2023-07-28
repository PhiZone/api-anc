using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class AdmissionDto<TAdmitter, TAdmittee>
{
    public TAdmitter Admitter { get; set; } = default!;

    public TAdmittee Admittee { get; set; } = default!;

    public RequestStatus Status { get; set; }

    public string? Label { get; set; }

    public int RequesterId { get; set; }

    public int RequesteeId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}