using Shared.Requests;

namespace Domain.Services;

public interface IStudentRosterFileService
{
    IReadOnlyList<StudentImportRow> ReadImport(byte[] bytes, string fileName);
    byte[] Write(string[] headers, IReadOnlyList<string[]> rows, string format);
}
