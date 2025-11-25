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
    WHERE [MigrationId] = N'20250817195939_DbInit01'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250817195939_DbInit01', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822181123_AddDeactivationAuditFieldsToCompany'
)
BEGIN
    ALTER TABLE [Companies] ADD [DeletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822181123_AddDeactivationAuditFieldsToCompany'
)
BEGIN
    ALTER TABLE [Companies] ADD [UserDeletedId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822181123_AddDeactivationAuditFieldsToCompany'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250822181123_AddDeactivationAuditFieldsToCompany', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822190500_AddAuditFieldsToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DeactivatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822190500_AddAuditFieldsToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DeactivatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822190500_AddAuditFieldsToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [UpdatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822190500_AddAuditFieldsToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [UserUpdatedId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250822190500_AddAuditFieldsToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250822190500_AddAuditFieldsToUser', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250830130132_AddUserCreatedId'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [UserCreatedId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250830130132_AddUserCreatedId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250830130132_AddUserCreatedId', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902183715_AddCreatedAtToUsers'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'CompanyName');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [CompanyName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902183715_AddCreatedAtToUsers'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902183715_AddCreatedAtToUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250902183715_AddCreatedAtToUsers', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902203807_AddCondominiumToUsers'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CondominiumId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902203807_AddCondominiumToUsers'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Profession] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902203807_AddCondominiumToUsers'
)
BEGIN
    CREATE TABLE [Condominium] (
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
        [DeletedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [UserCreatedId] nvarchar(max) NOT NULL,
        [UserUpdatedId] nvarchar(max) NULL,
        [UserDeletedId] nvarchar(max) NULL,
        CONSTRAINT [PK_Condominium] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902203807_AddCondominiumToUsers'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_CondominiumId] ON [AspNetUsers] ([CondominiumId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902203807_AddCondominiumToUsers'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Condominium_CondominiumId] FOREIGN KEY ([CondominiumId]) REFERENCES [Condominium] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250902203807_AddCondominiumToUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250902203807_AddCondominiumToUsers', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903170506_AdjustUserToCondoRelationship'
)
BEGIN
    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_Condominium_CondominiumId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903170506_AdjustUserToCondoRelationship'
)
BEGIN
    DROP TABLE [Condominium];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903170506_AdjustUserToCondoRelationship'
)
BEGIN
    DROP INDEX [IX_AspNetUsers_CondominiumId] ON [AspNetUsers];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250903170506_AdjustUserToCondoRelationship'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250903170506_AdjustUserToCondoRelationship', N'8.0.18');
END;
GO

COMMIT;
GO

