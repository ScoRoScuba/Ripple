CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE events (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(255)    NOT NULL,
    description     TEXT            NOT NULL DEFAULT '',
    venue           VARCHAR(255)    NOT NULL,
    date            DATE            NOT NULL,
    time            TIME            NOT NULL,
    total_capacity  INT             NOT NULL CHECK (total_capacity > 0),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE TABLE pricing_tiers (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id        UUID            NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    name            VARCHAR(255)    NOT NULL,
    price           NUMERIC(10, 2)  NOT NULL CHECK (price >= 0),
    capacity        INT             NOT NULL CHECK (capacity > 0),
    tickets_sold    INT             NOT NULL DEFAULT 0 CHECK (tickets_sold >= 0),
    CONSTRAINT chk_tickets_le_capacity CHECK (tickets_sold <= capacity)
);

CREATE TABLE tickets (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id        UUID            NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    pricing_tier_id UUID            NOT NULL REFERENCES pricing_tiers(id) ON DELETE CASCADE,
    purchaser_name  VARCHAR(255)    NOT NULL,
    purchaser_email VARCHAR(255)    NOT NULL,
    quantity        INT             NOT NULL CHECK (quantity > 0),
    unit_price      NUMERIC(10, 2)  NOT NULL,
    purchased_at    TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tickets_event_id        ON tickets(event_id);
CREATE INDEX idx_pricing_tiers_event_id  ON pricing_tiers(event_id);
