IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822170138_AddCondominiumEntity'
)
BEGIN
    CREATE TABLE [Condominiums] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [CondominiumManagerId] nvarchar(max) NULL,
        [Name] nvarchar(100) NOT NULL,
        [Address] nvarchar(200) NOT NULL,
        [PropertyRegistryNumber] nvarchar(50) NOT NULL,
        [NumberOfUnits] int NOT NULL,
        [ContractValue] decimal(18,2) NOT NULL,
        [FeePerUnit] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [UserCreatedId] nvarchar(max) NOT NULL,
        [UserUpdatedId] nvarchar(max) NULL,
        [UserDeletedId] nvarchar(max) NULL,
        CONSTRAINT [PK_Condominiums] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822170138_AddCondominiumEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250822170138_AddCondominiumEntity', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250901164752_AddDeletedAtToCondominiums'
)
BEGIN
    ALTER TABLE [Condominiums] ADD [DeletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250901164752_AddDeletedAtToCondominiums'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250901164752_AddDeletedAtToCondominiums', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903210527_AddUnitEntityAndRemoveNumberOfUnits'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Condominiums]') AND [c].[name] = N'NumberOfUnits');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Condominiums] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Condominiums] DROP COLUMN [NumberOfUnits];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903210527_AddUnitEntityAndRemoveNumberOfUnits'
)
BEGIN
    CREATE TABLE [Units] (
        [Id] int NOT NULL IDENTITY,
        [UnitNumber] nvarchar(50) NOT NULL,
        [CondominiumId] int NOT NULL,
        [OwnerId] nvarchar(max) NULL,
        CONSTRAINT [PK_Units] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Units_Condominiums_CondominiumId] FOREIGN KEY ([CondominiumId]) REFERENCES [Condominiums] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903210527_AddUnitEntityAndRemoveNumberOfUnits'
)
BEGIN
    CREATE INDEX [IX_Units_CondominiumId] ON [Units] ([CondominiumId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903210527_AddUnitEntityAndRemoveNumberOfUnits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250903210527_AddUnitEntityAndRemoveNumberOfUnits', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250909194536_AddUniqueConstraintToPropertyRegistryNumber'
)
BEGIN
    ALTER TABLE [Condominiums] ADD [ZipCode] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250909194536_AddUniqueConstraintToPropertyRegistryNumber'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Condominiums_CompanyId_PropertyRegistryNumber] ON [Condominiums] ([CompanyId], [PropertyRegistryNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250909194536_AddUniqueConstraintToPropertyRegistryNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250909194536_AddUniqueConstraintToPropertyRegistryNumber', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250909211323_AddCityToCondominium'
)
BEGIN
    ALTER TABLE [Condominiums] ADD [City] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250909211323_AddCityToCondominium'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250909211323_AddCityToCondominium', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912022433_AddUniqueConstraintToUnitNumber'
)
BEGIN
    DROP INDEX [IX_Units_CondominiumId] ON [Units];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912022433_AddUniqueConstraintToUnitNumber'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Units_CondominiumId_UnitNumber] ON [Units] ([CondominiumId], [UnitNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912022433_AddUniqueConstraintToUnitNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250912022433_AddUniqueConstraintToUnitNumber', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912023402_AddAuditFieldsToUnit'
)
BEGIN
    ALTER TABLE [Units] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912023402_AddAuditFieldsToUnit'
)
BEGIN
    ALTER TABLE [Units] ADD [DeletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912023402_AddAuditFieldsToUnit'
)
BEGIN
    ALTER TABLE [Units] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912023402_AddAuditFieldsToUnit'
)
BEGIN
    ALTER TABLE [Units] ADD [UpdatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250912023402_AddAuditFieldsToUnit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250912023402_AddAuditFieldsToUnit', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251012191302_AddMessagingTables'
)
BEGIN
    CREATE TABLE [Conversations] (
        [Id] int NOT NULL IDENTITY,
        [Subject] nvarchar(200) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251012191302_AddMessagingTables'
)
BEGIN
    CREATE TABLE [Messages] (
        [Id] int NOT NULL IDENTITY,
        [Content] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [SenderId] nvarchar(max) NOT NULL,
        [ReceiverId] nvarchar(max) NULL,
        [ConversationId] int NOT NULL,
        [IsRead] bit NOT NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251012191302_AddMessagingTables'
)
BEGIN
    CREATE INDEX [IX_Messages_ConversationId] ON [Messages] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251012191302_AddMessagingTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251012191302_AddMessagingTables', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014214003_AddAuditUserIdsToUnits'
)
BEGIN
    ALTER TABLE [Units] ADD [UserCreatedId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014214003_AddAuditUserIdsToUnits'
)
BEGIN
    ALTER TABLE [Units] ADD [UserDeletedId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014214003_AddAuditUserIdsToUnits'
)
BEGIN
    ALTER TABLE [Units] ADD [UserUpdatedId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014214003_AddAuditUserIdsToUnits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251014214003_AddAuditUserIdsToUnits', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014221055_AddOwnerNavigationToUnit'
)
BEGIN
    CREATE TABLE [ApplicationUser] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [IdentificationDocument] nvarchar(20) NOT NULL,
        [DocumentType] int NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [CompanyId] int NULL,
        [CompanyName] nvarchar(100) NULL,
        [CondominiumId] int NULL,
        [Profession] nvarchar(50) NULL,
        [DeactivatedAt] datetime2 NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DeactivatedByUserId] nvarchar(max) NULL,
        [UserUpdatedId] nvarchar(max) NULL,
        [UserCreatedId] nvarchar(max) NULL,
        [UserName] nvarchar(max) NULL,
        [NormalizedUserName] nvarchar(max) NULL,
        [NormalizedEmail] nvarchar(max) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_ApplicationUser] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014221055_AddOwnerNavigationToUnit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251014221055_AddOwnerNavigationToUnit', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014230735_AddAssignedToIdToConversations'
)
BEGIN
    ALTER TABLE [Conversations] ADD [AssignedToId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014230735_AddAssignedToIdToConversations'
)
BEGIN
    ALTER TABLE [Conversations] ADD [InitiatorId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014230735_AddAssignedToIdToConversations'
)
BEGIN
    ALTER TABLE [Conversations] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014230735_AddAssignedToIdToConversations'
)
BEGIN
    ALTER TABLE [Conversations] ADD [UnitId] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251014230735_AddAssignedToIdToConversations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251014230735_AddAssignedToIdToConversations', N'8.0.18');
END;
GO

COMMIT;
GO

