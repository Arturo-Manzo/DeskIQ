-- Role Refinement Migration
-- This script migrates from the old 3-role system to the new 7-role system
-- Old roles: Agent (1), Supervisor (2), Admin (3)
-- New roles: Cliente (1), ClienteSupervisor (2), Operador (3), OperadorSupervisor (4), SupervisorGeneral (5), Auditor (6), Administrador (7)

-- Step 1: Create UserDepartment table for multi-department support
CREATE TABLE IF NOT EXISTS "UserDepartments" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "DepartmentId" UUID NOT NULL,
    "AssignedAt" TIMESTAMP NOT NULL,
    "AssignedByUserId" UUID,
    CONSTRAINT "FK_UserDepartments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserDepartments_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserDepartments_Users_AssignedByUserId" FOREIGN KEY ("AssignedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

-- Create index for UserDepartments
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserDepartments_UserId_DepartmentId" ON "UserDepartments" ("UserId", "DepartmentId");

-- Step 2: Create AuditLog table for tracking role changes and administrative actions
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ActionType" VARCHAR(100) NOT NULL,
    "EntityName" VARCHAR(100) NOT NULL,
    "EntityId" UUID,
    "OldValue" VARCHAR(2000),
    "NewValue" VARCHAR(2000),
    "Description" VARCHAR(500) NOT NULL,
    "PerformedByUserId" UUID NOT NULL,
    "PerformedAt" TIMESTAMP NOT NULL,
    "IpAddress" VARCHAR(45),
    CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AuditLogs_Users_PerformedByUserId" FOREIGN KEY ("PerformedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

-- Create indexes for AuditLog
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_PerformedAt" ON "AuditLogs" ("PerformedAt");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityName_EntityId" ON "AuditLogs" ("EntityName", "EntityId");

-- Step 3: Migrate existing users from old roles to new roles
-- Agent (1) -> Operador (3)
-- Supervisor (2) -> OperadorSupervisor (4)
-- Admin (3) -> Administrador (7)

-- Update Agent users to Operador
UPDATE "Users" SET "Role" = 3 WHERE "Role" = 1;

-- Update Supervisor users to OperadorSupervisor
UPDATE "Users" SET "Role" = 4 WHERE "Role" = 2;

-- Update Admin users to Administrador
UPDATE "Users" SET "Role" = 7 WHERE "Role" = 3;

-- Step 4: Update the seed admin user if it exists
-- The seed admin was created with the old Admin role, now it should be Administrador (7)
-- This is already handled by the update above, but we ensure it explicitly
UPDATE "Users" SET "Role" = 7 WHERE "Email" = 'admin@deskiq.com';

-- Step 5: Add a note about the migration
-- This migration should be run after the application is updated with the new enum values
-- The application code now expects the new role values:
-- Cliente = 1
-- ClienteSupervisor = 2
-- Operador = 3
-- OperadorSupervisor = 4
-- SupervisorGeneral = 5
-- Auditor = 6
-- Administrador = 7
