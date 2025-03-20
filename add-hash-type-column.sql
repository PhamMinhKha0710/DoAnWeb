-- Add HashType column to Users table
ALTER TABLE Users ADD
    HashType NVARCHAR(50) NOT NULL DEFAULT 'SHA256'; 