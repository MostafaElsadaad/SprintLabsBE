namespace Domain.Models;

// A selectable curriculum/language pair, including examples before the question bank is populated.
public class PlayerEducationOption
{
    public string Curriculum { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}
