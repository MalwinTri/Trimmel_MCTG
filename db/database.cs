using Npgsql;
using Trimmel_MCTG.DB;

namespace Trimmel_MCTG.db
{
    public class Database : IDisposable
    {
        private readonly NpgsqlConnection conn;
        // Connection String 
        private readonly string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=a4gq4heU2cF*;Database=mctg_trimmel";

        public Database()
        {
            try
            {
                conn = new NpgsqlConnection(connectionString);
                conn.Open();
                Console.WriteLine("Database connection established successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening database connection: {ex.Message}");
                throw;
            }
        }

        public LoginExecuter LoginExecuter
        {
            get => default;
            set
            {
            }
        }

        public RegisterExecuter RegisterExecuter
        {
            get => default;
            set
            {
            }
        }


        public void CreateTables()
        {
            string createTablesCommand = @"
        CREATE TABLE IF NOT EXISTS users (
            user_id SERIAL PRIMARY KEY,
            username VARCHAR(50) NOT NULL UNIQUE,
            password VARCHAR(255) NOT NULL,
            coins INT DEFAULT 20,
            token VARCHAR(255) UNIQUE
        );

        CREATE TABLE IF NOT EXISTS cards (
            card_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            name VARCHAR(100) NOT NULL,
            damage INT NOT NULL,
            element_type VARCHAR(50) NOT NULL,
            card_type VARCHAR(50) NOT NULL CHECK (card_type IN ('spell', 'monster'))
        );

        CREATE TABLE IF NOT EXISTS user_stacks (
            user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
            card_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
            in_deck BOOLEAN DEFAULT FALSE,
            PRIMARY KEY (user_id, card_id)
        );

        CREATE TABLE IF NOT EXISTS packages (
            package_id SERIAL PRIMARY KEY,
            price INT DEFAULT 5
        );

        CREATE TABLE IF NOT EXISTS packageCards (
            package_id INT REFERENCES packages(package_id) ON DELETE CASCADE,
            card_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
            PRIMARY KEY (package_id, card_id)
        );

        CREATE TABLE IF NOT EXISTS decks (
            deck_id SERIAL PRIMARY KEY,
            user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
            card_1_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
            card_2_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
            card_3_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
            card_4_id UUID REFERENCES cards(card_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS battles (
            battle_id SERIAL PRIMARY KEY,
            user_1_id INT REFERENCES users(user_id) ON DELETE CASCADE,
            user_2_id INT REFERENCES users(user_id) ON DELETE CASCADE,
            winner_id INT REFERENCES users(user_id)
        );

        CREATE TABLE IF NOT EXISTS trades (
            trade_id SERIAL PRIMARY KEY,
            user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
            offered_card_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
            required_type VARCHAR(50) NOT NULL CHECK (required_type IN ('spell', 'monster')),
            min_damage INT
        );";

            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(createTablesCommand, conn))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Tables created successfully or already exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating tables: {ex.Message}");
            }
        }


        // Funktioniert eigentlich nicht. Erst am Ende wenn alles implementiert ist
        public void DropTables()
        {
            string dropTablesCommand = @"
                DROP TABLE IF EXISTS trades;
                DROP TABLE IF EXISTS battles;
                DROP TABLE IF EXISTS decks;
                DROP TABLE IF EXISTS packageCards;
                DROP TABLE IF EXISTS packages;
                DROP TABLE IF EXISTS user_stacks;
                DROP TABLE IF EXISTS cards;
                DROP TABLE IF EXISTS user_stats;
                DROP TABLE IF EXISTS users;";

            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(dropTablesCommand, conn))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("All tables dropped successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error dropping tables: {ex.Message}");
            }
        }

        public bool IsUserInDatabase(User user)
        {
            try
            {
                // SQL-Befehl vor, um zu überprüfen, ob der Benutzer bereits in der Datenbank existiert
                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT username FROM users WHERE username = @username", conn))
                {
                    // Füge den Benutzernamen als Parameter hinzu
                    cmd.Parameters.AddWithValue("username", user.Username);

                    // Führe den SQL-Befehl aus und verwende einen Reader, um die Ergebnisse zu überprüfen
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Wenn der Reader Zeilen hat, existiert der Benutzer bereits
                        return reader.HasRows;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if user exists: {ex.Message}");
                return false;
            }
        }


        public bool CreateUser(User user)
        {
            try
            {
                // Überprüfe, ob der Benutzer bereits in der Datenbank vorhanden ist
                if (IsUserInDatabase(user))
                {
                    Console.WriteLine("User already exists.");
                    return false; // Beende die Methode und gib false zurück, wenn der Benutzer bereits existiert
                }

                // Hash das Passwort des Benutzers für die sichere Speicherung
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
                // Generiere ein Token für den Benutzer
                string userToken = Guid.NewGuid().ToString();

                // SQL-Befehl zum Einfügen eines neuen Benutzers in die Datenbank vor
                using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO users (username, password, coins, token) VALUES (@username, @password, @coins, @token);", conn))
                {
                    // Füge die Parameter für den SQL-Befehl hinzu
                    cmd.Parameters.AddWithValue("username", user.Username);
                    cmd.Parameters.AddWithValue("password", hashedPassword);
                    cmd.Parameters.AddWithValue("coins", 20);
                    cmd.Parameters.AddWithValue("token", userToken);

                    // Führe den SQL-Befehl aus und speichere die Anzahl der betroffenen Zeilen
                    int rowsAffected = cmd.ExecuteNonQuery();

                    // Wenn mindestens eine Zeile betroffen ist, gib true zurück, ansonsten false
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user creation: {ex.Message}");
            }
            return false;
        }


        public bool Logging(User user)
        {
            // Überprüfung, ob die Benutzerinformationen vollständig sind
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                Console.WriteLine("Invalid user information.");
                return false;
            }

            try
            {
                // Benutzer in der Datenbank suchen
                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT password FROM users WHERE username = @username;", conn))
                {
                    cmd.Parameters.AddWithValue("username", user.Username);
                    string? storedPasswordHash = null;

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            storedPasswordHash = reader.GetString(0);
                        }
                    }

                    // Überprüfung, ob ein Passwort gefunden wurde und es mit dem übergebenen Passwort übereinstimmt
                    if (!string.IsNullOrEmpty(storedPasswordHash) && BCrypt.Net.BCrypt.Verify(user.Password, storedPasswordHash))
                    {
                        // Token generieren und speichern
                        return GenerateAndStoreToken(user);
                    }
                    else
                    {
                        Console.WriteLine($"Login failed for user: {user.Username}");
                        return false; // Passwort falsch oder Benutzer existiert nicht
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user login: {ex.Message}");
                return false; // Fehlerbehandlung
            }
        }

        public bool CheckAndRegister(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                Console.WriteLine("Invalid user information.");
                return false;
            }

            if (IsUserInDatabase(user))
            {
                Console.WriteLine($"User {user.Username} already exists.");
                return false;
            }

            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO users (username, password, coins) VALUES (@username, @password, @coins);", conn))
                {
                    cmd.Parameters.AddWithValue("username", user.Username);

                    // Hash the password for security
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    cmd.Parameters.AddWithValue("password", hashedPassword);

                    // Set default coins value from user object
                    cmd.Parameters.AddWithValue("coins", user.Coins);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"Failed to register user {user.Username}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error occurred while registering user {user.Username}: {ex.Message}");
            }

            return false;
        }

        public bool GenerateAndStoreToken(User user)
        {
            // Token Generieren - Randomly generate a secure token using Guid
            string token = Guid.NewGuid().ToString();

            try
            {
                using (var cmd = new NpgsqlCommand("UPDATE users SET token = @token WHERE username = @username", conn))
                {
                    cmd.Parameters.AddWithValue("username", user.Username);
                    cmd.Parameters.AddWithValue("token", token);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating and storing token for user {user.Username}: {ex.Message}");
                return false;
            }
        }

        public int InsertPackage()
        {
            string query = "INSERT INTO packages (price) VALUES (5) RETURNING package_id;";
            try
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    return (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting package: {ex.Message}");
                throw;
            }
        }

        public void InsertCard(Card card)
        {
            // Prüfen, ob die Karte schon existiert
            string checkQuery = "SELECT COUNT(*) FROM cards WHERE card_id = @cardId;";
            using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("cardId", card.CardId);
                var count = (long)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    // Statt Exception werfen -> einfach überspringen
                    Console.WriteLine($"[InsertCard] Card with ID {card.CardId} already exists. Skipping insert...");
                    return;
                }
            }

            // Karte noch nicht in DB -> jetzt einfügen
            string query = @"
                INSERT INTO cards (card_id, name, damage, element_type, card_type)
                VALUES (@cardId, @name, @damage, @elementType, @cardType);";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("cardId", card.CardId);
                cmd.Parameters.AddWithValue("name", card.Name);
                cmd.Parameters.AddWithValue("damage", card.Damage);
                cmd.Parameters.AddWithValue("elementType", card.ElementType);
                cmd.Parameters.AddWithValue("cardType", card.CardType);

                cmd.ExecuteNonQuery();
            }
        }


        public void InsertCardWithoutId(Card card)
        {
            string query = @"
                INSERT INTO cards (name, damage, element_type, card_type)
                VALUES (@name, @damage, @elementType, @cardType);";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("name", card.Name);
                cmd.Parameters.AddWithValue("damage", card.Damage);
                cmd.Parameters.AddWithValue("elementType", card.ElementType);
                cmd.Parameters.AddWithValue("cardType", card.CardType);

                cmd.ExecuteNonQuery();
            }
        }


        public void LinkCardToPackage(int packageId, Guid cardId)
        {
            // 1) Prüfen, ob Karte existiert
            string checkCardQuery = "SELECT COUNT(*) FROM cards WHERE card_id = @cardId;";
            using (var checkCmd = new NpgsqlCommand(checkCardQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("cardId", cardId);
                var count = (long)checkCmd.ExecuteScalar();
                if (count == 0)
                {
                    Console.WriteLine($"Card with ID {cardId} does not exist in 'cards'.");
                    return;
                }
            }

            // 2) Prüfen, ob (package_id, card_id) schon in packageCards vorhanden
            string checkPackageCardsQuery = "SELECT COUNT(*) FROM packageCards WHERE package_id = @packageId AND card_id = @cardId;";
            using (var checkCmd = new NpgsqlCommand(checkPackageCardsQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("packageId", packageId);
                checkCmd.Parameters.AddWithValue("cardId", cardId);
                var count = (long)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    // Already linked -> optional: skip or error out
                    Console.WriteLine($"(package_id, card_id) = ({packageId}, {cardId}) already exists. Skipping link...");
                    return;
                }
            }

            // 3) Verknüpfung einfügen
            string query = @"
                INSERT INTO packageCards (package_id, card_id)
                VALUES (@packageId, @cardId);";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("packageId", packageId);
                cmd.Parameters.AddWithValue("cardId", cardId);
                cmd.ExecuteNonQuery();
            }
        }



        public List<Card> GetCardsByPackageId(int packageId)
        {
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM cards c
                INNER JOIN packageCards pc ON c.card_id = pc.card_id
                WHERE pc.package_id = @packageId;";

            var cards = new List<Card>();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("packageId", packageId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine($"No cards found for package ID {packageId}");
                        return cards;
                    }

                    while (reader.Read())
                    {
                        cards.Add(new Card(
                            reader.GetGuid(0),
                            reader.GetString(1),
                            reader.GetDouble(2),
                            reader.GetString(3),
                            reader.GetString(4)
                        ));
                    }
                }
            }
            return cards;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public int GetUserCoins(string username)
        {
            string query = "SELECT coins FROM users WHERE username = @username;";
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                return (int)cmd.ExecuteScalar();
            }
        }

        public Package? GetNextAvailablePackage()
        {
            string query = "SELECT package_id, price FROM packages ORDER BY package_id LIMIT 1;";
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int packageId = reader.GetInt32(0);
                        int price = reader.GetInt32(1);
                        return new Package(packageId, price); // Geben Sie die Parameter an
                    }
                }
            }
            return null;
        }

        public void AssignCardToUser(string username, Guid cardId)
        {
            string query = @"
                INSERT INTO user_stacks (user_id, card_id) 
                SELECT user_id, @cardId FROM users WHERE username = @username;";
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("cardId", cardId);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateUserCoins(string username, int newCoins)
        {
            string query = "UPDATE users SET coins = @newCoins WHERE username = @username;";
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("newCoins", newCoins);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeletePackage(int packageId)
        {
            string query = "DELETE FROM packages WHERE package_id = @packageId;";
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("packageId", packageId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Card> GetBest4Cards(string username)
        {
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM user_stacks us
                JOIN cards c ON us.card_id = c.card_id
                JOIN users u ON us.user_id = u.user_id
                WHERE u.username = @username
                ORDER BY c.damage DESC
                LIMIT 4;";
            var bestCards = new List<Card>();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bestCards.Add(new Card(
                            reader.GetGuid(0),
                            reader.GetString(1),
                            reader.GetDouble(2),  // Falls Damage ein `double` ist
                            reader.GetString(3),
                            reader.GetString(4)
                        ));
                    }
                }
            }
            return bestCards;
        }


        public void InsertIntoDeck(Guid card1Id, Guid card2Id, Guid card3Id, Guid card4Id, string username)
        {
            string query = @"
                INSERT INTO decks (user_id, card_1_id, card_2_id, card_3_id, card_4_id)
                VALUES (
                    (SELECT user_id FROM users WHERE username = @username),
                    @card1Id, @card2Id, @card3Id, @card4Id);";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("card1Id", card1Id);
                cmd.Parameters.AddWithValue("card2Id", card2Id);
                cmd.Parameters.AddWithValue("card3Id", card3Id);
                cmd.Parameters.AddWithValue("card4Id", card4Id);

                cmd.ExecuteNonQuery();
            }
        }



        public void DeleteDeckByUser(string username)
        {
            string query = @"
                DELETE FROM decks
                WHERE user_id = (SELECT user_id FROM users WHERE username = @username);";

            try
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("username", username);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine($"{rowsAffected} deck(s) deleted for user {username}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting deck for user {username}: {ex.Message}");
                throw;
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            try
            {
                DropTables();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during table drop in Dispose: {ex.Message}");
            }
            finally
            {
                conn?.Close();
                conn?.Dispose();
                Console.WriteLine("Database connection closed and disposed.");
            }
        }
    }
}
