using Domain.Models;
using Domain.Models.QuestionData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess;

internal static class QuestionDataModelConfiguration
{
    public static void Configure(ModelBuilder model, bool mysql)
    {
        model.Entity<Question>(e =>
        {
            e.ToTable("Questions", t => t.HasCheckConstraint("CK_Questions_Numbers", "`Grade` > 0 AND `Term` > 0 AND `Unit` > 0 AND `Lesson` > 0"));
            e.HasKey(x => x.QuestionId);
            Uuid(e.Property(x => x.QuestionId));
            foreach (var name in new[] { "Curriculum", "Subject" }) e.Property<string>(name).IsRequired().HasMaxLength(120);
            e.Property(x => x.Language).IsRequired().HasMaxLength(20);
            e.Property(x => x.QuestionText).IsRequired().HasMaxLength(16000);
            e.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TimerSeconds).HasPrecision(10, 3).HasDefaultValue(10m);
            e.ToTable(t => t.HasCheckConstraint("CK_Questions_TimerSeconds", "`TimerSeconds` > 0"));
            e.HasIndex(x => new { x.Grade, x.Unit, x.Lesson });
        });
        model.Entity<MCQChoice>(e =>
        {
            e.ToTable("MCQChoices");
            e.HasKey(x => x.ChoiceId);
            Uuid(e.Property(x => x.QuestionId));
            e.HasOne<Question>().WithMany(x => x.Choices).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ChoiceId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.ChoiceId).UseCollation("utf8mb4_bin");
            e.Property(x => x.ChoiceText).IsRequired().HasMaxLength(4000);
        });
        model.Entity<TrueFalseAnswer>(e =>
        {
            e.ToTable("TrueFalseAnswers");
            e.HasKey(x => x.QuestionId);
            Uuid(e.Property(x => x.QuestionId));
            e.HasOne<Question>().WithMany(x => x.BooleanAnswers).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<FillBlankAnswer>(e =>
        {
            e.ToTable("FillBlankAnswers");
            e.HasKey(x => new { x.QuestionId, x.AcceptedAnswer });
            Uuid(e.Property(x => x.QuestionId));
            e.HasOne<Question>().WithMany(x => x.AcceptedAnswers).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AcceptedAnswer).IsRequired().HasMaxLength(255);
            if (mysql) e.Property(x => x.AcceptedAnswer).UseCollation("utf8mb4_bin");
        });
        model.Entity<OrderingItem>(e =>
        {
            e.ToTable("OrderingItems");
            e.HasKey(x => x.ItemId);
            Uuid(e.Property(x => x.QuestionId));
            e.HasOne<Question>().WithMany(x => x.Items).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ItemId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.ItemId).UseCollation("utf8mb4_bin");
            e.Property(x => x.ItemText).IsRequired().HasMaxLength(4000);
            e.HasIndex(x => new { x.QuestionId, x.CorrectPosition }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_OrderingItems_Position", "`CorrectPosition` >= 0"));
        });
        model.Entity<MatchingPair>(e =>
        {
            e.ToTable("MatchingPairs");
            e.HasKey(x => x.PairId);
            Uuid(e.Property(x => x.QuestionId));
            e.HasOne<Question>().WithMany(x => x.Pairs).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.PairId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.PairId).UseCollation("utf8mb4_bin");
            e.Property(x => x.LeftText).IsRequired().HasMaxLength(4000);
            e.Property(x => x.RightText).IsRequired().HasMaxLength(4000);
        });
        model.Entity<DragDropOption>(e =>
        {
            e.ToTable("DragDropOptions");
            e.HasKey(x => x.OptionId);
            Uuid(e.Property(x => x.QuestionId));
            e.HasOne<Question>().WithMany(x => x.Options).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.OptionId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.OptionId).UseCollation("utf8mb4_bin");
            e.Property(x => x.OptionText).IsRequired().HasMaxLength(4000);
        });
        model.Entity<DragDropAnswer>(e =>
        {
            e.ToTable("DragDropAnswers");
            e.HasKey(x => new { x.QuestionId, x.BlankNumber });
            Uuid(e.Property(x => x.QuestionId));
            e.HasOne<Question>().WithMany(x => x.BlankAnswers).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CorrectText).IsRequired().HasMaxLength(4000);
            e.ToTable(t => t.HasCheckConstraint("CK_DragDropAnswers_Blank", "`BlankNumber` >= 0"));
        });
        model.Entity<QuestionHistory>(e =>
        {
            e.ToTable("QuestionHistory", t => t.HasCheckConstraint("CK_QuestionHistory_Time", "`TimeTakenSeconds` >= 0"));
            e.Property(x => x.TimeTakenSeconds).HasPrecision(10, 3);
            e.HasKey(x => x.HistoryId);
            Uuid(e.Property(x => x.QuestionId));
            e.HasIndex(x => new { x.PlayerId, x.MatchId });
            e.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Match>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<MCQHistory>(e =>
        {
            e.ToTable("MCQHistory");
            e.HasKey(x => x.HistoryId);
            e.Property(x => x.HistoryId).ValueGeneratedNever();
            e.HasOne<QuestionHistory>().WithMany(x => x.MCQHistoryRows).HasForeignKey(x => x.HistoryId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SelectedChoiceId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.SelectedChoiceId).UseCollation("utf8mb4_bin");
            e.HasOne<MCQChoice>().WithMany().HasForeignKey(x => x.SelectedChoiceId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<TrueFalseHistory>(e =>
        {
            e.ToTable("TrueFalseHistory");
            e.HasKey(x => x.HistoryId);
            e.Property(x => x.HistoryId).ValueGeneratedNever();
            e.HasOne<QuestionHistory>().WithMany(x => x.TrueFalseHistoryRows).HasForeignKey(x => x.HistoryId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<FillBlankHistory>(e =>
        {
            e.ToTable("FillBlankHistory");
            e.HasKey(x => x.HistoryId);
            e.Property(x => x.HistoryId).ValueGeneratedNever();
            e.HasOne<QuestionHistory>().WithMany(x => x.FillBlankHistoryRows).HasForeignKey(x => x.HistoryId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AnswerText).IsRequired().HasMaxLength(4000);
        });
        model.Entity<OrderingHistory>(e =>
        {
            e.ToTable("OrderingHistory");
            e.HasKey(x => new { x.HistoryId, x.ItemId });
            e.Property(x => x.HistoryId).ValueGeneratedNever();
            e.HasOne<QuestionHistory>().WithMany(x => x.OrderingHistoryRows).HasForeignKey(x => x.HistoryId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ItemId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.ItemId).UseCollation("utf8mb4_bin");
            e.HasOne<OrderingItem>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.HistoryId, x.SelectedPosition }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_OrderingHistory_Position", "`SelectedPosition` >= 0"));
        });
        model.Entity<MatchingHistory>(e =>
        {
            e.ToTable("MatchingHistory");
            e.HasKey(x => new { x.HistoryId, x.LeftPairId });
            e.Property(x => x.HistoryId).ValueGeneratedNever();
            e.HasOne<QuestionHistory>().WithMany(x => x.MatchingHistoryRows).HasForeignKey(x => x.HistoryId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.LeftPairId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.LeftPairId).UseCollation("utf8mb4_bin");
            e.Property(x => x.SelectedRightPairId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.SelectedRightPairId).UseCollation("utf8mb4_bin");
            e.HasOne<MatchingPair>().WithMany().HasForeignKey(x => x.LeftPairId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MatchingPair>().WithMany().HasForeignKey(x => x.SelectedRightPairId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.HistoryId, x.SelectedRightPairId }).IsUnique();
        });
        model.Entity<DragDropHistory>(e =>
        {
            e.ToTable("DragDropHistory");
            e.HasKey(x => new { x.HistoryId, x.BlankNumber });
            e.Property(x => x.HistoryId).ValueGeneratedNever();
            e.HasOne<QuestionHistory>().WithMany(x => x.DragDropHistoryRows).HasForeignKey(x => x.HistoryId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SelectedOptionId).IsRequired().HasMaxLength(80);
            if (mysql) e.Property(x => x.SelectedOptionId).UseCollation("utf8mb4_bin");
            e.HasOne<DragDropOption>().WithMany().HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_DragDropHistory_Blank", "`BlankNumber` >= 0"));
        });
    }

    private static void Uuid(PropertyBuilder<Guid> property)
        => property.HasConversion(value => value.ToByteArray(true), value => new Guid(value, true))
            .HasColumnType("binary(16)").HasMaxLength(16).IsFixedLength().ValueGeneratedNever();
}
