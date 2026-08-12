namespace AsMs.Api.Authentication;

public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string[] Roles);
