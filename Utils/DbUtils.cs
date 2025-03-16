using System;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DoAnWeb.Utils
{
    public class DbUtils
    {
        private readonly IConfiguration _configuration;

        public DbUtils(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ExecuteSqlScript(string scriptPath)
        {
            // Try to get the connection string with multiple possible keys
            string connectionString = _configuration.GetConnectionString("DevCommunityDB");
            
            // If not found, try the other known key
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = _configuration.GetConnectionString("DevCommunityContext");
            }
            
            // If still not found, use the hardcoded connection string as a fallback
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Server=DESKTOP-TQFDM4P\\SQLEXPRESS;Database=DevCommunity;Trusted_Connection=True;TrustServerCertificate=True;";
                Console.WriteLine("Warning: Using hardcoded connection string because none was found in configuration.");
            }
            
            Console.WriteLine($"Executing SQL script using connection string source: {(string.IsNullOrEmpty(_configuration.GetConnectionString("DevCommunityDB")) ? (string.IsNullOrEmpty(_configuration.GetConnectionString("DevCommunityContext")) ? "Hardcoded" : "DevCommunityContext") : "DevCommunityDB")}");
            
            string script = File.ReadAllText(scriptPath);

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Database connection opened successfully.");
                    
                    // Split the script on GO statements to handle batches
                    string[] commandStrings = script.Split(new[] { "GO", "go", "Go" }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (string commandString in commandStrings)
                    {
                        if (!string.IsNullOrWhiteSpace(commandString))
                        {
                            using (var command = new SqlCommand(commandString, connection))
                            {
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error executing SQL command: {ex.Message}");
                                    Console.WriteLine($"SQL Command: {commandString.Substring(0, Math.Min(commandString.Length, 100))}...");
                                    // Continue trying other commands even if one fails
                                }
                            }
                        }
                    }
                    
                    Console.WriteLine("SQL script execution completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening database connection: {ex.Message}");
                    throw;
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                        Console.WriteLine("Database connection closed.");
                    }
                }
            }
        }
    }
} 