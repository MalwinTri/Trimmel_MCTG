using System;
using System.Collections.Generic;
using System.Linq;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.DB
{
    public class Decks
    {
        public int DeckId { get; set; }
        public int UserId { get; set; }
        public Guid? Card1Id { get; set; }
        public Guid? Card2Id { get; set; }
        public Guid? Card3Id { get; set; }
        public Guid? Card4Id { get; set; }

        // Konstruktor
        public Decks(int deckId, int userId, Guid? card1Id, Guid? card2Id, Guid? card3Id, Guid? card4Id)
        {
            DeckId = deckId;
            UserId = userId;
            Card1Id = card1Id;
            Card2Id = card2Id;
            Card3Id = card3Id;
            Card4Id = card4Id;
        }

        public static Decks LoadUserDeck(Database db, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@userid", userId }
            };

            var result = db.ExecuteQuery(
                "SELECT deck_id, userid, card_1_id, card_2_id, card_3_id, card_4_id FROM decks WHERE userid = @userid",
                parameters
            );

            if (result.Count == 0)
            {
                throw new KeyNotFoundException($"No deck found for userId: {userId}");
            }

            var row = result[0];
            return new Decks(
                Convert.ToInt32(row["deck_id"]),
                Convert.ToInt32(row["userid"]),
                row["card_1_id"] != DBNull.Value ? Guid.Parse(row["card_1_id"].ToString()) : (Guid?)null,
                row["card_2_id"] != DBNull.Value ? Guid.Parse(row["card_2_id"].ToString()) : (Guid?)null,
                row["card_3_id"] != DBNull.Value ? Guid.Parse(row["card_3_id"].ToString()) : (Guid?)null,
                row["card_4_id"] != DBNull.Value ? Guid.Parse(row["card_4_id"].ToString()) : (Guid?)null
            );
        }

        public List<Guid> GetHandCardIds()
        {
            return new List<Guid?> { Card1Id, Card2Id, Card3Id, Card4Id }
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();
        }
    }
}
