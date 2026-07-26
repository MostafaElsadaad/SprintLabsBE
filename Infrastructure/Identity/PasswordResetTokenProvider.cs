using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

public class PasswordResetTokenProvider : DataProtectorTokenProvider<User>
{
    public PasswordResetTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<PasswordResetTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<User>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
