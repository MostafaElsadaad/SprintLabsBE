using System.Net;

using Application.Features.Communities.GradesClasses.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.GradesClasses.CreateGrade;

public class CreateGradeCommandHandler : IRequestHandler<CreateGradeCommand, GradeResponse>
{
    private const int NameMaxLength = 120;

    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<Grade> _gradeRepository;

    public CreateGradeCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<Grade> gradeRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _gradeRepository = gradeRepository;
    }

    public async Task<GradeResponse> Handle(
        CreateGradeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            request.SortOrder < 0 ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            throw InvalidInput();
        }

        var name = request.Name.Trim();
        if (name.Length > NameMaxLength)
        {
            throw InvalidInput();
        }

        await CommunityGradesClassesAuthorization.EnsureCanManage(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        var grade = new Grade
        {
            CommunityId = request.CommunityId,
            Name = name,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        await _gradeRepository.AddAsync(grade);
        await _gradeRepository.SaveChangesAsync();

        return new GradeResponse
        {
            Id = grade.Id,
            CommunityId = grade.CommunityId,
            Name = grade.Name,
            SortOrder = grade.SortOrder,
            ClassCount = 0,
            CreatedAt = grade.CreatedAt
        };
    }

    private static GenericException InvalidInput()
    {
        return new GenericException(
            message: ErrorMessage.InvalidInput,
            statusCode: HttpStatusCode.BadRequest,
            errorCode: ErrorCode.Failure);
    }
}
