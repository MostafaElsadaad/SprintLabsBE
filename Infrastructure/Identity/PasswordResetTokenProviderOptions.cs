using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public PasswordResetTokenProviderOptions()
    {
        Name = "SprintLabsPasswordReset";
        TokenLifespan = TimeSpan.FromHours(1);
    }
}
