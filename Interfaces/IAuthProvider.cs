using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IAuthProvider
{
    Task InitializeAsync();

    Task<ServiceResponseDto> RequestIdentityAsync(string state, string redirectUri, User? user = null);

    Task<(string, string?)?>
        RequestTokenAsync(string code, string state, User? user = null, string? redirectUri = null);

    Task<User?> GetIdentityAsync(string accessToken);

    Task<RemoteUserDto?> GetRemoteIdentityAsync(string accessToken);

    Task<bool> BindIdentityAsync(User user);

    Task UpdateTokenAsync(User user, string accessToken, string? refreshToken = null);

    Task<bool> RefreshTokenAsync(User user);

    Task RevokeTokenAsync(User user);

    Guid GetApplicationId();
}