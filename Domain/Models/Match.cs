using Domain.Enums;

namespace Domain.Models;

public class Match
{
    public long Id { get; set; }
    public string MatchCode { get; set; } = default!;
    public string? MirrorRoomId { get; set; }
    public long? CommunityId { get; set; }
    public Domain.Enums.MatchType MatchType { get; set; } = Domain.Enums.MatchType.Friendly;
    public MatchStatus Status { get; set; } = MatchStatus.Created;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Community? Community { get; set; }
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<MatchQuestionResult> MatchQuestionResults { get; set; } = new List<MatchQuestionResult>();
    public ICollection<MatchRewardResult> MatchRewardResults { get; set; } = new List<MatchRewardResult>();
}
