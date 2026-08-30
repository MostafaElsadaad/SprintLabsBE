using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedCommunityGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Value",
                table: "Grades",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
CREATE PROCEDURE `__ValidateFixedCommunityGrades`()
BEGIN
    IF EXISTS (
        SELECT 1
        FROM (
            SELECT
                `CommunityId`,
                CASE
                    WHEN TRIM(`Name`) REGEXP '^(7|8|9|10|11|12)$' THEN CAST(TRIM(`Name`) AS UNSIGNED)
                    WHEN LOWER(TRIM(`Name`)) REGEXP '^grade (7|8|9|10|11|12)$' THEN CAST(SUBSTRING_INDEX(LOWER(TRIM(`Name`)), ' ', -1) AS UNSIGNED)
                    ELSE NULL
                END AS `RecognizedValue`
            FROM `Grades`
        ) AS `recognized`
        WHERE `RecognizedValue` IS NOT NULL
        GROUP BY `CommunityId`, `RecognizedValue`
        HAVING COUNT(*) > 1
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'FixedCommunityGrades cannot continue: duplicate recognizable grades exist. Run the legacy-grade audit in specs/019-fixed-grades-teacher-classes/quickstart.md.';
    END IF;
END");

            migrationBuilder.Sql("CALL `__ValidateFixedCommunityGrades`();");
            migrationBuilder.Sql("DROP PROCEDURE `__ValidateFixedCommunityGrades`;");

            migrationBuilder.Sql(@"
UPDATE `Grades` AS `grade`
INNER JOIN (
    SELECT
        `Id`,
        CASE
            WHEN TRIM(`Name`) REGEXP '^(7|8|9|10|11|12)$' THEN CAST(TRIM(`Name`) AS UNSIGNED)
            WHEN LOWER(TRIM(`Name`)) REGEXP '^grade (7|8|9|10|11|12)$' THEN CAST(SUBSTRING_INDEX(LOWER(TRIM(`Name`)), ' ', -1) AS UNSIGNED)
            ELSE NULL
        END AS `RecognizedValue`
    FROM `Grades`
) AS `recognized` ON `recognized`.`Id` = `grade`.`Id`
SET `grade`.`Value` = `recognized`.`RecognizedValue`
WHERE `recognized`.`RecognizedValue` IS NOT NULL;");

            migrationBuilder.Sql(@"
INSERT INTO `Grades` (`CommunityId`, `Value`, `Name`, `SortOrder`, `CreatedAt`)
SELECT
    `community`.`Id`,
    `supported`.`Value`,
    CONCAT('Grade ', `supported`.`Value`),
    `supported`.`Value`,
    UTC_TIMESTAMP(6)
FROM `Communities` AS `community`
CROSS JOIN (
    SELECT 7 AS `Value` UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL
    SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
) AS `supported`
LEFT JOIN `Grades` AS `grade`
    ON `grade`.`CommunityId` = `community`.`Id`
    AND `grade`.`Value` = `supported`.`Value`
WHERE `grade`.`Id` IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_CommunityId_Value",
                table: "Grades",
                columns: new[] { "CommunityId", "Value" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Grades_Value_Supported",
                table: "Grades",
                sql: "`Value` IS NULL OR `Value` IN (7, 8, 9, 10, 11, 12)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Grades_CommunityId_Value",
                table: "Grades");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Grades_Value_Supported",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Grades");
        }
    }
}
