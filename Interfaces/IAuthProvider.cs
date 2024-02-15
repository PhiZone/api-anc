using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IAuthProvider
{
    Task InitializeAsync();

    Task<ServiceResponseDto> RequestIdentityAsync(string state, string redirectUri, User? user = null);

    Task<(string, string?)?> RequestTokenAsync(string code, string state, User? user = null);

    Task<User?> GetIdentityAsync(string accessToken);

    Task<bool> BindIdentityAsync(User user);

    Task UpdateTokenAsync(User user, string accessToken, string? refreshToken = null);

    Task RevokeTokenAsync(User user);

    Guid GetApplicationId();
}