-- Generate ticket IDs for existing tickets using a subquery
UPDATE "Tickets" t1
SET "TicketId" = 'TK-' || LPAD(
  (SELECT COUNT(*)::text FROM "Tickets" t2 WHERE t2."CreatedAt" <= t1."CreatedAt"),
  4, '0'
)
WHERE "TicketId" IS NULL OR "TicketId" = '';

-- Make the column required after populating it
ALTER TABLE "Tickets" ALTER COLUMN "TicketId" SET NOT NULL;
