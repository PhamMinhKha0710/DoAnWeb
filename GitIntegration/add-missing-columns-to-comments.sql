-- Script để thêm các cột thiếu vào bảng Comments
-- Thêm cột Content nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND name = 'Content')
BEGIN
    ALTER TABLE [dbo].[Comments]
    ADD [Content] NVARCHAR(MAX) NULL;
    
    PRINT 'Added Content column to Comments table';
END
ELSE
BEGIN
    PRINT 'Content column already exists in Comments table';
END

-- Thêm cột UpdatedDate nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND name = 'UpdatedDate')
BEGIN
    ALTER TABLE [dbo].[Comments]
    ADD [UpdatedDate] DATETIME NULL;
    
    PRINT 'Added UpdatedDate column to Comments table';
END
ELSE
BEGIN
    PRINT 'UpdatedDate column already exists in Comments table';
END

-- Thêm cột IsApproved nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND name = 'IsApproved')
BEGIN
    ALTER TABLE [dbo].[Comments]
    ADD [IsApproved] BIT NOT NULL DEFAULT 1;
    
    PRINT 'Added IsApproved column to Comments table';
END
ELSE
BEGIN
    PRINT 'IsApproved column already exists in Comments table';
END

-- Thêm cột ParentCommentId nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND name = 'ParentCommentId')
BEGIN
    ALTER TABLE [dbo].[Comments]
    ADD [ParentCommentId] INT NULL;
    
    PRINT 'Added ParentCommentId column to Comments table';
    
    -- Thêm Foreign Key cho ParentCommentId
    ALTER TABLE [dbo].[Comments]
    ADD CONSTRAINT FK_Comments_ParentCommentId FOREIGN KEY (ParentCommentId)
    REFERENCES [dbo].[Comments] (CommentId);
    
    PRINT 'Added Foreign Key constraint for ParentCommentId';
END
ELSE
BEGIN
    PRINT 'ParentCommentId column already exists in Comments table';
END

-- Thêm cột PostId nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND name = 'PostId')
BEGIN
    ALTER TABLE [dbo].[Comments]
    ADD [PostId] INT NULL;
    
    PRINT 'Added PostId column to Comments table';
    
    -- Thêm Foreign Key cho PostId (giả sử bạn có bảng Posts với khóa chính là PostId)
    IF OBJECT_ID(N'[dbo].[Posts]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Comments]
        ADD CONSTRAINT FK_Comments_PostId FOREIGN KEY (PostId)
        REFERENCES [dbo].[Posts] (PostId);
        
        PRINT 'Added Foreign Key constraint for PostId';
    END
END
ELSE
BEGIN
    PRINT 'PostId column already exists in Comments table';
END 