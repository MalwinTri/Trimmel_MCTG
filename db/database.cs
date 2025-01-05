using Newtonsoft.Json;
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

        public void DropTables()
        {
            string dropTablesCommand = @"
                DROP TABLE IF EXISTS trading;
                DROP TABLE IF EXISTS battles;
                DROP TABLE IF EXISTS decks;
                DROP TABLE IF EXISTS packageCards;
                DROP TABLE IF EXISTS packages;
                DROP TABLE IF EXISTS userstats;
                DROP TABLE IF EXISTS cards;
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
                // SQL-Befehl vorbereiten, um zu überprüfen, ob der Benutzer bereits in der Datenbank existiert
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

                //// Generiere einen benutzerdefinierten Token
                //string userToken = $"{user.Username}-mtcgToken";

                // SQL-Befehl zum Einfügen eines neuen Benutzers in die Datenbank
                using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO users (username, password, coins, token) VALUES (@username, @password, @coins, @token);", conn))
                {
                    // Füge die Parameter für den SQL-Befehl hinzu
                    cmd.Parameters.AddWithValue("username", user.Username);
                    cmd.Parameters.AddWithValue("password", hashedPassword);
                    cmd.Parameters.AddWithValue("coins", 20);
                    //cmd.Parameters.AddWithValue("token", userToken);

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

        public string? Logging(Users user)
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                Console.WriteLine("Invalid user information.");
                return null;
            }

            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT password, token FROM users WHERE username = @username;", conn))
                {
                    cmd.Parameters.AddWithValue("username", user.Username);

                    string? storedPasswordHash = null;
                    string? token = null;

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            storedPasswordHash = reader.GetString(0); // Passwort-Hash
                            token = reader.GetString(1); // Vorhandener Token
                        }
                    }

                    if (!string.IsNullOrEmpty(storedPasswordHash) && BCrypt.Net.BCrypt.Verify(user.Password, storedPasswordHash))
                    {
                        // Benutzeranmeldung erfolgreich, gib den bestehenden Token zurück
                        Console.WriteLine($"Login successful for user: {user.Username}");
                        return token;
                    }
                    else
                    {
                        Console.WriteLine($"Login failed for user: {user.Username}");
                        return null; // Passwort falsch oder Benutzer existiert nicht
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user login: {ex.Message}");
                return null; // Fehlerbehandlung
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
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    cmd.Parameters.AddWithValue("password", hashedPassword);
                    cmd.Parameters.AddWithValue("coins", user.Coins);

                    cmd.ExecuteNonQuery();
                }

                // Eintrag in `userstats` hinzufügen
                int userId = GetSingleValue<int>("SELECT userid FROM users WHERE username = @username;", new Dictionary<string, object> { { "@username", user.Username } });

                using (NpgsqlCommand statsCmd = new NpgsqlCommand("INSERT INTO userstats (userid) VALUES (@userid);", conn))
                {
                    statsCmd.Parameters.AddWithValue("@userid", userId);
                    statsCmd.ExecuteNonQuery();
                }

                // Eintrag in `scoreboard` hinzufügen
                using (NpgsqlCommand scoreboardCmd = new NpgsqlCommand("INSERT INTO scoreboard (userid, wins, losses, elo) VALUES (@userid, 0, 0, 1000);", conn))
                {
                    scoreboardCmd.Parameters.AddWithValue("@userid", userId);
                    scoreboardCmd.ExecuteNonQuery();
                }

                Console.WriteLine($"User {user.Username} registered successfully with initial scoreboard entry.");
                return true;
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
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 1. Holen Sie die User-ID
                    string userIdQuery = "SELECT userid FROM users WHERE username = @username;";
                    int userId = GetSingleValue<int>(userIdQuery, new Dictionary<string, object> { { "@username", username } });

                    if (userId == 0)
                    {
                        throw new Exception("User not found.");
                    }

                    // 2. Lösche das bestehende Deck für den Benutzer
                    string deleteDeckQuery = "DELETE FROM decks WHERE userid = @userId;";
                    ExecuteNonQuery(deleteDeckQuery, new Dictionary<string, object> { { "@userId", userId } });

                    // 3. Füge das neue Deck ein
                    string insertDeckQuery = @"
                        INSERT INTO decks (userid, card_1_id, card_2_id, card_3_id, card_4_id)
                        VALUES (@userId, @card1Id, @card2Id, @card3Id, @card4Id);";
                    ExecuteNonQuery(insertDeckQuery, new Dictionary<string, object>
                    {
                        { "@userId", userId },
                        { "@card1Id", card1Id },
                        { "@card2Id", card2Id },
                        { "@card3Id", card3Id },
                        { "@card4Id", card4Id }
                    });

                    // 4. Setze den Status `in_deck` in `user_stacks`
                    // Setze alle Karten von diesem Benutzer auf FALSE
                    string resetInDeckQuery = "UPDATE user_stacks SET in_deck = FALSE WHERE userid = @userId;";
                    ExecuteNonQuery(resetInDeckQuery, new Dictionary<string, object> { { "@userId", userId } });

                    // Setze die ausgewählten Karten auf TRUE
                    string updateInDeckQuery = @"
                        UPDATE user_stacks
                        SET in_deck = TRUE
                        WHERE userid = @userId AND card_id IN (@card1Id, @card2Id, @card3Id, @card4Id);";
                    ExecuteNonQuery(updateInDeckQuery, new Dictionary<string, object>
                    {
                        { "@userId", userId },
                        { "@card1Id", card1Id },
                        { "@card2Id", card2Id },
                        { "@card3Id", card3Id },
                        { "@card4Id", card4Id }
                    });

                    // 5. Transaktion abschließen
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Fehlerbehandlung und Rollback der Transaktion
                    transaction.Rollback();
                    Console.WriteLine($"Error inserting deck: {ex.Message}");
                    throw;
                }
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
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM user_stacks us
                JOIN cards c ON us.card_id = c.card_id
                JOIN users u ON us.userid = u.userid
                WHERE u.username = @username AND us.in_deck = FALSE;";
            var parameters = new Dictionary<string, object> { { "@username", username } };
            return ExecuteQuery(query, parameters)
                .Select(row => new Cards(
                    Guid.Parse(row["card_id"].ToString()),
                    row["name"].ToString(),
                    double.Parse(row["damage"].ToString()),
                    row["element_type"].ToString(),
                    row["card_type"].ToString()))
                .ToList();
        }


        public void ConfigureDeck(string username, List<string> cardIds)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                Console.WriteLine($"Validating cards for user: {username}");

                // Kartentypen validieren
                var validateCardsCommand = new NpgsqlCommand(@"
                    SELECT c.card_id, c.card_type
                    FROM user_stacks us
                    JOIN cards c ON us.card_id = c.card_id
                    WHERE us.userid = (SELECT userid FROM users WHERE username = @username)
                      AND c.card_id = ANY(@cardIds::uuid[]);", connection);

                validateCardsCommand.Parameters.AddWithValue("username", username);
                validateCardsCommand.Parameters.AddWithValue("cardIds", cardIds.ToArray());

                var cardTypes = new Dictionary<string, int> { { "monster", 0 }, { "spell", 0 } };

                using (var reader = validateCardsCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var cardId = reader["card_id"].ToString();
                        var cardType = reader["card_type"].ToString();

                        Console.WriteLine($"Card ID: {cardId}, Type: {cardType}");

                        if (cardTypes.ContainsKey(cardType))
                        {
                            cardTypes[cardType]++;
                        }
                    }
                }

                Console.WriteLine($"Card type counts: Monsters = {cardTypes["monster"]}, Spells = {cardTypes["spell"]}");

                // Einschränkungen prüfen
                if (cardTypes["monster"] != 2 || cardTypes["spell"] != 2)
                {
                    throw new Exception("You must configure exactly 2 monsters and 2 spells in your deck.");
                }

                // Bestehendes Deck löschen
                var deleteDeckCommand = new NpgsqlCommand(@"
                    DELETE FROM decks
                    WHERE userid = (SELECT userid FROM users WHERE username = @username);", connection);
                deleteDeckCommand.Parameters.AddWithValue("username", username);
                deleteDeckCommand.ExecuteNonQuery();

                // Neues Deck einfügen
                var insertDeckCommand = new NpgsqlCommand(@"
                    INSERT INTO decks (userid, card_1_id, card_2_id, card_3_id, card_4_id)
                    VALUES (
                        (SELECT userid FROM users WHERE username = @username),
                        @card1::uuid, @card2::uuid, @card3::uuid, @card4::uuid);", connection);

                insertDeckCommand.Parameters.AddWithValue("username", username);
                insertDeckCommand.Parameters.AddWithValue("card1", Guid.Parse(cardIds[0]));
                insertDeckCommand.Parameters.AddWithValue("card2", Guid.Parse(cardIds[1]));
                insertDeckCommand.Parameters.AddWithValue("card3", Guid.Parse(cardIds[2]));
                insertDeckCommand.Parameters.AddWithValue("card4", Guid.Parse(cardIds[3]));
                insertDeckCommand.ExecuteNonQuery();

                // Kartenstatus aktualisieren
                var updateCardsCommand = new NpgsqlCommand(@"
                    UPDATE user_stacks
                    SET in_deck = TRUE
                    WHERE userid = (SELECT userid FROM users WHERE username = @username)
                      AND card_id = ANY(@cardIds::uuid[]);", connection);
                updateCardsCommand.Parameters.AddWithValue("username", username);
                updateCardsCommand.Parameters.AddWithValue("cardIds", cardIds.ToArray());
                updateCardsCommand.ExecuteNonQuery();

                Console.WriteLine("Deck configured successfully.");
            }
        }




        // Zusätzliche Hilfsmethode zum Abrufen der Benutzerkarten
        private List<UserCard> GetUserCards(int userId, List<string> cardIds)
        {
            string query = @"
                SELECT card_id, in_deck 
                FROM user_stacks 
                WHERE userid = @userId AND card_id = ANY(@cardIds);";

            var parameters = new Dictionary<string, object>
            {
                { "@userId", userId },
                { "@cardIds", cardIds.Select(Guid.Parse).ToArray() }
            };

            var result = ExecuteQuery(query, parameters);
            return result.Select(row => new UserCard
            {
                CardId = Guid.Parse(row["card_id"].ToString()),
                InDeck = Convert.ToBoolean(row["in_deck"])
            }).ToList();
        }

        public class UserCard
        {
            public Guid CardId { get; set; }
            public bool InDeck { get; set; }
        }

        public void AddCardToDeck(int userId, Guid cardId)
        {
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // Prüfen, ob die Karte bereits in einem Deck ist
                    string checkCardQuery = @"
                        SELECT in_deck
                        FROM user_stacks
                        WHERE card_id = @cardId;";

                    var inDeck = GetSingleValue<bool>(checkCardQuery, new Dictionary<string, object>
                    {
                        { "@cardId", cardId }
                    });

                    if (inDeck)
                    {
                        throw new Exception($"Card {cardId} is already in a deck and cannot be selected again.");
                    }

                    // Setze die Karte auf in_deck = 't'
                    string updateCardQuery = @"
                        UPDATE user_stacks
                        SET in_deck = 't'
                        WHERE card_id = @cardId AND userid = @userId;";

                    ExecuteNonQuery(updateCardQuery, new Dictionary<string, object>
                    {
                        { "@cardId", cardId },
                        { "@userId", userId }
                    });

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error adding card to deck: {ex.Message}");
                    throw;
                }
            }
        }

        public List<Cards> GetAvailableCardsForUser(int userId)
        {
            string query = @"
                SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                FROM user_stacks us
                JOIN cards c ON us.card_id = c.card_id
                WHERE us.userid = @userId AND us.in_deck = 'f';";

            var parameters = new Dictionary<string, object>
            {
                { "@userId", userId }
            };

            return ExecuteQuery(query, parameters)
                .Select(row => new Cards(
                    Guid.Parse(row["card_id"].ToString()),
                    row["name"].ToString(),
                    double.Parse(row["damage"].ToString()),
                    row["element_type"].ToString(),
                    row["card_type"].ToString()
                )).ToList();
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

        public void UpdateDeck(int userId, string cardId1, string cardId2, string cardId3, string cardId4)
        {
            // Zuerst das bestehende Deck löschen
            var deleteQuery = "DELETE FROM deck WHERE userid = @userId";
            var deleteParams = new Dictionary<string, object> { { "@userId", userId } };
            ExecuteNonQuery(deleteQuery, deleteParams);

            // Dann die neuen Karten hinzufügen
            var insertQuery = "INSERT INTO deck (userid, cardid) VALUES (@userId, @cardId)";
            var insertParams = new Dictionary<string, object> { { "@userId", userId }, { "@cardId", "" } };

            foreach (var cardId in new List<string> { cardId1, cardId2, cardId3, cardId4 })
            {
                insertParams["@cardId"] = cardId;
                ExecuteNonQuery(insertQuery, new Dictionary<string, object> { { "@userId", userId }, { "@cardId", cardId } });
            }
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
                Guid.Parse(row["card_id"].ToString()),
                row["name"].ToString(),
                double.Parse(row["damage"].ToString()),
                row["element_type"].ToString(),
                row["card_type"].ToString()
            )).ToList();
        }


        public T? GetSingleValue<T>(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using (var command = new NpgsqlCommand(query, conn))
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
                using (var command = new NpgsqlCommand(query, conn))
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing non-query: {ex.Message}");
                throw;
            }
        }

        public List<Dictionary<string, object>> ExecuteQuery(string query, Dictionary<string, object> parameters)
        {
            var results = new List<Dictionary<string, object>>();
            using (var command = new NpgsqlCommand(query, conn))
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

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void UpdateScoreboard(string winnerUsername, string loserUsername)
        {
            // Gewinner upserten
            var queryWinner = @"
                INSERT INTO scoreboard (userid, wins, losses, elo) 
                VALUES ((SELECT userid FROM users WHERE username = @winner), 1, 0, 10)
                ON CONFLICT (userid) 
                DO UPDATE SET wins = scoreboard.wins + 1, elo = scoreboard.elo + 10, updated_at = CURRENT_TIMESTAMP;";

            // Verlierer upserten
            var queryLoser = @"
                INSERT INTO scoreboard (userid, wins, losses, elo) 
                VALUES ((SELECT userid FROM users WHERE username = @loser), 0, 1, -10)
                ON CONFLICT (userid) 
                DO UPDATE SET losses = scoreboard.losses + 1, elo = scoreboard.elo - 10, updated_at = CURRENT_TIMESTAMP;";

            ExecuteNonQuery(queryWinner, new Dictionary<string, object> { { "@winner", winnerUsername } });
            ExecuteNonQuery(queryLoser, new Dictionary<string, object> { { "@loser", loserUsername } });
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
