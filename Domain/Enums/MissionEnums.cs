namespace Domain.Enums;
public enum MissionCategory { Exploration = 1, Progress, Competitive, Lucky }
public enum MissionPeriodType { Weekly = 1, Monthly, Open }
public enum MissionEventType { PowerupUsed = 1, MonsterDefeated, CorrectAnswer, WrongAnswer, StreakReached, MatchWon, MatchCompleted, DiceRolled, UnitUnlocked, PathDiscovered, MonsterFightLost }
public enum MissionProgressType { Count = 1, Boolean, MaxValue, Streak, UniqueCount }
public enum MissionRewardType { XP = 1, Coins, Box }
public enum MissionAssignmentMode { Fixed = 1, RandomPool }
public enum MissionActivationStatus { Active = 1, Ended, Cancelled }
public enum PlayerMissionStatus { Assigned = 1, InProgress, Completed, Claimed, Expired, AutoClaimed }
