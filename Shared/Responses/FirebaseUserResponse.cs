namespace Shared.Responses;

public class FirebaseUserResponse
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PictureUrl { get; set; } = string.Empty;
    public string SignInProvider { get; set; } = string.Empty;
    public string? GoogleProviderId { get; set; }
}
