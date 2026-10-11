using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestionDataScheme : Migration
    {
        // Preserve every existing pack row; no automatic educational UUID/metadata backfill.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "Questions", newName: "LegacyQuestionPacks");

            migrationBuilder.RenameIndex(name: "IX_Questions_Grade", table: "LegacyQuestionPacks", newName: "IX_LegacyQuestionPacks_Grade");
            migrationBuilder.RenameIndex(name: "IX_Questions_Grade_Assignment", table: "LegacyQuestionPacks", newName: "IX_LegacyQuestionPacks_Grade_Assignment");

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    Curriculum = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Term = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    Lesson = table.Column<int>(type: "int", nullable: false),
                    QuestionType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionText = table.Column<string>(type: "varchar(16000)", maxLength: 16000, nullable: false).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table => table.PrimaryKey("PK_Questions", x => x.QuestionId))
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DragDropAnswers",
                columns: table => new
                {
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    BlankNumber = table.Column<int>(type: "int", nullable: false),
                    CorrectText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DragDropAnswers", x => new { x.QuestionId, x.BlankNumber });
                    table.CheckConstraint("CK_DragDropAnswers_Blank", "`BlankNumber` >= 0");
                    table.ForeignKey(
                        name: "FK_DragDropAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DragDropOptions",
                columns: table => new
                {
                    OptionId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    OptionText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DragDropOptions", x => x.OptionId);
                    table.ForeignKey(
                        name: "FK_DragDropOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FillBlankAnswers",
                columns: table => new
                {
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    AcceptedAnswer = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillBlankAnswers", x => new { x.QuestionId, x.AcceptedAnswer });
                    table.ForeignKey(
                        name: "FK_FillBlankAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MatchingPairs",
                columns: table => new
                {
                    PairId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    LeftText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RightText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingPairs", x => x.PairId);
                    table.ForeignKey(
                        name: "FK_MatchingPairs_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MCQChoices",
                columns: table => new
                {
                    ChoiceId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    ChoiceText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MCQChoices", x => x.ChoiceId);
                    table.ForeignKey(
                        name: "FK_MCQChoices_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderingItems",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    ItemText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorrectPosition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderingItems", x => x.ItemId);
                    table.CheckConstraint("CK_OrderingItems_Position", "`CorrectPosition` >= 0");
                    table.ForeignKey(
                        name: "FK_OrderingItems_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QuestionHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    TimeTakenMs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionHistory", x => x.HistoryId);
                    table.CheckConstraint("CK_QuestionHistory_Time", "`TimeTakenMs` >= 0");
                    table.ForeignKey(
                        name: "FK_QuestionHistory_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionHistory_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionHistory_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrueFalseAnswers",
                columns: table => new
                {
                    QuestionId = table.Column<byte[]>(type: "binary(16)", fixedLength: true, maxLength: 16, nullable: false),
                    CorrectAnswer = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrueFalseAnswers", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_TrueFalseAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DragDropHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false),
                    BlankNumber = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DragDropHistory", x => new { x.HistoryId, x.BlankNumber });
                    table.CheckConstraint("CK_DragDropHistory_Blank", "`BlankNumber` >= 0");
                    table.ForeignKey(
                        name: "FK_DragDropHistory_DragDropOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "DragDropOptions",
                        principalColumn: "OptionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DragDropHistory_QuestionHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "QuestionHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FillBlankHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false),
                    AnswerText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillBlankHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_FillBlankHistory_QuestionHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "QuestionHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MatchingHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false),
                    LeftPairId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SelectedRightPairId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingHistory", x => new { x.HistoryId, x.LeftPairId });
                    table.ForeignKey(
                        name: "FK_MatchingHistory_MatchingPairs_LeftPairId",
                        column: x => x.LeftPairId,
                        principalTable: "MatchingPairs",
                        principalColumn: "PairId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchingHistory_MatchingPairs_SelectedRightPairId",
                        column: x => x.SelectedRightPairId,
                        principalTable: "MatchingPairs",
                        principalColumn: "PairId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchingHistory_QuestionHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "QuestionHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MCQHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false),
                    SelectedChoiceId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MCQHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_MCQHistory_MCQChoices_SelectedChoiceId",
                        column: x => x.SelectedChoiceId,
                        principalTable: "MCQChoices",
                        principalColumn: "ChoiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MCQHistory_QuestionHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "QuestionHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderingHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SelectedPosition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderingHistory", x => new { x.HistoryId, x.ItemId });
                    table.CheckConstraint("CK_OrderingHistory_Position", "`SelectedPosition` >= 0");
                    table.ForeignKey(
                        name: "FK_OrderingHistory_OrderingItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "OrderingItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderingHistory_QuestionHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "QuestionHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrueFalseHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false),
                    SelectedAnswer = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrueFalseHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_TrueFalseHistory_QuestionHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "QuestionHistory",
                        principalColumn: "HistoryId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Grade_Unit_Lesson",
                table: "Questions",
                columns: new[] { "Grade", "Unit", "Lesson" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` > 0 AND `Unit` > 0 AND `Lesson` > 0");

            migrationBuilder.CreateIndex(
                name: "IX_DragDropHistory_SelectedOptionId",
                table: "DragDropHistory",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DragDropOptions_QuestionId",
                table: "DragDropOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHistory_HistoryId_SelectedRightPairId",
                table: "MatchingHistory",
                columns: new[] { "HistoryId", "SelectedRightPairId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHistory_LeftPairId",
                table: "MatchingHistory",
                column: "LeftPairId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHistory_SelectedRightPairId",
                table: "MatchingHistory",
                column: "SelectedRightPairId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingPairs_QuestionId",
                table: "MatchingPairs",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_MCQChoices_QuestionId",
                table: "MCQChoices",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_MCQHistory_SelectedChoiceId",
                table: "MCQHistory",
                column: "SelectedChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderingHistory_HistoryId_SelectedPosition",
                table: "OrderingHistory",
                columns: new[] { "HistoryId", "SelectedPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderingHistory_ItemId",
                table: "OrderingHistory",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderingItems_QuestionId_CorrectPosition",
                table: "OrderingItems",
                columns: new[] { "QuestionId", "CorrectPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionHistory_MatchId",
                table: "QuestionHistory",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionHistory_PlayerId_MatchId",
                table: "QuestionHistory",
                columns: new[] { "PlayerId", "MatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionHistory_QuestionId",
                table: "QuestionHistory",
                column: "QuestionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DragDropAnswers");

            migrationBuilder.DropTable(
                name: "DragDropHistory");

            migrationBuilder.DropTable(
                name: "FillBlankAnswers");

            migrationBuilder.DropTable(
                name: "FillBlankHistory");

            migrationBuilder.DropTable(
                name: "MatchingHistory");

            migrationBuilder.DropTable(
                name: "MCQHistory");

            migrationBuilder.DropTable(
                name: "OrderingHistory");

            migrationBuilder.DropTable(
                name: "TrueFalseAnswers");

            migrationBuilder.DropTable(
                name: "TrueFalseHistory");

            migrationBuilder.DropTable(
                name: "DragDropOptions");

            migrationBuilder.DropTable(
                name: "MatchingPairs");

            migrationBuilder.DropTable(
                name: "MCQChoices");

            migrationBuilder.DropTable(
                name: "OrderingItems");

            migrationBuilder.DropTable(
                name: "QuestionHistory");

            migrationBuilder.DropTable(name: "Questions");

            migrationBuilder.RenameIndex(name: "IX_LegacyQuestionPacks_Grade", table: "LegacyQuestionPacks", newName: "IX_Questions_Grade");

            migrationBuilder.RenameIndex(name: "IX_LegacyQuestionPacks_Grade_Assignment", table: "LegacyQuestionPacks", newName: "IX_Questions_Grade_Assignment");

            migrationBuilder.RenameTable(name: "LegacyQuestionPacks", newName: "Questions");
        }
    }
}
