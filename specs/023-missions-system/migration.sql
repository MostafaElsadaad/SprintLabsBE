START TRANSACTION;

CREATE TABLE `Items` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ItemKey` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Rarity` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `IconKey` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `PrefabKey` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Items` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `MissionActivations` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `PeriodType` int NOT NULL,
    `StartsAt` datetime(6) NOT NULL,
    `EndsAt` datetime(6) NULL,
    `RandomMissionCount` int NOT NULL,
    `AutoClaimCompletedOnReset` tinyint(1) NOT NULL,
    `Status` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_MissionActivations` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `MissionEventLogs` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `EventId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `PlayerProfileId` bigint NOT NULL,
    `CommunityId` bigint NULL,
    `MatchId` bigint NULL,
    `EventType` int NOT NULL,
    `EventDataJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `OccurredAt` datetime(6) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ResultJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_MissionEventLogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MissionEventLogs_Communities_CommunityId` FOREIGN KEY (`CommunityId`) REFERENCES `Communities` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionEventLogs_Matches_MatchId` FOREIGN KEY (`MatchId`) REFERENCES `Matches` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionEventLogs_Players_PlayerProfileId` FOREIGN KEY (`PlayerProfileId`) REFERENCES `Players` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `MissionTemplates` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `MissionKey` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
    `Category` int NOT NULL,
    `PeriodType` int NOT NULL,
    `EventType` int NOT NULL,
    `ProgressType` int NOT NULL,
    `TargetValue` int NOT NULL,
    `ConditionsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ProgressField` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ResetEventType` int NULL,
    `MatchScoped` tinyint(1) NOT NULL,
    `RepeatValue` tinyint(1) NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_MissionTemplates` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `ShopProducts` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ShopProducts` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlayerInventoryItems` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PlayerProfileId` bigint NOT NULL,
    `ItemId` bigint NOT NULL,
    `Quantity` int NOT NULL,
    CONSTRAINT `PK_PlayerInventoryItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PlayerInventoryItems_Items_ItemId` FOREIGN KEY (`ItemId`) REFERENCES `Items` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlayerInventoryItems_Players_PlayerProfileId` FOREIGN KEY (`PlayerProfileId`) REFERENCES `Players` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlayerMissionAssignments` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PlayerProfileId` bigint NOT NULL,
    `MissionActivationId` bigint NOT NULL,
    `AssignedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlayerMissionAssignments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PlayerMissionAssignments_MissionActivations_MissionActivatio~` FOREIGN KEY (`MissionActivationId`) REFERENCES `MissionActivations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlayerMissionAssignments_Players_PlayerProfileId` FOREIGN KEY (`PlayerProfileId`) REFERENCES `Players` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `MissionActivationItems` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `MissionActivationId` bigint NOT NULL,
    `MissionTemplateId` bigint NOT NULL,
    `AssignmentMode` int NOT NULL,
    `Weight` int NOT NULL,
    `SortOrder` int NOT NULL,
    CONSTRAINT `PK_MissionActivationItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MissionActivationItems_MissionActivations_MissionActivationId` FOREIGN KEY (`MissionActivationId`) REFERENCES `MissionActivations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionActivationItems_MissionTemplates_MissionTemplateId` FOREIGN KEY (`MissionTemplateId`) REFERENCES `MissionTemplates` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlayerMissions` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PlayerProfileId` bigint NOT NULL,
    `MissionActivationId` bigint NOT NULL,
    `MissionTemplateId` bigint NOT NULL,
    `CurrentProgress` int NOT NULL,
    `TargetProgress` int NOT NULL,
    `Status` int NOT NULL,
    `ProgressStateJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DefinitionSnapshotJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ClaimSnapshotJson` longtext CHARACTER SET utf8mb4 NULL,
    `AssignedAt` datetime(6) NOT NULL,
    `CompletedAt` datetime(6) NULL,
    `ClaimedAt` datetime(6) NULL,
    `ExpiresAt` datetime(6) NULL,
    `LastEventAt` datetime(6) NULL,
    CONSTRAINT `PK_PlayerMissions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PlayerMissions_MissionActivations_MissionActivationId` FOREIGN KEY (`MissionActivationId`) REFERENCES `MissionActivations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlayerMissions_MissionTemplates_MissionTemplateId` FOREIGN KEY (`MissionTemplateId`) REFERENCES `MissionTemplates` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlayerMissions_Players_PlayerProfileId` FOREIGN KEY (`PlayerProfileId`) REFERENCES `Players` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `BoxRewardEntries` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ShopProductId` bigint NOT NULL,
    `ItemId` bigint NOT NULL,
    `Weight` int NOT NULL,
    `Quantity` int NOT NULL,
    CONSTRAINT `PK_BoxRewardEntries` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_BoxRewardEntries_Items_ItemId` FOREIGN KEY (`ItemId`) REFERENCES `Items` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_BoxRewardEntries_ShopProducts_ShopProductId` FOREIGN KEY (`ShopProductId`) REFERENCES `ShopProducts` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `MissionRewards` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `MissionTemplateId` bigint NOT NULL,
    `RewardType` int NOT NULL,
    `Amount` int NULL,
    `ShopProductId` bigint NULL,
    CONSTRAINT `PK_MissionRewards` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MissionRewards_MissionTemplates_MissionTemplateId` FOREIGN KEY (`MissionTemplateId`) REFERENCES `MissionTemplates` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionRewards_ShopProducts_ShopProductId` FOREIGN KEY (`ShopProductId`) REFERENCES `ShopProducts` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `MissionClaimLogs` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PlayerMissionId` bigint NOT NULL,
    `PlayerProfileId` bigint NOT NULL,
    `MissionTemplateId` bigint NOT NULL,
    `RewardType` int NOT NULL,
    `Amount` int NOT NULL,
    `ShopProductId` bigint NULL,
    `RewardItemId` bigint NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_MissionClaimLogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MissionClaimLogs_Items_RewardItemId` FOREIGN KEY (`RewardItemId`) REFERENCES `Items` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionClaimLogs_MissionTemplates_MissionTemplateId` FOREIGN KEY (`MissionTemplateId`) REFERENCES `MissionTemplates` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionClaimLogs_PlayerMissions_PlayerMissionId` FOREIGN KEY (`PlayerMissionId`) REFERENCES `PlayerMissions` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionClaimLogs_Players_PlayerProfileId` FOREIGN KEY (`PlayerProfileId`) REFERENCES `Players` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_MissionClaimLogs_ShopProducts_ShopProductId` FOREIGN KEY (`ShopProductId`) REFERENCES `ShopProducts` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_BoxRewardEntries_ItemId` ON `BoxRewardEntries` (`ItemId`);

CREATE INDEX `IX_BoxRewardEntries_ShopProductId` ON `BoxRewardEntries` (`ShopProductId`);

CREATE UNIQUE INDEX `IX_Items_ItemKey` ON `Items` (`ItemKey`);

CREATE UNIQUE INDEX `IX_MissionActivationItems_MissionActivationId_MissionTemplateId` ON `MissionActivationItems` (`MissionActivationId`, `MissionTemplateId`);

CREATE INDEX `IX_MissionActivationItems_MissionTemplateId` ON `MissionActivationItems` (`MissionTemplateId`);

CREATE UNIQUE INDEX `IX_MissionActivations_Name` ON `MissionActivations` (`Name`);

CREATE INDEX `IX_MissionActivations_Status_EndsAt` ON `MissionActivations` (`Status`, `EndsAt`);

CREATE INDEX `IX_MissionClaimLogs_MissionTemplateId` ON `MissionClaimLogs` (`MissionTemplateId`);

CREATE INDEX `IX_MissionClaimLogs_PlayerMissionId` ON `MissionClaimLogs` (`PlayerMissionId`);

CREATE INDEX `IX_MissionClaimLogs_PlayerProfileId` ON `MissionClaimLogs` (`PlayerProfileId`);

CREATE INDEX `IX_MissionClaimLogs_RewardItemId` ON `MissionClaimLogs` (`RewardItemId`);

CREATE INDEX `IX_MissionClaimLogs_ShopProductId` ON `MissionClaimLogs` (`ShopProductId`);

CREATE INDEX `IX_MissionEventLogs_CommunityId` ON `MissionEventLogs` (`CommunityId`);

CREATE INDEX `IX_MissionEventLogs_MatchId` ON `MissionEventLogs` (`MatchId`);

CREATE UNIQUE INDEX `IX_MissionEventLogs_PlayerProfileId_EventId` ON `MissionEventLogs` (`PlayerProfileId`, `EventId`);

CREATE INDEX `IX_MissionRewards_MissionTemplateId` ON `MissionRewards` (`MissionTemplateId`);

CREATE INDEX `IX_MissionRewards_ShopProductId` ON `MissionRewards` (`ShopProductId`);

CREATE UNIQUE INDEX `IX_MissionTemplates_MissionKey` ON `MissionTemplates` (`MissionKey`);

CREATE INDEX `IX_PlayerInventoryItems_ItemId` ON `PlayerInventoryItems` (`ItemId`);

CREATE UNIQUE INDEX `IX_PlayerInventoryItems_PlayerProfileId_ItemId` ON `PlayerInventoryItems` (`PlayerProfileId`, `ItemId`);

CREATE INDEX `IX_PlayerMissionAssignments_MissionActivationId` ON `PlayerMissionAssignments` (`MissionActivationId`);

CREATE UNIQUE INDEX `IX_PlayerMissionAssignments_PlayerProfileId_MissionActivationId` ON `PlayerMissionAssignments` (`PlayerProfileId`, `MissionActivationId`);

CREATE INDEX `IX_PlayerMissions_MissionActivationId_Status` ON `PlayerMissions` (`MissionActivationId`, `Status`);

CREATE INDEX `IX_PlayerMissions_MissionTemplateId` ON `PlayerMissions` (`MissionTemplateId`);

CREATE UNIQUE INDEX `IX_PlayerMissions_PlayerProfileId_MissionActivationId_MissionTe~` ON `PlayerMissions` (`PlayerProfileId`, `MissionActivationId`, `MissionTemplateId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261003223626_MissionsSystem', '8.0.8');

COMMIT;
