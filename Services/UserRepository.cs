using Microsoft.Data.Sqlite;

namespace SafeVaultMicrosoftCourse.Services;

public class UserRepository
{
    private readonly string _connectionString;
    
    public UserRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("SafeVault") 
            ?? "Data Source=safevault.db";
    }
    
    // used by tests to provide an in-memory database connection string
    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Email TEXT NOT NULL
            );
        """;
        
        command.ExecuteNonQuery();
    }

    public long CreateUser(string username, string email)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        command.CommandText = """
            INSERT INTO Users (Username, Email)
            VALUES ($username, $email);
            SELECT last_insert_rowid();
        """;
        
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$email", email);
        
        return (long)command.ExecuteScalar()!;
    }
    
    public bool UserExists(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        command.CommandText = """
            SELECT COUNT(*)
            FROM Users
            WHERE Username = $username;
        """;
        
        command.Parameters.AddWithValue("$username", username);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}