namespace Application.Features.Accounts.TeacherAuthentication.RegisterTeacher;

public class RegisterTeacherResponse
{
    public string Message { get; set; } = "Registration successful. Please check your email to confirm your account.";
    public DateTime ResendAvailableAt { get; set; }
}
