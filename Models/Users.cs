using System;
using System.Collections.Generic;
using Trimmel_MCTG.DB;

namespace Trimmel_MCTG.db
{
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
        public List<Cards> Hand { get; set; }
        public List<Cards> DeckStack { get; set; }
        public UserStats UserStats { get; set; }
        public Trading Trading { get; set; }

        public Cards Cards
        {
            get => default;
            set
            {
            }
        }

        public UserStack UserStack
        {
            get => default;
            set
            {
            }
        }

        public Trading Trading1
        {
            get => default;
            set
            {
            }
        }

        public Battle Battle
        {
            get => default;
            set
            {
            }
        }

        public Users()
        {
            Deck = new Decks(0, 0, null, null, null, null);
            Hand = new List<Cards>();
            DeckStack = new List<Cards>();
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
            var user = new Users
            {
                UserId = Convert.ToInt32(row["userid"]),
                Username = row["username"].ToString(),
                Password = row["password"].ToString(),
                Coins = Convert.ToInt32(row["coins"]),
                Bio = row["bio"]?.ToString(),
                Image = row["image"]?.ToString()
            };

            user.Deck = Decks.LoadUserDeck(db, user.UserId);
            user.Hand = db.GetUserHand(user.UserId, user.Deck.GetHandCardIds());
            user.DeckStack = db.GetUserDeckStack(user.UserId, user.Deck.GetHandCardIds());

            return user;
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

       public Cards? DrawCard(Database db)
        {
            if (Hand.Count >= 4)
                return null; 

            Cards? drawnCard = db.DrawCard(UserId);
            if (drawnCard != null)
            {
                Hand.Add(drawnCard);
                DeckStack.Add(drawnCard);
            }

            return drawnCard;
        }

        public Cards? PlayCard(Guid cardId)
        {
            var card = Hand.Find(c => c.CardId == cardId);
            if (card != null)
            {
                Hand.Remove(card);
                return card;
            }
            return null;
        }
    }
}
