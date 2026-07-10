using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Enums;

namespace Domain.Models;

public class Player
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string GoogleId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public int? Age { get; set; }
    public int? Grade { get; set; }
    public string? SchoolName { get; set; }

    // Progression
    public int Gold { get; set; } = 0;
    public int Experience { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int Rp { get; set; } = 0;
    public RankTier RankTier { get; set; } = RankTier.Student;
    public RankTier HighestRankTier { get; set; } = RankTier.Student;
    public int TotalMatches { get; set; } = 0;
    public int TotalWins { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<MatchQuestionResult> MatchQuestionResults { get; set; } = new List<MatchQuestionResult>();
    public ICollection<MatchRewardResult> MatchRewardResults { get; set; } = new List<MatchRewardResult>();
    public ICollection<PlayerXpLog> PlayerXpLogs { get; set; } = new List<PlayerXpLog>();
    public ICollection<PlayerRankLog> PlayerRankLogs { get; set; } = new List<PlayerRankLog>();
}
