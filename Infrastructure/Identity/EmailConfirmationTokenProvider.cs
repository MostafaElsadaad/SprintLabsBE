using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

public class EmailConfirmationTokenProvider : DataProtectorTokenProvider<User>
{
    public EmailConfirmationTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<EmailConfirmationTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<User>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
