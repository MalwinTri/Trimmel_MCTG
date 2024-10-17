using BCrypt.Net;
using Npgsql;
using System;
using System.Collections.Generic;
using Trimmel_MCTG.DB;

namespace Trimmel_MCTG.db
{
    public class Database : IDisposable
    {
        private readonly NpgsqlConnection conn;
        // Connection String 
        private readonly string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=#;Database=mctg_trimmel";

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
                    user_id SERIAL PRIMARY KEY,
                    username VARCHAR(50) NOT NULL UNIQUE,
                    password VARCHAR(255) NOT NULL,
                    coins INT DEFAULT 20,
                    token VARCHAR(255) UNIQUE
                );

                CREATE TABLE IF NOT EXISTS user_stats (
                    user_id INT PRIMARY KEY,
                    wins INT DEFAULT 0,
                    losses INT DEFAULT 0,
                    elo INT DEFAULT 1000,
                    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS cards (
                    card_id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    damage INT NOT NULL,
                    element_type VARCHAR(50) NOT NULL,
                    card_type VARCHAR(50) NOT NULL CHECK (card_type IN ('spell', 'monster'))
                );

                CREATE TABLE IF NOT EXISTS user_stacks (
                    user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
                    card_id INT REFERENCES cards(card_id) ON DELETE CASCADE,
                    in_deck BOOLEAN DEFAULT FALSE,
                    PRIMARY KEY (user_id, card_id)
                );

                CREATE TABLE IF NOT EXISTS packages (
                    package_id SERIAL PRIMARY KEY,
                    price INT DEFAULT 5
                );

                CREATE TABLE IF NOT EXISTS package_cards (
                    package_id INT REFERENCES packages(package_id) ON DELETE CASCADE,
                    card_id INT REFERENCES cards(card_id) ON DELETE CASCADE,
                    PRIMARY KEY (package_id, card_id)
                );

                CREATE TABLE IF NOT EXISTS decks (
                    deck_id SERIAL PRIMARY KEY,
                    user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
                    card_1_id INT REFERENCES cards(card_id) ON DELETE CASCADE,
                    card_2_id INT REFERENCES cards(card_id) ON DELETE CASCADE,
                    card_3_id INT REFERENCES cards(card_id) ON DELETE CASCADE,
                    card_4_id INT REFERENCES cards(card_id) ON DELETE CASCADE
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
                    offered_card_id INT REFERENCES cards(card_id) ON DELETE CASCADE,
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
                DROP TABLE IF EXISTS package_cards;
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
