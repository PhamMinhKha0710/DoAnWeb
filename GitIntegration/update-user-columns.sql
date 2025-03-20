-- Thêm các cột gitea_username và gitea_token vào bảng Users
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'gitea_username')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [gitea_username] NVARCHAR(50) NULL;
    
    PRINT 'Added gitea_username column to Users table';
END
ELSE
BEGIN
    PRINT 'gitea_username column already exists in Users table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'gitea_token')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [gitea_token] NVARCHAR(255) NULL;
    
    PRINT 'Added gitea_token column to Users table';
END
ELSE
BEGIN
    PRINT 'gitea_token column already exists in Users table';
END

-- Thêm cột last_login_date vào bảng Users
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'last_login_date')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [last_login_date] DATETIME NULL;
    
    PRINT 'Added last_login_date column to Users table';
END
ELSE
BEGIN
    PRINT 'last_login_date column already exists in Users table';
END 