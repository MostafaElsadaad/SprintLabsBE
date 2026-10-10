using System.Globalization;
using System.Net;
using Application.Features.CommunityDashboard.Common;
using Application.Features.Communities.StudentLicenses.AddStudentLicense;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Enums;

namespace Application.Features.Communities.Students.ImportStudents;

public class ImportStudentsCommandHandler(DashboardAuthorization authorization, IStudentRosterFileService files,
    IBaseRepository<StudentLicense> licenses, ISender sender) : IRequestHandler<ImportStudentsCommand, ImportStudentsResponse>
{
    public async Task<ImportStudentsResponse> Handle(ImportStudentsCommand request, CancellationToken ct)
    {
        var scope = await authorization.ResolveAsync(request.UserId, true, ct);
        var rows = files.ReadImport(request.Bytes, request.FileName);
        var result = new ImportStudentsResponse();
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            var email = row.Email.Trim().ToLowerInvariant();
            var output = new ImportStudentResult { RowNumber = row.RowNumber, Email = email };
            result.Rows.Add(output);
            if (!long.TryParse(row.GradeId, NumberStyles.None, CultureInfo.InvariantCulture, out var gradeId) || gradeId <= 0 ||
                !long.TryParse(row.ClassId, NumberStyles.None, CultureInfo.InvariantCulture, out var classId) || classId <= 0 ||
                string.IsNullOrWhiteSpace(email) || email.Length > 256 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            { output.Outcome = "FAILED"; output.ReasonCode = "INVALID_ROW"; output.Reason = "Valid email, gradeId and classId are required."; continue; }
            if (await licenses.AsQueryable().AnyAsync(x => x.CommunityId == scope.CommunityId && x.Email == email && x.Status != StudentLicenseStatus.Revoked, ct))
            { output.Outcome = "SKIPPED"; output.ReasonCode = "ALREADY_ENROLLED"; output.Reason = "A non-revoked enrollment already exists."; continue; }
            try
            {
                var enrolled = await sender.Send(new AddStudentLicenseCommand { UserId = request.UserId, CommunityId = scope.CommunityId,
                    Email = email, GradeId = gradeId, ClassId = classId }, ct);
                output.LicenseId = enrolled.Id; output.Outcome = "IMPORTED";
            }
            catch (GenericException ex) when (ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            {
                // A concurrent import may have enrolled this email after our initial duplicate check.
                var duplicate = await licenses.AsQueryable().AnyAsync(x => x.CommunityId == scope.CommunityId && x.Email == email && x.Status != StudentLicenseStatus.Revoked, ct);
                output.Outcome = duplicate ? "SKIPPED" : "FAILED";
                var exhausted = ex.Message == ErrorMessage.NoStudentLicenseSeats;
                output.ReasonCode = duplicate ? "ALREADY_ENROLLED" : exhausted ? "NO_AVAILABLE_SEATS" : "ENROLLMENT_REJECTED";
                output.Reason = duplicate ? "A non-revoked enrollment already exists." : exhausted ? ErrorMessage.NoStudentLicenseSeats :
                    "Enrollment rejected: check class/grade and membership.";
            }
        }
        return result;
    }
}
