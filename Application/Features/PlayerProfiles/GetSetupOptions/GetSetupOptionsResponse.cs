namespace Application.Features.PlayerProfiles.GetSetupOptions;

public class GetSetupOptionsResponse
{
    public int[] Grades { get; set; } = [7, 8, 9, 10, 11, 12];
    public List<EducationOptionDto> EducationOptions { get; set; } = new();
}
