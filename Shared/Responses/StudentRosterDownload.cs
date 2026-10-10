namespace Shared.Responses;

public class StudentRosterDownload
{
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "";
    public string FileName { get; set; } = "";
}
