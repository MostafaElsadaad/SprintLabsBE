using Shared.Requests;

namespace Domain.Services
{
    public interface IBitbucketService
    {
        Task<bool> CommitContent(CommitRequest commitRequest);
    }
}