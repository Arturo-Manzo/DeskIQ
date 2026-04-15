-- Migration: Add SSO fields to Users table
-- Date: 2026-04-12
-- Description: Add ExtId and DepartmentPendingAssign fields for SSO integration

-- Add ExtId column (nullable for existing users)
ALTER TABLE "Users" 
ADD COLUMN "ExtId" character varying(255);

-- Add DepartmentPendingAssign column with default false
ALTER TABLE "Users" 
ADD COLUMN "DepartmentPendingAssign" boolean NOT NULL DEFAULT false;

-- Create unique index on ExtId for SSO provider lookup
CREATE UNIQUE INDEX "IX_Users_ExtId" ON "Users" ("ExtId") WHERE "ExtId" IS NOT NULL;

-- Comment on new columns
COMMENT ON COLUMN "Users"."ExtId" IS 'External ID from SSO provider (e.g., Azure AD, Okta)';
COMMENT ON COLUMN "Users"."DepartmentPendingAssign" IS 'Flag indicating user from SSO without department assignment';
