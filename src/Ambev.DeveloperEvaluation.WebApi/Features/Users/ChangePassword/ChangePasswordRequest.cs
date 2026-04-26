namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.ChangePassword;

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
