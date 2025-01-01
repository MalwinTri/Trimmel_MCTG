using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;

public class Users
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int Coins { get; set; } = 20;
    public string Bio { get; set; }
    public string Image { get; set; }
    public string Name { get; set; }

    // Read-only token logic (if needed)
    public string Token => $"{Username}-msgToken";

    // Optional relationships
    public Decks Deck { get; set; }
    public UserStack UserStack { get; set; }
    public UserStats UserStats { get; set; }
    public Trade Trade { get; set; }

    public Users()
    {
        Deck = new Decks();
        UserStack = new UserStack();
        UserStats = new UserStats();
        Trade = new Trade();
    }

    // For creation
    public bool IsValidForCreation()
    {
        return !string.IsNullOrEmpty(Username) &&
               !string.IsNullOrEmpty(Password) &&
               Username.Length <= 50 &&
               (Bio == null || Bio.Length <= 500) &&
               (Image == null || Image.Length <= 255);
    }

    // For partial updates
    public bool IsValidForUpdate()
    {
        return (Bio == null || Bio.Length <= 500) &&
               (Image == null || Image.Length <= 255);
    }

    public void SaveToDatabase(Database db)
    {
        // This might be "Create" method. For partial updates, consider a separate method.
        var parameters = new Dictionary<string, object>
        {
            { "@Username", Username },
            { "@Password", Password },
            { "@Coins", Coins },
            { "@Bio", Bio ?? string.Empty },
            { "@Image", Image ?? string.Empty }
            // Possibly { "@Name", Name ?? string.Empty }
        };
        db.ExecuteNonQuery(
            "INSERT INTO Users (Username, Password, Coins, Bio, Image) " +
            "VALUES (@Username, @Password, @Coins, @Bio, @Image)",
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

        // Optionally update "Name" if relevant:
        // parameters["@Name"] = Name ?? string.Empty;
        // SET Name = @Name, ...

        db.ExecuteNonQuery(
            "UPDATE Users SET Bio = @Bio, Image = @Image WHERE userid = @UserId",
            parameters
        );
    }

    public static Users? LoadFromDatabase(Database db, string username)
    {
        var parameters = new Dictionary<string, object> { { "@username", username } };
        var result = db.ExecuteQuery(
            "SELECT userid, username, password, coins, bio, image FROM Users WHERE username = @username",
            parameters
        );

        if (result.Count == 0)
        {
            // or return null if you don't want to throw
            throw new KeyNotFoundException($"User with username '{username}' not found.");
        }

        var row = result[0];
        var user = new Users
        {
            UserId = Convert.ToInt32(row["userid"] ?? 0),
            Username = row["username"]?.ToString() ?? "",
            Password = row["password"]?.ToString() ?? "",
            Coins = Convert.ToInt32(row["coins"] ?? 0),
            Bio = row["bio"]?.ToString(),
            Image = row["image"]?.ToString()
        };
        return user;
    }

    public static Users LoadFromDatabase(Database db, int userId)
    {
        var parameters = new Dictionary<string, object> { { "@userid", userId } };
        var result = db.ExecuteQuery("SELECT userid, username, password, coins, bio, image FROM users WHERE userid = @userid", parameters);

        if (result.Count == 0)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' not found.");
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
}
