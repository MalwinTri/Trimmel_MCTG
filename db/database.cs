using Npgsql;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.Execute;

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
                userid SERIAL PRIMARY KEY,
                username VARCHAR(50) UNIQUE NOT NULL,
                password TEXT NOT NULL,
                coins INT DEFAULT 20 NOT NULL,
                token UUID NOT NULL DEFAULT gen_random_uuid(),
                bio TEXT DEFAULT NULL,
                name VARCHAR(100) DEFAULT NULL,
                image TEXT DEFAULT NULL                
            );

            CREATE TABLE IF NOT EXISTS cards (
                card_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                name VARCHAR(100) NOT NULL,
                damage INT NOT NULL,
                element_type VARCHAR(50) NOT NULL,
                card_type VARCHAR(50) NOT NULL CHECK (card_type IN ('spell', 'monster'))
            );

            CREATE TABLE IF NOT EXISTS userstats (
                userid INT PRIMARY KEY REFERENCES users(userid) ON DELETE CASCADE,
                wins INT DEFAULT 0,
                losses INT DEFAULT 0,
                elo INT DEFAULT 1000
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
                userid INT REFERENCES users(userid) ON DELETE CASCADE,
                card_1_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
                card_2_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
                card_3_id UUID REFERENCES cards(card_id) ON DELETE CASCADE,
                card_4_id UUID REFERENCES cards(card_id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS battles (
                battle_id SERIAL PRIMARY KEY,
                user_1_id INT REFERENCES users(userid) ON DELETE CASCADE,
                user_2_id INT REFERENCES users(userid) ON DELETE CASCADE,
                winner_id INT REFERENCES users(userid)
            );

            CREATE TABLE IF NOT EXISTS trading (
                tradingid UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                userid INT NOT NULL REFERENCES users(userid) ON DELETE CASCADE,
                offered_card_id UUID NOT NULL REFERENCES cards(card_id) ON DELETE CASCADE,
                required_type card_type_enum NOT NULL,
                min_damage INT DEFAULT 0 CHECK (min_damage >= 0)
            );

            CREATE TABLE IF NOT EXISTS scoreboard (
                id SERIAL PRIMARY KEY,
                userid INTEGER NOT NULL,
                wins INTEGER DEFAULT 0,
                losses INTEGER DEFAULT 0,
                elo INTEGER DEFAULT 1000,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT fk_scoreboard_user FOREIGN KEY (userid) REFERENCES users(userid) ON DELETE CASCADE,
                CONSTRAINT fk_scoreboard_userstats FOREIGN KEY (userid) REFERENCES userstats(userid) ON DELETE CASCADE
            );";

            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(createTablesCommand, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    Console.WriteLine("Tables created successfully or already exist.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error creating tables: {ex.Message}");
                }
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

        public bool IsUserInDatabase(Users user)
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


        public bool CreateUser(Users user)
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

                // Generiere einen benutzerdefinierten Token
                string userToken = $"{user.Username}-mtcgToken";

                // SQL-Befehl zum Einfügen eines neuen Benutzers in die Datenbank
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



        public bool Logging(Users user)
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

        public bool CheckAndRegister(Users user)
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

        public bool GenerateAndStoreToken(Users user)
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

        public void InsertCard(Cards card)
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


        public void InsertCardWithoutId(Cards card)
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



        public List<Cards> GetCardsByPackageId(int packageId)
        {
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM cards c
                INNER JOIN packageCards pc ON c.card_id = pc.card_id
                WHERE pc.package_id = @packageId;";

            var cards = new List<Cards>();
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
                        cards.Add(new Cards(
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
                INSERT INTO user_stacks (userid, card_id) 
                SELECT userid, @cardId FROM users WHERE username = @username;";
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

        public List<Cards> GetBest4Cards(string username)
        {
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM user_stacks us
                JOIN cards c ON us.card_id = c.card_id
                JOIN users u ON us.userid = u.userid
                WHERE u.username = @username
                ORDER BY c.damage DESC
                LIMIT 4;";
            var bestCards = new List<Cards>();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bestCards.Add(new Cards(
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
                INSERT INTO decks (userid, card_1_id, card_2_id, card_3_id, card_4_id)
                VALUES (
                    (SELECT userid FROM users WHERE username = @username),
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
                WHERE userid = (SELECT userid FROM users WHERE username = @username);";

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

        public List<Cards> GetCardsByUsername(string username)
        {
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM cards c
                JOIN user_stacks us ON c.card_id = us.card_id
                JOIN users u ON us.userid = u.userid
                WHERE u.username = @username;";

            List<Cards> cards = new List<Cards>();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cards.Add(new Cards(
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

        public List<Cards> ShowUnconfiguredDeck(string username)
        {
            var deck = new List<Cards>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                    SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                    FROM decks d
                    JOIN users u ON d.userid = u.userid
                    JOIN cards c ON c.card_id = ANY(ARRAY[d.card_1_id, d.card_2_id, d.card_3_id, d.card_4_id])
                    WHERE u.username = @username;";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            deck.Add(new Cards(
                                reader.GetGuid(0),
                                reader.GetString(1),
                                reader.GetDouble(2),
                                reader.GetString(3),
                                reader.GetString(4)
                            ));
                        }
                    }
                }
            }

            return deck;
        }

        public void ConfigureDeck(string username, List<string> cardIds)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Überprüfen, ob die Karten dem Benutzer gehören
                var command = new NpgsqlCommand("SELECT COUNT(*) FROM user_stacks WHERE userid = (SELECT userid FROM users WHERE username = @username) AND card_id = ANY(@cardIds)", connection);
                command.Parameters.AddWithValue("username", username);
                command.Parameters.AddWithValue("cardIds", cardIds.ToArray());

                int count = (int)command.ExecuteScalar();

                if (count != 4)
                {
                    throw new Exception("Not all specified cards belong to the user.");
                }

                // Bestehendes Deck löschen
                var deleteCommand = new NpgsqlCommand("DELETE FROM decks WHERE userid = (SELECT userid FROM users WHERE username = @username)", connection);
                deleteCommand.Parameters.AddWithValue("username", username);
                deleteCommand.ExecuteNonQuery();

                // Neues Deck einfügen
                var insertCommand = new NpgsqlCommand("INSERT INTO decks (userid, card_1_id, card_2_id, card_3_id, card_4_id) VALUES ((SELECT userid FROM users WHERE username = @username), @card1, @card2, @card3, @card4)", connection);
                insertCommand.Parameters.AddWithValue("username", username);
                insertCommand.Parameters.AddWithValue("card1", cardIds[0]);
                insertCommand.Parameters.AddWithValue("card2", cardIds[1]);
                insertCommand.Parameters.AddWithValue("card3", cardIds[2]);
                insertCommand.Parameters.AddWithValue("card4", cardIds[3]);
                insertCommand.ExecuteNonQuery();
            }
        }


        public bool DoesUserOwnCard(string username, Guid cardId)
        {
            string query = @"
            SELECT COUNT(*) 
            FROM user_stacks us
            JOIN users u ON us.userid = u.userid
            WHERE u.username = @username 
              AND us.card_id = @cardId;";  // card_id = UUID, @cardId = STRING

                var parameters = new Dictionary<string, object>
        {
            { "@username", username },
            { "@cardId", cardId }
        };

            return GetSingleValue<int>(query, parameters) > 0;
        }


        public void UpdateDeck(string username, string card1Id, string card2Id, string card3Id, string card4Id)
        {
            string deleteQuery = "DELETE FROM decks WHERE userid = (SELECT userid FROM users WHERE username = @username);";
            ExecuteNonQuery(deleteQuery, new Dictionary<string, object> { { "@username", username } });

            string insertQuery = "INSERT INTO decks (userid, card_1_id, card_2_id, card_3_id, card_4_id) " +
                                 "VALUES ((SELECT userid FROM users WHERE username = @username), @card1, @card2, @card3, @card4);";
            var parameters = new Dictionary<string, object>
            {
                { "@username", username },
                { "@card1", Guid.Parse(card1Id) }, // UUID umwandeln
                { "@card2", Guid.Parse(card2Id) }, // UUID umwandeln
                { "@card3", Guid.Parse(card3Id) }, // UUID umwandeln
                { "@card4", Guid.Parse(card4Id) }  // UUID umwandeln
            };
                    ExecuteNonQuery(insertQuery, parameters);
        }

        public List<Cards> GetConfiguredDeck(string username)
        {
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM decks d
                JOIN cards c ON c.card_id = ANY (ARRAY[d.card_1_id, d.card_2_id, d.card_3_id, d.card_4_id])
                WHERE d.userid = (SELECT userid FROM users WHERE username = @username);";

                    var parameters = new Dictionary<string, object>
            {
                { "@username", username }
            };

            var result = ExecuteQuery(query, parameters);
            return result.Select(row => new Cards(
                Guid.Parse(row["card_id"].ToString()),  // card_id sollte ein Guid sein
                row["name"].ToString(),                // name als String
                double.Parse(row["damage"].ToString()), // damage als double
                row["element_type"].ToString(),         // element_type als String
                row["card_type"].ToString()             // card_type als String
            )).ToList();
        }

        public T? GetSingleValue<T>(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Parameter hinzufügen, falls vorhanden
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        // Einzelwert ausführen
                        var result = command.ExecuteScalar();

                        // Ergebnis in den gewünschten Typ konvertieren
                        if (result == null || result == DBNull.Value)
                        {
                            return default;
                        }

                        return (T)Convert.ChangeType(result, typeof(T));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing scalar query: {ex.Message}");
                throw;
            }
        }

        public int ExecuteNonQuery(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Parameter hinzufügen, falls vorhanden
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        // SQL-Befehl ausführen und die Anzahl der betroffenen Zeilen zurückgeben
                        return command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing non-query: {ex.Message}");
                throw;
            }
        }


        public List<Dictionary<string, object>> ExecuteQuery(string query, Dictionary<string, object> parameters)
        {
            var results = new List<Dictionary<string, object>>();
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }
            return results;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public UserData? GetUserData(string username)
        {
            var query = @"SELECT userid, name, bio, image 
                  FROM users 
                  WHERE username = @username;";
            var parameters = new Dictionary<string, object> { { "@username", username } };

            var result = ExecuteQuery(query, parameters).FirstOrDefault();
            if (result == null) return null;

            // Now you have "userid" in `result` too
            // (but be aware "userid" is an int column in your table)
            // If you have a property "UserId" in UserData, set it:
            return new UserData
            {
                UserId = Convert.ToInt32(result["userid"]),  // if you have an int
                Name = result["name"].ToString(),
                Bio = result["bio"].ToString(),
                Image = result["image"].ToString()
            };
        }

        public void UpdateUserData(Database db, string currentUsername, string newName, string bio, string image)
        {
            // Prüfen, ob der neue Benutzername existiert
            if (!string.IsNullOrEmpty(newName) && newName != currentUsername)
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@newUsername", newName }
                };

                var existingUserCheck = db.ExecuteQuery("SELECT username FROM Users WHERE username = @newUsername", parameters);
                if (existingUserCheck.Count > 0)
                {
                    throw new Exception($"The username '{newName}' is already taken.");
                }
            }

            // Parameter für das Update vorbereiten
            var updateParams = new Dictionary<string, object>
            {
                { "@bio", bio ?? string.Empty },
                { "@image", image ?? string.Empty },
                { "@currentUsername", currentUsername }
            };

            // Grundlegendes Update ohne Benutzername
            string updateQuery = "UPDATE Users SET Bio = @bio, Image = @image WHERE Username = @currentUsername";

            // Benutzername wird aktualisiert, wenn ein neuer Name angegeben wurde
            if (!string.IsNullOrEmpty(newName) && newName != currentUsername)
            {
                updateQuery = "UPDATE Users SET Bio = @bio, Image = @image, Username = @newUsername WHERE Username = @currentUsername";
                updateParams["@newUsername"] = newName;
            }

            // Update ausführen
            db.ExecuteNonQuery(updateQuery, updateParams);
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void UpdateUser(Database db, string username, string newName, string bio, string image)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                // Prüfe, ob der neue Benutzername bereits existiert
                var parameters = new Dictionary<string, object>
                {
                    { "@NewName", newName }
                };
                        var result = db.ExecuteQuery("SELECT username FROM Users WHERE username = @NewName", parameters);

                if (result.Count > 0)
                {
                    throw new Exception("Username already exists.");
                }
            }

            // Führe das Update durch
            var updateParams = new Dictionary<string, object>
            {
                { "@Bio", bio ?? string.Empty },
                { "@Image", image ?? string.Empty },
                { "@Username", username }
            };
            string query = "UPDATE Users SET Bio = @Bio, Image = @Image WHERE Username = @Username";

            if (!string.IsNullOrEmpty(newName))
            {
                query = "UPDATE Users SET Bio = @Bio, Image = @Image, Username = @NewName WHERE Username = @Username";
                updateParams["@NewName"] = newName;
            }

            db.ExecuteNonQuery(query, updateParams);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public int? GetUserIdFromToken(string token)
        {
            try
            {
                return GetSingleValue<int>(
                    "SELECT userid FROM users WHERE token = @token",
                    new Dictionary<string, object> { { "@token", token } }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving UserId from token: {ex.Message}");
                return null;
            }
        }



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
