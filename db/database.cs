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
                    coins INT DEFAULT 20
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

        public bool IsUserInDatabase(User user)
        {
            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT username FROM users WHERE username = @username", conn))
                {
                    cmd.Parameters.AddWithValue("username", user.Username);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
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
                if (IsUserInDatabase(user))
                {
                    Console.WriteLine("User already exists.");
                    return false;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

                using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO users (username, password, coins) VALUES (@username, @password, @coins);", conn))
                {
                    cmd.Parameters.AddWithValue("username", user.Username);
                    cmd.Parameters.AddWithValue("password", hashedPassword);
                    cmd.Parameters.AddWithValue("coins", 20); // Set default coins value

                    int rowsAffected = cmd.ExecuteNonQuery();
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
            try
            {
                if (!IsUserInDatabase(user))
                {
                    return false;
                }

                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT password FROM users WHERE username = @username;", conn))
                {
                    cmd.Parameters.AddWithValue("username", user.Username);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPassword = reader.GetString(0);
                            return BCrypt.Net.BCrypt.Verify(user.Password, storedPassword);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user login: {ex.Message}");
            }

            return false;
        }

        public void Dispose()
        {
            conn?.Close();
            conn?.Dispose();
        }
    }
}