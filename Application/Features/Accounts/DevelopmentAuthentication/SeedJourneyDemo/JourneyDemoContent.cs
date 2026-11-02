using Shared.Requests.QuestionData;
namespace Application.Features.Accounts.DevelopmentAuthentication.SeedJourneyDemo;
public static class JourneyDemoContent
{
    public const string Curriculum = "SprintLab Test High School";
    public const int Grade = 10;
    public static List<QuestionDataRequest> Create()
    {
        var rows = new (string Subject, int Unit, string Text, string Correct, string Wrong)[]
        {
            ("Physics", 1, "What is the SI unit of speed?", "metres per second", "kilograms"),
            ("Physics", 1, "Distance divided by time gives which quantity?", "speed", "mass"),
            ("Physics", 1, "An object covers 20 metres in 4 seconds. What is its average speed?", "5 m/s", "80 m/s"),
            ("Physics", 2, "What is the SI unit of force?", "newton", "joule"),
            ("Physics", 2, "Which equation describes Newton's second law?", "F = ma", "F = m/a"),
            ("Physics", 2, "What is the acceleration of a 2 kg object acted on by a net force of 6 N?", "3 m/s squared", "12 m/s squared"),
            ("Chemistry", 1, "Which particle has a negative electric charge?", "electron", "proton"),
            ("Chemistry", 1, "The atomic number equals the number of which particles?", "protons", "neutrons"),
            ("Chemistry", 1, "Where are protons found?", "in the nucleus", "outside the nucleus"),
            ("Chemistry", 2, "What is the chemical formula of water?", "H2O", "CO2"),
            ("Chemistry", 2, "Which substance has a pH below 7?", "an acid", "a base"),
            ("Chemistry", 2, "What does a catalyst change?", "reaction rate", "total mass of the reactants")
        };
        return rows.Select((r, i) => new QuestionDataRequest
        {
            QuestionId = Guid.Parse($"d37e2026-1102-4000-8000-{i + 1:000000000000}"),
            Curriculum = Curriculum, Grade = Grade, Language = "en", Subject = r.Subject,
            Term = 1, Unit = r.Unit, Lesson = 1, QuestionType = "MCQ", QuestionText = r.Text, TimerSeconds = 30,
            Choices = new() { new() { ChoiceId = $"journey-demo-{i}-a", ChoiceText = r.Correct, IsCorrect = true },
                new() { ChoiceId = $"journey-demo-{i}-b", ChoiceText = r.Wrong, IsCorrect = false } }
        }).ToList();
    }
}
