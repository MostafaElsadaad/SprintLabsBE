namespace Domain.Enums;

public enum MatchType
{
    Ranked = 1,
    Friendly = 2,
    Private = 3
}

public enum MatchStatus
{
    Created = 1,
    Started = 2,
    Completed = 3,
    Cancelled = 4
}

public enum RankTier
{
    Student = 1,
    Seeker = 2,
    Challenger = 3,
    Arcanist = 4,
    Expert = 5,
    Master = 6,
    GrandMaster = 7,
    Legend = 8,
    Immortal = 9
}

public enum XpSourceType
{
    MatchAnswer = 1,
    MatchResult = 2,
    Mission = 3
}
