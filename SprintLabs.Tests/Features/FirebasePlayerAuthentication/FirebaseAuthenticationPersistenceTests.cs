using FluentAssertions;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Compass.Tests.Features.FirebasePlayerAuthentication;

public class FirebaseAuthenticationPersistenceTests
{
    [Fact]
    public void FirebaseIdentityMappings_KeepFirebaseUidUniqueAndGoogleIdNullable()
    {
        using var context = CreateContext();
        var user = context.Model.FindEntityType(typeof(User))!;
        var player = context.Model.FindEntityType(typeof(Domain.Models.Player))!;

        user.FindProperty(nameof(User.FirebaseUid))!.IsNullable.Should().BeTrue();
        user.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Single().Name == nameof(User.FirebaseUid));
        player.FindProperty(nameof(Domain.Models.Player.GoogleId))!.IsNullable.Should().BeTrue();
        player.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Single().Name == nameof(Domain.Models.Player.UserId));
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
