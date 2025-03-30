using DoAnWeb.Models;
using DoAnWeb.Utils;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Extensions
{
    public static class DatabaseMigrationExtensions
    {
        /// <summary>
        /// Executes SQL scripts for database initialization
        /// </summary>
        public static WebApplication MigrateDatabase(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var services = scope.ServiceProvider;
                    var configuration = services.GetRequiredService<IConfiguration>();
                    var dbUtils = new DbUtils(configuration);
                    
                    // First execute the entity tables script
                    string entityScriptPath = Path.Combine(app.Environment.ContentRootPath, "entity-tables.sql");
                    if (File.Exists(entityScriptPath))
                    {
                        Console.WriteLine("Executing SQL script to create entity tables...");
                        Console.WriteLine($"Script path: {entityScriptPath}");
                        dbUtils.ExecuteSqlScript(entityScriptPath);
                        Console.WriteLine("Entity tables SQL script execution completed.");
                    }
                    else
                    {
                        Console.WriteLine($"Entity tables SQL script not found at path: {entityScriptPath}");
                    }
                    
                    // Then execute the chat tables script
                    string chatScriptPath = Path.Combine(app.Environment.ContentRootPath, "chat-tables.sql");
                    if (File.Exists(chatScriptPath))
                    {
                        Console.WriteLine("Executing SQL script to create chat tables...");
                        Console.WriteLine($"Script path: {chatScriptPath}");
                        dbUtils.ExecuteSqlScript(chatScriptPath);
                        Console.WriteLine("Chat tables SQL script execution completed.");
                    }
                    else
                    {
                        Console.WriteLine($"Chat tables SQL script not found at path: {chatScriptPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing SQL scripts: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue application startup even if script execution fails
                }
            }

            return app;
        }

        /// <summary>
        /// Updates database schema with necessary columns
        /// </summary>
        public static WebApplication UpdateDatabaseSchema(this WebApplication app)
        {
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DevCommunityContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    
                    // Update Comments table
                    UpdateCommentsTable(dbContext, logger);
                    
                    // Update Answers table
                    UpdateAnswersTable(dbContext, logger);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating database schema: {ex.Message}");
            }

            return app;
        }

        private static void UpdateCommentsTable(DevCommunityContext dbContext, ILogger logger)
        {
            // Check if ParentCommentId column exists
            bool columnExists = false;
            try
            {
                // Try to access the property to see if it exists
                var comment = dbContext.Comments.FirstOrDefault();
                if (comment != null)
                {
                    var parentId = comment.ParentCommentId;
                    columnExists = true;
                }
            }
            catch
            {
                columnExists = false;
            }
            
            if (!columnExists)
            {
                logger.LogInformation("Adding ParentCommentId column to Comments table...");
                
                try
                {
                    // Execute raw SQL to add the column
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND name = 'ParentCommentId')
                        BEGIN
                            ALTER TABLE [dbo].[Comments] ADD [ParentCommentId] INT NULL;
                            
                            -- Add foreign key constraint
                            ALTER TABLE [dbo].[Comments] ADD CONSTRAINT [FK_Comments_Comments_ParentCommentId] 
                                FOREIGN KEY ([ParentCommentId]) REFERENCES [dbo].[Comments] ([CommentId]) ON DELETE NO ACTION;
                            
                            -- Create index for better performance
                            CREATE INDEX [IX_Comments_ParentCommentId] ON [dbo].[Comments] ([ParentCommentId]);
                        END");
                    
                    logger.LogInformation("ParentCommentId column added successfully!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error adding ParentCommentId column: {Message}", ex.Message);
                }
            }
            else
            {
                logger.LogInformation("ParentCommentId column already exists in Comments table.");
            }

            // Check if EditedDate and IsEdited columns exist in Comments table
            bool editedDateExists = false;
            bool isEditedExists = false;
            
            try
            {
                // Use INFORMATION_SCHEMA to check if columns exist
                var columnCheckSql = @"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Comments' AND COLUMN_NAME = 'EditedDate'";
                
                using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = columnCheckSql;
                    dbContext.Database.OpenConnection();
                    var result = command.ExecuteScalar();
                    editedDateExists = Convert.ToInt32(result) > 0;
                }
                
                columnCheckSql = @"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Comments' AND COLUMN_NAME = 'IsEdited'";
                
                using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = columnCheckSql;
                    var result = command.ExecuteScalar();
                    isEditedExists = Convert.ToInt32(result) > 0;
                }
                
                logger.LogInformation($"Column check results - EditedDate exists: {editedDateExists}, IsEdited exists: {isEditedExists}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking if columns exist in Comments table");
            }
            
            // Add columns if they don't exist
            if (!editedDateExists)
            {
                try
                {
                    logger.LogInformation("Adding EditedDate column to Comments table...");
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                                      WHERE TABLE_NAME = 'Comments' AND COLUMN_NAME = 'EditedDate')
                        ALTER TABLE [dbo].[Comments] ADD [EditedDate] DATETIME2 NULL");
                    logger.LogInformation("EditedDate column added successfully!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error adding EditedDate column: {Message}", ex.Message);
                }
            }
            
            if (!isEditedExists)
            {
                try
                {
                    logger.LogInformation("Adding IsEdited column to Comments table...");
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                                      WHERE TABLE_NAME = 'Comments' AND COLUMN_NAME = 'IsEdited')
                        ALTER TABLE [dbo].[Comments] ADD [IsEdited] BIT NOT NULL DEFAULT 0");
                    logger.LogInformation("IsEdited column added successfully!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error adding IsEdited column: {Message}", ex.Message);
                }
            }
        }

        private static void UpdateAnswersTable(DevCommunityContext dbContext, ILogger logger)
        {
            // Check if ParentAnswerId column exists in Answers table
            bool parentAnswerIdExists = false;
            try
            {
                var columnCheckSql = @"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Answers' AND COLUMN_NAME = 'ParentAnswerId'";
                
                using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = columnCheckSql;
                    if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        dbContext.Database.OpenConnection();
                    }
                    var result = command.ExecuteScalar();
                    parentAnswerIdExists = Convert.ToInt32(result) > 0;
                }
                
                logger.LogInformation($"Column check result - ParentAnswerId exists in Answers table: {parentAnswerIdExists}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking if ParentAnswerId column exists in Answers table");
            }
            
            // Add ParentAnswerId column if it doesn't exist
            if (!parentAnswerIdExists)
            {
                try
                {
                    logger.LogInformation("Adding ParentAnswerId column to Answers table...");
                    dbContext.Database.ExecuteSqlRaw(@"
                        -- Add ParentAnswerId column to Answers table
                        ALTER TABLE [dbo].[Answers] ADD [ParentAnswerId] INT NULL;
                        
                        -- Add foreign key constraint
                        ALTER TABLE [dbo].[Answers] ADD CONSTRAINT [FK_Answers_Answers_ParentAnswerId] 
                            FOREIGN KEY ([ParentAnswerId]) REFERENCES [dbo].[Answers] ([AnswerId]) ON DELETE NO ACTION;
                        
                        -- Create index for better performance
                        CREATE INDEX [IX_Answers_ParentAnswerId] ON [dbo].[Answers] ([ParentAnswerId]);");
                    logger.LogInformation("ParentAnswerId column added successfully to Answers table!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error adding ParentAnswerId column to Answers table: {Message}", ex.Message);
                }
            }
        }
    }
} 