-- Add security columns to Users table
ALTER TABLE Users ADD
    IsEmailVerified BIT NOT NULL DEFAULT 0,
    VerificationToken NVARCHAR(128) NULL,
    VerificationTokenExpiry DATETIME NULL,
    IsLocked BIT NOT NULL DEFAULT 0,
    FailedLoginAttempts INT NULL DEFAULT 0,
    LockoutEnd DATETIME NULL,
    LastPasswordChangeDate DATETIME NULL,
    PasswordResetToken NVARCHAR(128) NULL,
    PasswordResetTokenExpiry DATETIME NULL; 