-- Initialize database for DeskIQ Ticket System
-- This script runs when PostgreSQL container starts

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create indexes for better performance
-- These will be created by EF Core migrations, but we can add them here for initial setup

-- Set default permissions
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO postgres;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO postgres;

-- Create view for department management (admin only)
-- This view provides department statistics and summary information
CREATE OR REPLACE VIEW vw_DepartmentManagement AS
SELECT 
    d.Id,
    d.Name,
    d.Code,
    d.Description,
    d.AutoAssignRules,
    d.IsActive,
    d.CreatedAt,
    d.UpdatedAt,
    COUNT(DISTINCT u.Id) FILTER (WHERE u.IsActive = true) AS ActiveUsersCount,
    COUNT(DISTINCT u.Id) FILTER (WHERE u.IsActive = false) AS InactiveUsersCount,
    COUNT(DISTINCT t.Id) FILTER (WHERE t.Status = 0) AS OpenTicketsCount,
    COUNT(DISTINCT t.Id) FILTER (WHERE t.Status = 1) AS InProgressTicketsCount,
    COUNT(DISTINCT t.Id) FILTER (WHERE t.Status = 2) AS ClosedTicketsCount,
    COUNT(DISTINCT t.Id) AS TotalTicketsCount,
    COUNT(DISTINCT ea.Id) AS EmailAccountsCount,
    COUNT(DISTINCT wc.Id) AS WhatsAppConfigsCount
FROM "Departments" d
LEFT JOIN "Users" u ON d.Id = u."DepartmentId"
LEFT JOIN "Tickets" t ON d.Id = t."DepartmentId"
LEFT JOIN "EmailAccounts" ea ON d.Id = ea."DepartmentId"
LEFT JOIN "WhatsAppConfigs" wc ON d.Id = wc."DepartmentId"
GROUP BY d.Id, d.Name, d.Code, d.Description, d.AutoAssignRules, d.IsActive, d.CreatedAt, d.UpdatedAt;

-- Grant permissions on the view
GRANT SELECT ON vw_DepartmentManagement TO postgres;

-- Create default department if it doesn't exist
INSERT INTO "Departments" ("Id", "Name", "Code", "Description", "AutoAssignRules", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 
    '00000000-0000-0000-0000-000000000001'::uuid,
    'IT Support',
    'TI',
    'Technical support department',
    '{"roundRobin": true}',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Departments" WHERE "Name" = 'IT Support'
);
