namespace Application.Features.Accounts.TeacherAuthentication.ResendConfirmation;

public class ResendConfirmationResponse
{
    public string Message { get; set; } = "If an account exists, a confirmation email has been sent.";
    public DateTime? ResendAvailableAt { get; set; }
}
