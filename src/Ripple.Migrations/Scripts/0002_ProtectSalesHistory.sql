-- Ticket purchases are permanent records. Previously both foreign keys cascaded, so deleting a
-- pricing tier or an event silently deleted the tickets sold for it. RESTRICT makes the database
-- refuse any delete that would remove sales history.

ALTER TABLE tickets DROP CONSTRAINT tickets_pricing_tier_id_fkey;
ALTER TABLE tickets ADD CONSTRAINT tickets_pricing_tier_id_fkey
    FOREIGN KEY (pricing_tier_id) REFERENCES pricing_tiers(id) ON DELETE RESTRICT;

ALTER TABLE tickets DROP CONSTRAINT tickets_event_id_fkey;
ALTER TABLE tickets ADD CONSTRAINT tickets_event_id_fkey
    FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE RESTRICT;
