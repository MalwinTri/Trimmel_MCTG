using System;
using System.Collections.Generic;
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

        public static Decks LoadUserDeck(Database db, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@userid", userId }
            };

            var result = db.ExecuteQuery(
                "SELECT deck_id, userid, card_1_id, card_2_id, card_3_id, card_4_id " +
                "FROM decks WHERE userid = @userid",
                parameters
            );

            if (result.Count == 0)
            {
                throw new KeyNotFoundException($"No deck found for userId: {userId}");
            }

            var row = result[0];
            return new Decks
            {
                DeckId = Convert.ToInt32(row["deck_id"]),
                UserId = Convert.ToInt32(row["userid"]),
                Card1Id = row["card_1_id"] as Guid?,
                Card2Id = row["card_2_id"] as Guid?,
                Card3Id = row["card_3_id"] as Guid?,
                Card4Id = row["card_4_id"] as Guid?
            };
        }
    }
}
