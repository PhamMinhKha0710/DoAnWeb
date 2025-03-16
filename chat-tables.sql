-- Create Conversations table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Conversations')
BEGIN
    CREATE TABLE [Conversations] (
        [ConversationId] INT NOT NULL IDENTITY(1,1),
        [Title] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
        [LastActivityAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([ConversationId])
    );
    PRINT 'Created Conversations table';
END
ELSE
BEGIN
    PRINT 'Conversations table already exists';
END

-- Create ConversationParticipants table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConversationParticipants')
BEGIN
    CREATE TABLE [ConversationParticipants] (
        [ParticipantId] INT NOT NULL IDENTITY(1,1),
        [ConversationId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [IsArchived] BIT NOT NULL DEFAULT (0),
        [IsMuted] BIT NOT NULL DEFAULT (0),
        CONSTRAINT [PK_ConversationParticipants] PRIMARY KEY ([ParticipantId]),
        CONSTRAINT [FK_ConversationParticipants_Conversations] FOREIGN KEY ([ConversationId]) 
            REFERENCES [Conversations] ([ConversationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ConversationParticipants_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
    PRINT 'Created ConversationParticipants table';
END
ELSE
BEGIN
    PRINT 'ConversationParticipants table already exists';
END

-- Create Messages table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
BEGIN
    CREATE TABLE [Messages] (
        [MessageId] INT NOT NULL IDENTITY(1,1),
        [ConversationId] INT NOT NULL,
        [SenderId] INT NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [SentAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
        [IsRead] BIT NOT NULL DEFAULT (0),
        [ReadAt] DATETIME2 NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([MessageId]),
        CONSTRAINT [FK_Messages_Conversations] FOREIGN KEY ([ConversationId]) 
            REFERENCES [Conversations] ([ConversationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Messages_Users] FOREIGN KEY ([SenderId]) 
            REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
    PRINT 'Created Messages table';
END
ELSE
BEGIN
    PRINT 'Messages table already exists';
END

-- Add ConnectionId column to Users table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ConnectionId' AND object_id = OBJECT_ID('Users'))
BEGIN
    ALTER TABLE [Users] ADD [ConnectionId] NVARCHAR(MAX) NULL;
    PRINT 'Added ConnectionId column to Users table';
END
ELSE
BEGIN
    PRINT 'ConnectionId column already exists in Users table';
END

-- Add ProfilePicture column to Users table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ProfilePicture' AND object_id = OBJECT_ID('Users'))
BEGIN
    ALTER TABLE [Users] ADD [ProfilePicture] NVARCHAR(MAX) NULL;
    PRINT 'Added ProfilePicture column to Users table';
END
ELSE
BEGIN
    PRINT 'ProfilePicture column already exists in Users table';
END 