using MediatR;

namespace Application.Features.Users.GetCurrentUser
{
    public class GetCurrentUserQuery : IRequest<CurrentUserResponse>
    {
        public long UserId { get; set; }
    }
}