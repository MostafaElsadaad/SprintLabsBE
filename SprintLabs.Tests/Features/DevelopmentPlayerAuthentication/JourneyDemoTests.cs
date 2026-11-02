using Application.Features.Accounts.DevelopmentAuthentication.SeedJourneyDemo;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using Moq;
using Shared.DevelopmentAuthentication;
using Shared.Exceptions;
using Shared.Requests.QuestionData;
using Shared.Responses;
namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;
public class JourneyDemoTests
{
    [Theory]
    [InlineData("guard")]
    [InlineData("admin")]
    [InlineData("identity")]
    public async Task Rejects_before_any_mutation(string failure)
    {
        var f = new Fixture();
        if (failure == "guard") f.Guard.Setup(g => g.EnsureEndpointAccess(It.IsAny<IReadOnlyCollection<string>>())).Throws(new InvalidOperationException());
        if (failure == "admin") f.Admin.IsPlatformAdmin = false;
        if (failure == "identity") f.Identities.Last().FirebaseUid = "not-a-development-account";
        await Assert.ThrowsAnyAsync<Exception>(() => f.Handler().Handle(new() { AdminUserId = 1 }, default));
        f.Questions.Verify(q => q.PublishAsync(It.IsAny<long>(), It.IsAny<QuestionDataRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        f.Players.Verify(p => p.UpdatePlayer(It.IsAny<Player>()), Times.Never);
    }
    [Fact]
    public async Task Retry_updates_only_demo_education_and_keeps_rewards()
    {
        var f = new Fixture();
        var first = await f.Handler().Handle(new() { AdminUserId = 1 }, default);
        await f.Handler().Handle(new() { AdminUserId = 1 }, default);
        Assert.Equal(12, first.Questions); Assert.Equal(8, first.DevelopmentPlayers);
        f.Players.Verify(p => p.UpdatePlayer(It.IsAny<Player>()), Times.Exactly(8));
        Assert.All(f.Profiles, p => { Assert.Equal(10, p.Grade); Assert.Equal(JourneyDemoContent.Curriculum, p.Curriculum);
            Assert.Equal(123, p.Gold); Assert.Equal(456, p.Experience); });
    }
    private sealed class Fixture
    {
        public Mock<IDevelopmentAuthenticationGuard> Guard = new();
        public Mock<IUserService> Users = new();
        public Mock<IPlayerRepository> Players = new();
        public Mock<IQuestionDataService> Questions = new();
        public UserIdentityResponse Admin = new() { Id = 1, IsPlatformAdmin = true, Status = "Active" };
        public List<UserIdentityResponse> Identities = new();
        public List<Player> Profiles = new();
        public Fixture()
        {
            Users.Setup(u => u.GetCurrentUser(1)).ReturnsAsync(Admin);
            foreach (var entry in DevelopmentPlayerCatalog.All)
            {
                var id = Identities.Count + 10;
                var user = new UserIdentityResponse { Id = id, PlayerProfileId = id + 100, UserName = entry.AccountKey, Email = entry.Email, Status = "Active" };
                var player = new Player { Id = id + 100, UserId = id, Email = entry.Email, Gold = 123, Experience = 456 };
                Identities.Add(user); Profiles.Add(player);
                Users.Setup(u => u.FindByEmail(entry.Email)).ReturnsAsync(user);
                Players.Setup(p => p.GetByUserIdAsync(id)).ReturnsAsync(player);
            }
        }
        public SeedJourneyDemoCommandHandler Handler() => new(Guard.Object, Users.Object, Players.Object, Questions.Object);
    }
}
