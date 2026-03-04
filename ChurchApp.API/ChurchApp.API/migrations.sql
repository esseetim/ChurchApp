CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "Families" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        CONSTRAINT "PK_Families" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "Members" (
        "Id" uuid NOT NULL,
        "FirstName" character varying(150) NOT NULL,
        "LastName" character varying(150) NOT NULL,
        "Email" character varying(320),
        "PhoneNumber" character varying(50),
        CONSTRAINT "PK_Members" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "DonationAccounts" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "Method" integer NOT NULL,
        "Handle" character varying(320) NOT NULL,
        "DisplayName" character varying(200),
        "IsActive" boolean NOT NULL,
        CONSTRAINT "PK_DonationAccounts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DonationAccounts_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "FamilyMembers" (
        "FamilyId" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        CONSTRAINT "PK_FamilyMembers" PRIMARY KEY ("FamilyId", "MemberId"),
        CONSTRAINT "FK_FamilyMembers_Families_FamilyId" FOREIGN KEY ("FamilyId") REFERENCES "Families" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_FamilyMembers_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "Reports" (
        "Id" uuid NOT NULL,
        "Type" integer NOT NULL,
        "GeneratedAtUtc" timestamp with time zone NOT NULL,
        "ServiceName" character varying(200),
        "StartDate" date,
        "EndDate" date,
        "MemberId" uuid,
        "FamilyId" uuid,
        "ParametersJson" text NOT NULL,
        "OutputJson" text NOT NULL,
        CONSTRAINT "PK_Reports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Reports_Families_FamilyId" FOREIGN KEY ("FamilyId") REFERENCES "Families" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Reports_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "Summaries" (
        "Id" uuid NOT NULL,
        "Type" integer NOT NULL,
        "PeriodType" integer NOT NULL,
        "StartDate" date NOT NULL,
        "EndDate" date NOT NULL,
        "ServiceName" character varying(200),
        "MemberId" uuid,
        "FamilyId" uuid,
        "TotalAmount" numeric(18,2) NOT NULL,
        "DonationCount" integer NOT NULL,
        "BreakdownJson" text NOT NULL,
        "GeneratedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Summaries" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Summaries_TotalAmount_NotNegative" CHECK ("TotalAmount" >= 0),
        CONSTRAINT "FK_Summaries_Families_FamilyId" FOREIGN KEY ("FamilyId") REFERENCES "Families" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Summaries_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "Donations" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "DonationAccountId" uuid,
        "Type" integer NOT NULL,
        "Method" integer NOT NULL,
        "DonationDate" date NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL DEFAULT 1,
        "IdempotencyKey" character varying(100),
        "ServiceName" character varying(200),
        "Notes" character varying(1000),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(120) NOT NULL,
        "VoidedAtUtc" timestamp with time zone,
        "VoidedBy" character varying(120),
        "VoidReason" character varying(500),
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Donations" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Donations_Amount_NotZero" CHECK ("Amount" <> 0),
        CONSTRAINT "FK_Donations_DonationAccounts_DonationAccountId" FOREIGN KEY ("DonationAccountId") REFERENCES "DonationAccounts" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Donations_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE TABLE "DonationAudits" (
        "Id" uuid NOT NULL,
        "DonationId" uuid NOT NULL,
        "Action" integer NOT NULL,
        "OccurredAtUtc" timestamp with time zone NOT NULL,
        "PerformedBy" character varying(120) NOT NULL,
        "Reason" character varying(500),
        "SnapshotJson" text NOT NULL,
        CONSTRAINT "PK_DonationAudits" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DonationAudits_Donations_DonationId" FOREIGN KEY ("DonationId") REFERENCES "Donations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_DonationAccounts_MemberId" ON "DonationAccounts" ("MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE UNIQUE INDEX "IX_DonationAccounts_Method_Handle" ON "DonationAccounts" ("Method", "Handle");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_DonationAudits_DonationId_OccurredAtUtc" ON "DonationAudits" ("DonationId", "OccurredAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Donations_DonationAccountId" ON "Donations" ("DonationAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Donations_DonationDate_Type" ON "Donations" ("DonationDate", "Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE UNIQUE INDEX "IX_Donations_IdempotencyKey" ON "Donations" ("IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Donations_MemberId" ON "Donations" ("MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_FamilyMembers_MemberId" ON "FamilyMembers" ("MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Reports_FamilyId" ON "Reports" ("FamilyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Reports_MemberId" ON "Reports" ("MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Reports_Type_GeneratedAtUtc" ON "Reports" ("Type", "GeneratedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Summaries_FamilyId" ON "Summaries" ("FamilyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Summaries_MemberId" ON "Summaries" ("MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    CREATE INDEX "IX_Summaries_Type_PeriodType_StartDate_EndDate_MemberId_Family~" ON "Summaries" ("Type", "PeriodType", "StartDate", "EndDate", "MemberId", "FamilyId", "ServiceName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303041017_InitialPostgresBaseline') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260303041017_InitialPostgresBaseline', '10.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303061249_AddFinancialObligations') THEN
    ALTER TABLE "Donations" ADD "ObligationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303061249_AddFinancialObligations') THEN
    CREATE TABLE "FinancialObligations" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Title" character varying(200) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "StartDate" date NOT NULL,
        "DueDate" date NOT NULL,
        "Status" integer NOT NULL,
        CONSTRAINT "PK_FinancialObligations" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_FinancialObligations_TotalAmount_Positive" CHECK ("TotalAmount" > 0),
        CONSTRAINT "FK_FinancialObligations_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303061249_AddFinancialObligations') THEN
    CREATE INDEX "IX_Donations_ObligationId" ON "Donations" ("ObligationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303061249_AddFinancialObligations') THEN
    CREATE INDEX "IX_FinancialObligations_DueDate" ON "FinancialObligations" ("DueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303061249_AddFinancialObligations') THEN
    CREATE INDEX "IX_FinancialObligations_MemberId_Status" ON "FinancialObligations" ("MemberId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303061249_AddFinancialObligations') THEN
    ALTER TABLE "Donations" ADD CONSTRAINT "FK_Donations_FinancialObligations_ObligationId" FOREIGN KEY ("ObligationId") REFERENCES "FinancialObligations" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303061249_AddFinancialObligations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260303061249_AddFinancialObligations', '10.0.1');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303195726_AddedFinancailObligation') THEN
    CREATE TABLE "RawTransactions" (
        "Id" uuid NOT NULL,
        "ProviderTransactionId" character varying(100) NOT NULL,
        "GmailMessageId" character varying(100),
        "Provider" integer NOT NULL,
        "SenderName" character varying(200) NOT NULL,
        "SenderHandle" character varying(100),
        "Amount" numeric(18,2) NOT NULL,
        "TransactionDate" date NOT NULL,
        "Memo" character varying(500),
        "RawContentJson" text NOT NULL,
        "Status" integer NOT NULL,
        "ResolvedDonationId" uuid,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RawTransactions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RawTransactions_Donations_ResolvedDonationId" FOREIGN KEY ("ResolvedDonationId") REFERENCES "Donations" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303195726_AddedFinancailObligation') THEN
    CREATE UNIQUE INDEX "IX_RawTransactions_ProviderTransactionId_Unique" ON "RawTransactions" ("ProviderTransactionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303195726_AddedFinancailObligation') THEN
    CREATE INDEX "IX_RawTransactions_Provider_Status" ON "RawTransactions" ("Provider", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303195726_AddedFinancailObligation') THEN
    CREATE INDEX "IX_RawTransactions_ResolvedDonationId" ON "RawTransactions" ("ResolvedDonationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303195726_AddedFinancailObligation') THEN
    CREATE INDEX "IX_RawTransactions_Status" ON "RawTransactions" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260303195726_AddedFinancailObligation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260303195726_AddedFinancailObligation', '10.0.1');
    END IF;
END $EF$;
COMMIT;

