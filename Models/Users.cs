using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using System;
using System.Collections.Generic;

public class Users
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int Coins { get; set; } = 20;
    public string Bio { get; set; }
    public string Image { get; set; }
    public string Name { get; set; }

    public string Token => $"{Username}-msgToken";

    public Decks Deck { get; set; }
    public UserStack UserStack { get; set; }
    public UserStats UserStats { get; set; }
    public Trading Trading { get; set; }

    public Users()
    {
        Deck = new Decks();
        UserStack = new UserStack();
        UserStats = new UserStats();
        Trading = new Trading();
    }

    public bool IsValidForCreation()
    {
        return !string.IsNullOrEmpty(Username) &&
               !string.IsNullOrEmpty(Password) &&
               Username.Length <= 50 &&
               (Bio == null || Bio.Length <= 500) &&
               (Image == null || Image.Length <= 255);
    }

    public bool IsValidForUpdate()
    {
        // Überprüft, ob Bio und Image die erwarteten Längen einhalten
        return (Bio == null || Bio.Length <= 500) &&
               (Image == null || Image.Length <= 255);
    }

    public void SaveToDatabase(Database db)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@Username", Username },
            { "@Password", Password },
            { "@Coins", Coins },
            { "@Bio", Bio ?? string.Empty },
            { "@Image", Image ?? string.Empty }
        };

        db.ExecuteNonQuery(
            "INSERT INTO users (username, password, coins, bio, image) VALUES (@Username, @Password, @Coins, @Bio, @Image)",
            parameters
        );
    }

    public void UpdateProfile(Database db)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@UserId", UserId },
            { "@Bio", Bio ?? string.Empty },
            { "@Image", Image ?? string.Empty }
        };

        db.ExecuteNonQuery(
            "UPDATE users SET bio = @Bio, image = @Image WHERE userid = @UserId",
            parameters
        );
    }

    public static Users LoadFromDatabase(Database db, object identifier)
    {
        string query;
        var parameters = new Dictionary<string, object>();

        if (identifier is int userId)
        {
            query = "SELECT userid, username, password, coins, bio, image FROM users WHERE userid = @userid";
            parameters["@userid"] = userId;
        }
        else if (identifier is string username)
        {
            query = "SELECT userid, username, password, coins, bio, image FROM users WHERE username = @username";
            parameters["@username"] = username;
        }
        else
        {
            throw new ArgumentException("Identifier must be either a username (string) or a userId (int).");
        }

        var result = db.ExecuteQuery(query, parameters);

        if (result.Count == 0)
        {
            throw new KeyNotFoundException($"User not found with identifier '{identifier}'.");
        }

        var row = result[0];
        return new Users
        {
            UserId = Convert.ToInt32(row["userid"]),
            Username = row["username"].ToString(),
            Password = row["password"].ToString(),
            Coins = Convert.ToInt32(row["coins"]),
            Bio = row["bio"]?.ToString(),
            Image = row["image"]?.ToString()
        };
    }

    public bool DeductCoins(int amount)
    {
        if (Coins >= amount)
        {
            Coins -= amount;
            return true;
        }
        return false;
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
    }
}
