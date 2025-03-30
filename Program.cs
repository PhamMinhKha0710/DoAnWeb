using DoAnWeb;
using DoAnWeb.Hubs;
using DoAnWeb.Models;
using DoAnWeb.Repositories;
using DoAnWeb.Services;
using DoAnWeb.GitIntegration;
using DoAnWeb.Middleware;
using DoAnWeb.Filters;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using DoAnWeb.Utils;
using DoAnWeb.Extensions;
using DoAnWeb.Extensions.ServiceExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

// Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// Cấu hình logging chi tiết hơn
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddFilter("DoAnWeb.Controllers", LogLevel.Trace);
builder.Logging.AddFilter("DoAnWeb.GitIntegration", LogLevel.Trace);

// ✅ 1. Database and Repository Configuration
builder.Services
    .AddDatabaseServices(builder.Configuration)
    .AddRepositories();

// ✅ 2. Application and Business Services
builder.Services
    .AddApplicationServices()
    .AddGiteaServices();

// Add CheckGiteaUsers service
builder.Services.AddScoped<DoAnWeb.GitIntegration.CheckGiteaUsers>();

// ✅ 3. Infrastructure Services
builder.Services
    .AddCompressionServices()
    .AddCorsServices()
    .AddCachingServices()
    .AddSessionServices();

// ✅ 4. MVC and Authentication
builder.Services
    .AddMvcWithFilters()
    .AddAuthenticationServices(builder.Configuration)
    .AddSignalRServices();

// Build the application
var app = builder.Build();

// ✅ 5. Database Migrations and Schema Updates
app.MigrateDatabase()
   .UpdateDatabaseSchema();

// Fix Vote identity column configuration - ensure VoteId is an identity column
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DoAnWeb.Models.DevCommunityContext>();
    try
    {
        // Check if we need to update the VoteId column
        var connection = dbContext.Database.GetDbConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            -- First check if VoteId is already an identity column
            IF NOT EXISTS (
                SELECT 1 FROM sys.identity_columns 
                WHERE object_id = OBJECT_ID('Votes') 
                AND name = 'VoteId'
            )
            BEGIN
                -- Get the current primary key constraint name if it exists
                DECLARE @constraintName NVARCHAR(128)
                SELECT @constraintName = name 
                FROM sys.key_constraints 
                WHERE parent_object_id = OBJECT_ID('Votes') 
                AND type = 'PK'
                
                -- Drop the existing primary key constraint if it exists
                IF @constraintName IS NOT NULL
                BEGIN
                    DECLARE @dropSQL NVARCHAR(200)
                    SET @dropSQL = 'ALTER TABLE Votes DROP CONSTRAINT ' + @constraintName
                    EXEC(@dropSQL)
                END
                
                -- Create a temporary backup of the Votes table
                SELECT * INTO Votes_Temp FROM Votes
                
                -- Get the maximum VoteId to use for reseed later
                DECLARE @maxVoteId INT
                SELECT @maxVoteId = ISNULL(MAX(VoteId), 0) FROM Votes_Temp
                
                -- Drop the original Votes table
                DROP TABLE Votes
                
                -- Create a new Votes table with VoteId as IDENTITY column
                CREATE TABLE Votes (
                    VoteId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT,
                    TargetType NVARCHAR(20) NOT NULL,
                    TargetId INT NOT NULL,
                    AnswerId INT,
                    VoteValue INT NOT NULL,
                    IsUpvote BIT NOT NULL,
                    CreatedDate DATETIME
                )
                
                -- Reseed the identity to start after the max value 
                DBCC CHECKIDENT ('Votes', RESEED, @maxVoteId)
                
                -- Copy data from the temporary table back to the new Votes table
                SET IDENTITY_INSERT Votes ON
                INSERT INTO Votes (VoteId, UserId, TargetType, TargetId, AnswerId, VoteValue, IsUpvote, CreatedDate)
                SELECT VoteId, UserId, TargetType, TargetId, AnswerId, VoteValue, IsUpvote, CreatedDate
                FROM Votes_Temp
                SET IDENTITY_INSERT Votes OFF
                
                -- Drop the temporary table
                DROP TABLE Votes_Temp
                
                -- Add foreign key constraints back
                ALTER TABLE Votes ADD CONSTRAINT FK_Votes_Users 
                FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                
                ALTER TABLE Votes ADD CONSTRAINT FK_Votes_Answers 
                FOREIGN KEY (AnswerId) REFERENCES Answers(AnswerId) ON DELETE CASCADE
                
                PRINT 'Vote table successfully restructured with identity column'
            END
            ELSE
            BEGIN
                PRINT 'VoteId is already an identity column, no action needed'
            END";
        
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();
            
        command.ExecuteNonQuery();
        
        app.Logger.LogInformation("Vote table schema updated successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error updating Vote table schema");
    }
}

// ✅ 6. Configure Middleware Pipeline
app.ConfigureMiddleware();


app.UseStaticFiles();


// ✅ 7. Configure Endpoints
app.ConfigureEndpoints();

// Configure endpoints
app.MapControllerRoute(
    name: "user-profile",
    pattern: "User/Profile",
    defaults: new { controller = "Users", action = "Profile" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

    

// Add diagnostic endpoints (for admin only)
app.MapGet("/api/admin/check-gitea-users", async (
    DoAnWeb.GitIntegration.CheckGiteaUsers checker,
    HttpContext context) => 
{
    // Only allow admins
    if (!context.User.IsInRole("Admin"))
    {
        return Results.Forbid();
    }
    
    await checker.CheckUsers();
    return Results.Ok(new { message = "Check completed. See logs for details." });
})
.RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Admin" });

// Start the application
app.Run();
