using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftflipSolutions.Migrations
{
    [DbContext(typeof(SoftflipSolutions.Data.ApplicationDbContext))]
    [Migration("20260821190000_PartnerPanelExtras")]
    public partial class PartnerPanelExtras : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: previous run may have applied Stage/ReferralCode before failing on FK.
            migrationBuilder.Sql(@"
IF COL_LENGTH('PartnerClients', 'Stage') IS NULL
    ALTER TABLE [PartnerClients] ADD [Stage] nvarchar(30) NOT NULL CONSTRAINT DF_PartnerClients_Stage DEFAULT N'New';

IF COL_LENGTH('ChannelPartners', 'ReferralCode') IS NULL
    ALTER TABLE [ChannelPartners] ADD [ReferralCode] nvarchar(40) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PartnerClients_ChannelPartnerId_Stage')
    CREATE INDEX [IX_PartnerClients_ChannelPartnerId_Stage] ON [PartnerClients] ([ChannelPartnerId], [Stage]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChannelPartners_ReferralCode')
    CREATE INDEX [IX_ChannelPartners_ReferralCode] ON [ChannelPartners] ([ReferralCode]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[PartnerInvoices]', N'U') IS NULL
BEGIN
    CREATE TABLE [PartnerInvoices] (
        [Id] int NOT NULL IDENTITY,
        [ChannelPartnerId] int NOT NULL,
        [PartnerClientId] int NOT NULL,
        [PartnerProposalId] int NULL,
        [InvoiceNumber] nvarchar(30) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [AmountPaid] decimal(18,2) NOT NULL,
        [Cgst] decimal(18,2) NOT NULL,
        [Sgst] decimal(18,2) NOT NULL,
        [Igst] decimal(18,2) NOT NULL,
        [HsnSac] nvarchar(20) NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PaidAt] datetime2 NULL,
        CONSTRAINT [PK_PartnerInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartnerInvoices_ChannelPartners_ChannelPartnerId] FOREIGN KEY ([ChannelPartnerId]) REFERENCES [ChannelPartners] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PartnerInvoices_PartnerClients_PartnerClientId] FOREIGN KEY ([PartnerClientId]) REFERENCES [PartnerClients] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PartnerInvoices_PartnerProposals_PartnerProposalId] FOREIGN KEY ([PartnerProposalId]) REFERENCES [PartnerProposals] ([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_PartnerInvoices_InvoiceNumber] ON [PartnerInvoices] ([InvoiceNumber]);
    CREATE INDEX [IX_PartnerInvoices_ChannelPartnerId] ON [PartnerInvoices] ([ChannelPartnerId]);
    CREATE INDEX [IX_PartnerInvoices_PartnerClientId] ON [PartnerInvoices] ([PartnerClientId]);
    CREATE INDEX [IX_PartnerInvoices_PartnerProposalId] ON [PartnerInvoices] ([PartnerProposalId]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[PartnerTickets]', N'U') IS NULL
BEGIN
    CREATE TABLE [PartnerTickets] (
        [Id] int NOT NULL IDENTITY,
        [ChannelPartnerId] int NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Message] nvarchar(4000) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [AdminReply] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [ResolvedAt] datetime2 NULL,
        CONSTRAINT [PK_PartnerTickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartnerTickets_ChannelPartners_ChannelPartnerId] FOREIGN KEY ([ChannelPartnerId]) REFERENCES [ChannelPartners] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_PartnerTickets_ChannelPartnerId] ON [PartnerTickets] ([ChannelPartnerId]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[PartnerNotifications]', N'U') IS NULL
BEGIN
    CREATE TABLE [PartnerNotifications] (
        [Id] int NOT NULL IDENTITY,
        [ChannelPartnerId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Url] nvarchar(300) NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PartnerNotifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartnerNotifications_ChannelPartners_ChannelPartnerId] FOREIGN KEY ([ChannelPartnerId]) REFERENCES [ChannelPartners] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_PartnerNotifications_ChannelPartnerId_IsRead_CreatedAt] ON [PartnerNotifications] ([ChannelPartnerId], [IsRead], [CreatedAt]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[MarketingKitItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [MarketingKitItems] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Category] nvarchar(40) NOT NULL,
        [FilePath] nvarchar(400) NOT NULL,
        [FileName] nvarchar(120) NULL,
        [ContentType] nvarchar(80) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_MarketingKitItems] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_MarketingKitItems_IsActive_SortOrder] ON [MarketingKitItems] ([IsActive], [SortOrder]);
END
");

            migrationBuilder.Sql(@"
UPDATE ChannelPartners
SET ReferralCode = CONCAT('SF', RIGHT('0000' + CAST(Id AS varchar(10)), 4), LEFT(REPLACE(NEWID(), '-', ''), 4))
WHERE ReferralCode IS NULL OR ReferralCode = '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[PartnerInvoices]', N'U') IS NOT NULL DROP TABLE [PartnerInvoices];
IF OBJECT_ID(N'[PartnerTickets]', N'U') IS NOT NULL DROP TABLE [PartnerTickets];
IF OBJECT_ID(N'[PartnerNotifications]', N'U') IS NOT NULL DROP TABLE [PartnerNotifications];
IF OBJECT_ID(N'[MarketingKitItems]', N'U') IS NOT NULL DROP TABLE [MarketingKitItems];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PartnerClients_ChannelPartnerId_Stage') DROP INDEX [IX_PartnerClients_ChannelPartnerId_Stage] ON [PartnerClients];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChannelPartners_ReferralCode') DROP INDEX [IX_ChannelPartners_ReferralCode] ON [ChannelPartners];
IF COL_LENGTH('PartnerClients', 'Stage') IS NOT NULL ALTER TABLE [PartnerClients] DROP COLUMN [Stage];
IF COL_LENGTH('ChannelPartners', 'ReferralCode') IS NOT NULL ALTER TABLE [ChannelPartners] DROP COLUMN [ReferralCode];
");
        }
    }
}
