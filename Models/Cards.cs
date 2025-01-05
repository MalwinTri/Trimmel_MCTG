using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.DB
{
    public class Cards
    {
        [JsonProperty("Id")]
        public Guid CardId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
        public double Damage { get; set; }
        public string ElementType { get; set; } // fire, water, normal
        public string CardType { get; set; } // monster, spell
        public bool InDeck { get; set; }

        public Cards(Guid cardId, string name, double damage, string elementType, string cardType, bool inDeck = false)
        {
            CardId = cardId;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
            InDeck = inDeck;
        }

        public bool HasSpecialAbility()
        {
            // Liste der Karten mit Spezialfähigkeiten
            var specialCards = new List<string>
        {
            "Goblin",
            "Dragon",
            "Wizard",
            "Ork",
            "Knight",
            "WaterSpell",
            "Kraken",
            "FireElves"
        };

            return specialCards.Contains(this.Name);
        }

        public override string ToString()
        {
            return $"Card ID: {CardId}, Name: {Name}, Damage: {Damage}, Element: {ElementType}, Type: {CardType}";
        }

        public void SetElementType()
        {
            if (Name != null)
            {
                if (Name.Contains("Fire", StringComparison.OrdinalIgnoreCase))
                    ElementType = "Fire";
                else if (Name.Contains("Water", StringComparison.OrdinalIgnoreCase))
                    ElementType = "Water";
                else
                    ElementType = "Normal";
            }
        }

        public void SetCardType()
        {
            if (Name != null && Name.Contains("Spell", StringComparison.OrdinalIgnoreCase))
            {
                CardType = "spell";
            }
            else
            {
                CardType = "monster";
            }
        }

        public void ValidateCardType()
        {
            if (CardType != "spell" && CardType != "monster")
            {
                throw new InvalidOperationException("Invalid card type. Allowed values are 'spell' and 'monster'.");
            }
        }

        public string GetCardTypeAsString()
        {
            return CardType.ToLowerInvariant();
        }

        public static string ParseCardType(string cardTypeString)
        {
            if (!string.IsNullOrEmpty(cardTypeString))
            {
                string lowerCardType = cardTypeString.ToLowerInvariant();
                if (lowerCardType == "spell" || lowerCardType == "monster")
                {
                    return lowerCardType;
                }
            }
            return "monster"; // Default value if invalid
        }

        // Hinzugefügte Methode zur Interaktion mit der Datenbank
        public static Cards LoadFromDatabase(Database db, Guid cardId)
        {
            var parameters = new Dictionary<string, object> { { "@cardId", cardId } };
            var result = db.ExecuteQuery("SELECT * FROM cards WHERE card_id = @cardId", parameters);

            if (result.Count == 0)
                throw new KeyNotFoundException($"Card with ID {cardId} not found.");

            var row = result[0];
            return new Cards(
                Guid.Parse(row["card_id"].ToString()),
                row["name"].ToString(),
                Convert.ToDouble(row["damage"]),
                row["element_type"].ToString(),
                row["card_type"].ToString()
            );
        }


    }
}
