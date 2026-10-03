START TRANSACTION;

ALTER TABLE `Matches` DROP INDEX `IX_Matches_MatchCode`;

ALTER TABLE `Matches` ADD `TotalPlayers` int NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX `IX_Matches_MatchCode` ON `Matches` (`MatchCode`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261003194331_MatchProgressionApiIntegrity', '8.0.8');

COMMIT;

START TRANSACTION;

ALTER TABLE `MatchQuestionResults` ADD `Sequence` int NOT NULL DEFAULT 0;

CREATE INDEX `IX_MatchQuestionResults_MatchId_PlayerProfileId_Sequence` ON `MatchQuestionResults` (`MatchId`, `PlayerProfileId`, `Sequence`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261003195828_MatchAnswerOrdering', '8.0.8');

COMMIT;
