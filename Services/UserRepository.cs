using Microsoft.Data.Sqlite;
using SafeVaultMicrosoftCourse.Models;

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
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                Role TEXT NOT NULL DEFAULT 'User'
            );
        """;
        
        command.ExecuteNonQuery();
    }

    public long CreateUser(string username, string email, string passwordHash, string role = "User")
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        command.CommandText = """
            INSERT INTO Users (Username, Email, PasswordHash, Role)
            VALUES ($username, $email, $passwordHash, $role);
            SELECT last_insert_rowid();
        """;
        
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$passwordHash", passwordHash);
        command.Parameters.AddWithValue("$role", role);
        
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

    public User? GetUserByUsername(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
                              SELECT UserID, Username, Email, PasswordHash, Role
                              FROM Users
                              WHERE Username = $username;
                              """;
        
        command.Parameters.AddWithValue("$username", username);

        using var reader = command.ExecuteReader();

        if (!reader.Read()) return null;
        
        return new User
        {
            UserID = reader.GetInt64(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            Role = reader.GetString(4)
        };
    }
}