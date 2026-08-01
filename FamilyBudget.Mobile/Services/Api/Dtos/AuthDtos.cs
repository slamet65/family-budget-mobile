namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, UserDto User);

public record UserDto(int Id, string Name, string Email);

public record CreateUserRequest(string Name, string Email, string Password);

// Any authenticated user can reset any user's password (including their own), no old-password
// confirmation -- matches the equal-access model (no admin/viewer distinction). See
// workers/api/REQUIREMENTS.md, 2026-08-01 entry.
public record ResetPasswordRequest(string NewPassword);
