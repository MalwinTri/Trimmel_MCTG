using System;
using System.Collections.Generic;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.DB
{
    public class Trading
    {
        public Guid TradingId { get; set; } 
        public int UserId { get; set; }
        public Guid OfferedCardId { get; set; } 
        public string RequiredType { get; set; }
        public int MinDamage { get; set; }

        public Users? User { get; set; }
        public Cards? OfferedCard { get; set; }

        public Trading() { }

        public Trading(int userId, Guid offeredCardId, string requiredType, int minDamage)
        {
            UserId = userId;
            OfferedCardId = offeredCardId;
            RequiredType = requiredType;
            MinDamage = minDamage;
        }

        public static Trading LoadFromDatabase(Database db, Guid tradingId)
        {
            var parameters = new Dictionary<string, object> { { "@tradingId", tradingId } };
            var result = db.ExecuteQuery("SELECT * FROM trading WHERE tradingid = @tradingId", parameters);

            if (result.Count == 0)
                throw new KeyNotFoundException($"Trading entry with ID {tradingId} not found.");

            var row = result[0];
            return new Trading
            {
                TradingId = Guid.Parse(row["tradingid"].ToString()),
                UserId = Convert.ToInt32(row["userid"]),
                OfferedCardId = Guid.Parse(row["offered_card_id"].ToString()),
                RequiredType = row["required_type"].ToString(),
                MinDamage = Convert.ToInt32(row["min_damage"])
            };
        }

        public void SaveToDatabase(Database db)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@userid", UserId },
                { "@offeredCardId", OfferedCardId },
                { "@requiredType", RequiredType },
                { "@minDamage", MinDamage }
            };

            if (TradingId == Guid.Empty)
            {
                db.ExecuteNonQuery(
                    "INSERT INTO trading (userid, offered_card_id, required_type, min_damage) VALUES (@userid, @offeredCardId, @requiredType, @minDamage)",
                    parameters
                );
            }
            else 
            {
                parameters["@tradingId"] = TradingId;
                db.ExecuteNonQuery(
                    "UPDATE trading SET userid = @userid, offered_card_id = @offeredCardId, required_type = @requiredType, min_damage = @minDamage WHERE tradingid = @tradingId",
                    parameters
                );
            }
        }

        public void DeleteFromDatabase(Database db)
        {
            var parameters = new Dictionary<string, object> { { "@tradingId", TradingId } };
            db.ExecuteNonQuery("DELETE FROM trading WHERE tradingid = @tradingId", parameters);
        }

        public void LoadRelatedData(Database db)
        {
            User = Users.LoadFromDatabase(db, UserId);
            OfferedCard = Cards.LoadFromDatabase(db, OfferedCardId);
        }

        public bool IsValid()
        {
            return UserId > 0 &&
                   OfferedCardId != Guid.Empty &&
                   !string.IsNullOrEmpty(RequiredType) &&
                   MinDamage >= 0;
        }
    }
}
