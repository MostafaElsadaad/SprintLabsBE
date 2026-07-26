using FluentAssertions;

using Shared.Helpers;

namespace Compass.Tests.Features.TeacherEmailAuthentication;

public class UrlSafeTokenHelperTests
{
    [Fact]
    public void Token_round_trips_in_url_safe_form()
    {
        var encoded = UrlSafeTokenHelper.Encode("a+/teacher token=");
        UrlSafeTokenHelper.TryDecode(encoded, out var decoded).Should().BeTrue();
        decoded.Should().Be("a+/teacher token=");
        encoded.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }
}
