using Newtonsoft.Json;
using System;

namespace Trimmel_MCTG.DB
{
    public class Cards
    {
        [JsonProperty("Id")]
        public Guid CardId { get; set; } 

        [JsonProperty("Name")]
        public string Name { get; set; }
        public double Damage { get; set; } 
        public string ElementType { get; set; }
        public string CardType { get; set; }


        public Cards(Guid cardId, string name, double damage, string elementType, string cardType)
        {
            CardId = cardId;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
        }

        // Methode zur Ausgabe der Karteninformationen
        public override string ToString()
        {
            return $"Card ID: {CardId}, Name: {Name}, Damage: {Damage}, Element: {ElementType}, Type: {CardType}";
        }

        // Methode zur Festlegung des Elementtyps basierend auf dem Namen
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

        // Methode zur Festlegung des Kartentyps basierend auf dem Namen
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

        // Methode zur Konvertierung des CardType in einen String (für die Datenbank)
        public string GetCardTypeAsString()
        {
            return CardType.ToString().ToLowerInvariant(); // Enum-Wert in Kleinbuchstaben
        }

        // Methode zur Wiederherstellung des CardType aus einem String (von der Datenbank)
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
            return "monster"; // Standardwert, wenn ungültig
        }

    }
}
