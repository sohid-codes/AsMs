using AsMs.Data.Identity;

namespace AsMs.Api.Authentication;

public interface ITokenService
{
    Task<TokenResponse> CreateAsync(ApplicationUser user);
}
