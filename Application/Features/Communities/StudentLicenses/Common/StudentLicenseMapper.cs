using Domain.Models;
using Domain.Services;

namespace Application.Features.Communities.StudentLicenses.Common;

public static class StudentLicenseMapper
{
    public static async Task<StudentLicenseResponse> Map(
        StudentLicense license,
        IUserService userService)
    {
        var assignedBy = await userService.GetCurrentUser(license.AssignedByUserId);

        return new StudentLicenseResponse
        {
            Id = license.Id,
            CommunityId = license.CommunityId,
            Email = license.Email,
            UserId = license.UserId,
            PlayerProfileId = license.PlayerProfileId,
            Status = license.Status.ToString(),
            Grade = new StudentLicenseGradeResponse
            {
                Id = license.GradeId,
                Name = license.Grade?.Name ?? string.Empty
            },
            Class = new StudentLicenseClassResponse
            {
                Id = license.ClassId,
                Name = license.Class?.Name ?? string.Empty
            },
            EmailChangeCount = license.EmailChangeCount,
            AssignedByUserId = license.AssignedByUserId,
            AssignedByName = assignedBy?.Name ?? string.Empty,
            ActivatedAt = license.ActivatedAt,
            CreatedAt = license.CreatedAt
        };
    }
}
