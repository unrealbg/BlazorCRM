-- Extensions for FTS (optional for later search)
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Attachment ContentType (if not exists)
DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns WHERE table_name='"Attachments"' AND column_name='"ContentType"'
    ) THEN
        ALTER TABLE "Attachments" ADD COLUMN IF NOT EXISTS "ContentType" text NULL;
    END IF;
END $$;

-- RefreshTokens table
CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id" uuid PRIMARY KEY,
    "UserId" text NOT NULL,
    "TokenHash" text NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL DEFAULT false,
    "ReplacedByHash" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_refreshtokens_user_token ON "RefreshTokens"("UserId","TokenHash");
CREATE INDEX IF NOT EXISTS ix_refreshtokens_expires ON "RefreshTokens"("ExpiresAtUtc");
