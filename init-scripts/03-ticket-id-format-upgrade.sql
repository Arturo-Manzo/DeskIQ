-- Upgrade script for department code and ticket ID sequence format support

ALTER TABLE "Departments"
ADD COLUMN IF NOT EXISTS "Code" VARCHAR(4);

-- Seed missing codes for existing departments from the first alphanumeric chars in the name.
WITH candidate_codes AS (
    SELECT
        d."Id",
        UPPER(LEFT(REGEXP_REPLACE(d."Name", '[^A-Za-z0-9]', '', 'g'), 4)) AS base_code
    FROM "Departments" d
)
UPDATE "Departments" d
SET "Code" = CASE
    WHEN LENGTH(c.base_code) >= 2 THEN c.base_code
    WHEN LENGTH(c.base_code) = 1 THEN c.base_code || 'X'
    ELSE 'GE'
END
FROM candidate_codes c
WHERE d."Id" = c."Id"
  AND (d."Code" IS NULL OR d."Code" = '');

-- Resolve duplicate codes by adding a numeric suffix where possible.
WITH ranked AS (
    SELECT
        d."Id",
        d."Code",
        ROW_NUMBER() OVER (PARTITION BY d."Code" ORDER BY d."CreatedAt", d."Id") AS rn
    FROM "Departments" d
)
UPDATE "Departments" d
SET "Code" = CASE
    WHEN r.rn = 1 THEN d."Code"
    WHEN LENGTH(d."Code") = 4 THEN LEFT(d."Code", 3) || (r.rn - 1)::text
    ELSE LEFT(d."Code", 3) || (r.rn - 1)::text
END
FROM ranked r
WHERE d."Id" = r."Id";

-- Enforce code requirements.
UPDATE "Departments"
SET "Code" = UPPER("Code")
WHERE "Code" IS NOT NULL;

ALTER TABLE "Departments"
ALTER COLUMN "Code" SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public'
          AND indexname = 'IX_Departments_Code'
    ) THEN
        CREATE UNIQUE INDEX "IX_Departments_Code" ON "Departments" ("Code");
    END IF;
END $$;

-- Create sequence table used to generate [CODE]-[YY]-[NNNNNN] ids.
CREATE TABLE IF NOT EXISTS "TicketSequences" (
    "Id" uuid NOT NULL,
    "DepartmentId" uuid NOT NULL,
    "Year" integer NOT NULL,
    "LastValue" integer NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_TicketSequences" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TicketSequences_Departments_DepartmentId"
        FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public'
          AND indexname = 'IX_TicketSequences_DepartmentId_Year'
    ) THEN
        CREATE UNIQUE INDEX "IX_TicketSequences_DepartmentId_Year"
            ON "TicketSequences" ("DepartmentId", "Year");
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public'
          AND indexname = 'IX_Tickets_TicketId'
    ) THEN
        CREATE UNIQUE INDEX "IX_Tickets_TicketId" ON "Tickets" ("TicketId");
    END IF;
END $$;

-- Initialize current-year sequence state using existing tickets by department.
INSERT INTO "TicketSequences" ("Id", "DepartmentId", "Year", "LastValue", "UpdatedAt")
SELECT
    uuid_generate_v4(),
    t."DepartmentId",
    EXTRACT(YEAR FROM t."CreatedAt")::int AS "Year",
    COUNT(*)::int AS "LastValue",
    NOW()
FROM "Tickets" t
GROUP BY t."DepartmentId", EXTRACT(YEAR FROM t."CreatedAt")::int
ON CONFLICT ("DepartmentId", "Year") DO UPDATE
SET "LastValue" = EXCLUDED."LastValue",
    "UpdatedAt" = NOW();
