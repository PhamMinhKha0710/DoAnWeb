-- Check if ExternalLogins table exists, if not create it
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExternalLogins]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ExternalLogins](
        [ExternalLoginId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [Provider] [nvarchar](50) NOT NULL,
        [ProviderKey] [nvarchar](128) NOT NULL,
        [ProviderDisplayName] [nvarchar](100) NOT NULL,
        [CreatedDate] [datetime] NOT NULL DEFAULT (getdate()),
        CONSTRAINT [PK__External__77860B2A12345678] PRIMARY KEY CLUSTERED 
        (
            [ExternalLoginId] ASC
        )
    )

    -- Add foreign key constraint
    ALTER TABLE [dbo].[ExternalLogins] WITH CHECK ADD CONSTRAINT [FK__ExternalL__UserI__12345678]
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId])
    ON DELETE CASCADE

    PRINT 'ExternalLogins table created successfully.'
END
ELSE
BEGIN
    PRINT 'ExternalLogins table already exists.'
END

-- Add unique constraint to prevent duplicate external logins for the same provider
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_ExternalLogins_Provider_ProviderKey' AND object_id = OBJECT_ID('[dbo].[ExternalLogins]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_ExternalLogins_Provider_ProviderKey]
    ON [dbo].[ExternalLogins] ([Provider], [ProviderKey])

    PRINT 'Unique index for Provider and ProviderKey added.'
END
ELSE
BEGIN
    PRINT 'Unique index for Provider and ProviderKey already exists.'
END 