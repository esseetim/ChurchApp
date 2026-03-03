-- Migration: Add RawTransactions Table
-- Created: 2026-03-03
-- Purpose: Support Gmail transaction extraction and automatic donation resolution

-- Create RawTransactions table
CREATE TABLE IF NOT EXISTS "RawTransactions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ProviderTransactionId" varchar(100) NOT NULL,
    "GmailMessageId" varchar(100),
    "Provider" integer NOT NULL,
    "SenderName" varchar(200) NOT NULL,
    "SenderHandle" varchar(100),
    "Amount" numeric(18,2) NOT NULL,
    "TransactionDate" date NOT NULL,
    "Memo" varchar(500),
    "RawContentJson" text NOT NULL,
    "Status" integer NOT NULL,
    "ResolvedDonationId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL
);

-- Create unique index on ProviderTransactionId for idempotency
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RawTransactions_ProviderTransactionId_Unique" 
    ON "RawTransactions" ("ProviderTransactionId");

-- Create index on Status for querying unmatched transactions
CREATE INDEX IF NOT EXISTS "IX_RawTransactions_Status" 
    ON "RawTransactions" ("Status");

-- Create composite index for provider and status queries
CREATE INDEX IF NOT EXISTS "IX_RawTransactions_Provider_Status" 
    ON "RawTransactions" ("Provider", "Status");

-- Add foreign key to Donations table
ALTER TABLE "RawTransactions"
    ADD CONSTRAINT "FK_RawTransactions_Donations_ResolvedDonationId"
    FOREIGN KEY ("ResolvedDonationId")
    REFERENCES "Donations" ("Id")
    ON DELETE SET NULL;

-- Create index on the FK for performance
CREATE INDEX IF NOT EXISTS "IX_RawTransactions_ResolvedDonationId"
    ON "RawTransactions" ("ResolvedDonationId");
