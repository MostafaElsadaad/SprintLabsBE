using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public EmailConfirmationTokenProviderOptions()
    {
        Name = "SprintLabsEmailConfirmation";
        TokenLifespan = TimeSpan.FromHours(24);
    }
}
