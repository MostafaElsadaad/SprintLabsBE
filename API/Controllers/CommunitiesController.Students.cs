using Application.Features.Communities.Students.StudentStats;
using Application.Features.Communities.Students.ImportStudents;
using Application.Features.Communities.Students.ExportStudents;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Exceptions;

namespace API.Controllers;

public partial class CommunitiesController
{
    [HttpGet("students/stats")]
    public Task<IActionResult> GetStudentStats([FromQuery] StudentStatsQuery query)
    {
        query.UserId = DashboardCallerId();
        return SendDashboard(query);
    }

    [HttpPost("students/import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> ImportStudents(IFormFile file)
    {
        var userId = DashboardCallerId();
        if (file == null || file.Length == 0) throw new GenericException(ErrorCode.ValidationError, "A student file is required.", System.Net.HttpStatusCode.BadRequest);
        if (file.Length > 5 * 1024 * 1024) throw new GenericException(ErrorCode.ValidationError, "File exceeds 5 MiB.", System.Net.HttpStatusCode.RequestEntityTooLarge);
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, HttpContext.RequestAborted);
        return await SendDashboard(new ImportStudentsCommand { UserId = userId, Bytes = buffer.ToArray(), FileName = Path.GetFileName(file.FileName) });
    }

    [HttpGet("students/export")]
    public async Task<IActionResult> ExportStudents([FromQuery] ExportStudentsQuery query)
    {
        query.UserId = DashboardCallerId();
        var result = await _mediator.Send(query, HttpContext.RequestAborted);
        return File(result.Bytes, result.ContentType, result.FileName);
    }
}
