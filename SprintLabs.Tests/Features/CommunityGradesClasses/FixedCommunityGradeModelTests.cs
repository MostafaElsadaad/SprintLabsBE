using Domain.Models;

using FluentAssertions;

namespace Compass.Tests.Features.CommunityGradesClasses;

public class FixedCommunityGradeModelTests
{
    [Fact]
    public async Task Grade_model_allows_legacy_null_values_and_prevents_duplicate_supported_values()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();

        var grade = context.Model.FindEntityType(typeof(Grade));

        grade.Should().NotBeNull();
        grade!.FindProperty(nameof(Grade.Value))!.IsNullable.Should().BeTrue();
        grade.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Grade.CommunityId), nameof(Grade.Value) }));
    }
}
