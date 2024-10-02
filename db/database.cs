using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trimmel_MCTG.DB;

namespace Trimmel_MCTG.db
{
    public class Database
    {
        NpgsqlConnection conn;
        string connection = "Host=localhost;Port=5432;Username=postgres;Password=#;Database=mctg_trimmel";
        public Database()
        {
            conn = new NpgsqlConnection(connection);
            conn.Open();
        }

        public void createTables()
        {
            string CreateTablesCommand = @"
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
                );

                CREATE OR REPLACE FUNCTION set_default_coins()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NEW.coins IS NULL THEN
                        NEW.coins := 20;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER set_default_coins_trigger
                BEFORE INSERT ON users
                FOR EACH ROW
                EXECUTE FUNCTION set_default_coins();
                ";

            using (NpgsqlCommand cmd = new NpgsqlCommand(CreateTablesCommand, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public bool IsUserInDatabase(User user)
        {

            using (NpgsqlCommand cmd = new NpgsqlCommand("select username from users where username = @username", conn))
            {
                cmd.Parameters.AddWithValue("username", user.Username);
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        return true;
                    }
                }

            }
            return false;
        }

    }
}
