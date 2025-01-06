using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trimmel_MCTG.helperClass;

public class BattleExecuter : IRouteCommand
{
    private readonly RequestContext requestContext;
    private Database db;

    // Warteschlange für Spieler
    private static Queue<int> waitingPlayers = new Queue<int>();
    private static Random rand = new Random();

    private StringBuilder battleLog = new StringBuilder();

    public BattleExecuter(RequestContext requestContext)
    {
        this.requestContext = requestContext;
    }

    public void SetDatabase(Database database)
    {
        db = database;
    }

    public Response Execute()
    {
        var response = new Response();

        try
        {
            string logFilePath = "battle_log.txt";

            string username = ExtractUsernameFromToken(requestContext.Token);
            var user = Users.LoadFromDatabase(db, username);

            if (user == null)
            {
                response.Payload = "User not found.";
                response.StatusCode = StatusCode.NotFound;
                return response;
            }

            // Schreibe in den Log, wer sich gemeldet hat
            battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - User '{user.Username}' joined battles.");

            lock (waitingPlayers)
            {
                if (waitingPlayers.Count == 0)
                {
                    waitingPlayers.Enqueue(user.UserId);
                    battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - 1st player waiting: {user.Username}");

                    response.Payload = "Wait for the second player";
                    response.StatusCode = StatusCode.Accepted;

                    // Log in Datei sichern
                    SaveLogsToFile(logFilePath);

                    return response;
                }
                else
                {
                    int opponentId = waitingPlayers.Dequeue();
                    var opponent = Users.LoadFromDatabase(db, opponentId);

                    battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - 2nd player found: {opponent.Username}. Battle starting...");

                    // Starte den Kampf zwischen user und opponent
                    var result = DoBattle(user, opponent);

                    battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - {result.Winner.Username} hat den Kampf gewonnen.");

                    response.Payload = $"Kampf beendet. Gewinner: {result.Winner.Username}\n\nBattle Log:\n{battleLog.ToString().Replace(Environment.NewLine, "\n")}";
                    response.StatusCode = StatusCode.Ok;

                    // Am Ende vom Kampf Log speichern
                    SaveLogsToFile(logFilePath);

                    return response;
                }
            }
        }
        catch (Exception ex)
        {
            // Falls Fehler: in Log + Response
            battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - {ex.Message}");
            var responseError = new Response
            {
                Payload = $"An error occurred: {ex.Message}",
                StatusCode = StatusCode.InternalServerError
            };

            // Log in Datei sichern
            SaveLogsToFile("battle_log.txt");
            return responseError;
        }
    }


    // ---------------------------------------
    // Hier starten wir den eigentlichen Kampf
    private void StartBattle(Users player1, Users player2)
    {
        Console.WriteLine($"Figth between {player1.Username} and {player2.Username} starting.");

        int battleId = db.InsertBattle(player1.UserId, player2.UserId);

        if (battleId == -1)
        {
            Console.WriteLine("Failed to insert battle into database.");
            return;
        }

        // Hole das Deck beider Spieler aus der DB. 
        // Angenommen, db.GetConfiguredDeck(...) liefert eine Liste von bis zu 20 Karten.
        var deck1 = db.GetConfiguredDeck(player1.Username);
        var deck2 = db.GetConfiguredDeck(player2.Username);

        // Rufe die erweiterte Kampf-Logik auf
        var result = DoBattle(player1, player2);

            
        Console.WriteLine($"{result.Winner.Username} has won!");

        // Stats anpassen (Wins, Losses, Elo)
        UpdateStats(result.Winner, true);
        UpdateStats(result.Loser, false);

        // **Coins und Karten austauschen** (Siehe EndBattle-Logik in DoBattle)
    }

    // ---------------------------------------
    // Das Herzstück der erweiterten Kampf-Logik
    // mit Deck & Hand-Mechanik
    private BattleResult DoBattle(Users player1, Users player2)
    {
        Console.WriteLine($"Fight between {player1.Username} and {player2.Username} starting.");


        // Lade die gesamten Decks beider Spieler
        var deck1 = db.GetAllDeckCards(player1.UserId);
        var deck2 = db.GetAllDeckCards(player2.UserId);

        try
        {
            // Führe den Kampf durch
            var result = ExecuteBattle(player1, deck1, player2, deck2);

            // Aktualisiere ELO-Ratings und Statistiken
            db.UpdateUserStats(result.Winner.UserId, true);
            db.UpdateUserStats(result.Loser.UserId, false);
            db.UpdateElo(result.Winner.UserId, result.Loser.UserId);

            // Weitere Aktionen wie das Stehlen von Karten werden bereits in ExecuteBattle behandelt

            return result;
        }
        catch (Exception ex)
        {
            // Fehler protokollieren
            battleLog.AppendLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {ex.Message}");
            throw;
        }
    }


    // ---------------------------------------
    // Das Herzstück der erweiterten Kampf-Logik
    // mit Deck & Hand-Mechanik
    private BattleResult ExecuteBattle(Users p1, List<Cards> userStacks1, Users p2, List<Cards> userStacks2)
    {
        // Initialisiere Hand und Deck für beide Spieler
        var hand1 = new List<Cards>();
        var deck1 = new List<Cards>(userStacks1); // Kopie der UserStacks als Deck
        var hand2 = new List<Cards>();
        var deck2 = new List<Cards>(userStacks2); // Kopie der UserStacks als Deck

        // Begrenze das Deck auf maximal 20 Karten (optional)
        if (deck1.Count > 20) deck1 = deck1.Take(20).ToList();
        if (deck2.Count > 20) deck2 = deck2.Take(20).ToList();

        // Ziehe die Anfangshand (z.B. 4 Karten) aus dem Deck
        DrawInitialHand(deck1, hand1, p1.UserId);
        DrawInitialHand(deck2, hand2, p2.UserId);

        int roundNumber = 1;
        int maxRounds = 100; // Optional: Maximale Rundenanzahl, um Endlosschleifen zu vermeiden

        while (roundNumber <= maxRounds)
        {
            battleLog.AppendLine($"\n--- Runde {roundNumber} ---");

            // Überprüfe, ob ein Spieler verloren hat
            if (deck1.Count == 0 && hand1.Count == 0)
            {
                var result = EndBattle(p2, p1, deck2, deck1);
                battleLog.AppendLine($"**{p2.Username} won the Fight!**");
                return result;
            }
            if (deck2.Count == 0 && hand2.Count == 0)
            {
                var result = EndBattle(p1, p2, deck1, deck2);
                battleLog.AppendLine($"**{p1.Username} won the Fight!!**");
                return result;
            }

            //// Fülle die Hand auf (z.B. 4 Karten) aus dem Deck
            DrawHand(deck1, hand1, p1.UserId);
            DrawHand(deck2, hand2, p2.UserId);

            // Überprüfe erneut, ob ein Spieler keine Karten mehr hat
            if (hand1.Count == 0 && deck1.Count == 0)
            {
                var result = EndBattle(p2, p1, deck2, deck1);
                battleLog.AppendLine($"**{p2.Username} won the Fight!!**");
                return result;
            }
            if (hand2.Count == 0 && deck2.Count == 0)
            {
                var result = EndBattle(p1, p2, deck1, deck2);
                battleLog.AppendLine($"**{p1.Username} won the Fight!!**");
                return result;
            }

            // Jeder Spieler wählt ein Monster und 0-2 Spells aus der Hand
            var (p1Monster, p1Spells) = ChooseMonsterAndSpells(hand1);
            var (p2Monster, p2Spells) = ChooseMonsterAndSpells(hand2);

            if (p1Monster == null || p2Monster == null)
            {
                // Falls einer kein Monster hat, breche den Kampf ab
                battleLog.AppendLine("   -> One of the players cannot play a monster. Fight is canceled.");
                break;
            }

            // Berechne den Schaden
            double dmg1 = CalculateDamage(p1, p1Monster, p1Spells, p2, p2Monster, p2Spells);
            double dmg2 = CalculateDamage(p2, p2Monster, p2Spells, p1, p1Monster, p1Spells);

            // Logge die gespielten Karten und Spells
            string p1SpellsNames = p1Spells.Count > 0 ? string.Join(", ", p1Spells.Select(s => s.Name)) : "no";
            string p2SpellsNames = p2Spells.Count > 0 ? string.Join(", ", p2Spells.Select(s => s.Name)) : "no";

            battleLog.AppendLine($"**{p1.Username} plays {p1Monster.Name} + {p1Spells.Count} Spells ({p1SpellsNames}) => {dmg1:F2} Damage**");
            battleLog.AppendLine($"**{p2.Username} plays {p2Monster.Name} + {p2Spells.Count} Spells ({p2SpellsNames}) => {dmg2:F2} Damage**");

            // Bestimme das Ergebnis der Runde
            if (Math.Abs(dmg1 - dmg2) < 0.001)
            {
                // **Unentschieden:** Zufälligen Gewinner bestimmen
                Users randomWinner, randomLoser;
                if (rand.Next(2) == 0)
                {
                    randomWinner = p1;
                    randomLoser = p2;
                }
                else
                {
                    randomWinner = p2;
                    randomLoser = p1;
                }

                // Wähle eine zufällige Karte aus der Hand des Verlierers, um sie zu entfernen
                List<Cards> loserHand = randomLoser.UserId == p1.UserId ? hand1 : hand2;
                if (loserHand.Count > 0)
                {
                    int cardIndex = rand.Next(loserHand.Count);
                    Cards cardToRemove = loserHand[cardIndex];
                    loserHand.RemoveAt(cardIndex);
                    db.RemoveCardFromHand(randomLoser.UserId, cardToRemove.CardId);

                    battleLog.AppendLine($"-> **Draw! {randomWinner.Username} wins the round by chance and removes {cardToRemove.Name} from {randomLoser.Username}.**");
                }
                else
                {
                    // Verlierer hat keine Karten mehr
                    battleLog.AppendLine($"-> **Draw! {randomLoser.Username} has no more cards. {randomWinner.Username} wins the fight!**");
                    return new BattleResult { Winner = randomWinner, Loser = randomLoser };
                }
            }
            else if (dmg1 > dmg2)
            {
                battleLog.AppendLine($"-> **{p1.Username} won the round! {p2Monster.Name} will be removed.**");
                hand2.Remove(p2Monster);
                db.RemoveCardFromHand(p2.UserId, p2Monster.CardId);
            }
            else
            {
                battleLog.AppendLine($"-> **{p2.Username} wins the round! {p1Monster.Name} is removed.**");
                hand1.Remove(p1Monster);
                db.RemoveCardFromHand(p1.UserId, p1Monster.CardId);
            }

            battleLog.AppendLine();
            roundNumber++;
        }

        // Falls die maximale Rundenanzahl erreicht wurde
        var defaultResultEnd = EndBattle(p1, p2, deck1, deck2);
        battleLog.AppendLine($"**{defaultResultEnd.Winner.Username} won the fight!**");
        return defaultResultEnd;
    }



    // ---------------------------------------
    // Zieht so lange Karten vom Deck, bis Hand 4 Karten hat (oder Deck leer)
    private void DrawHand(List<Cards> deck, List<Cards> hand, int userId)
    {
        while (hand.Count < 4 && deck.Count > 0)
        {
            // Ziehe eine zufällige Karte aus dem Deck
            int idx = rand.Next(deck.Count);
            var card = deck[idx];
            deck.RemoveAt(idx);
            hand.Add(card);

            // Aktualisiere die Datenbank: Setze in_deck auf FALSE (Handkarte)
            bool updateSuccess = db.UpdateCardStatus(userId, card.CardId, false);
            if (updateSuccess)
            {
                battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - User '{userId}' draw Card '{card.Name}'");
            }
            else
            {
                battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - Error drawing card '{card.Name}' from user '{userId}'.");
            }
        }
    }


    private void DrawInitialHand(List<Cards> deck, List<Cards> hand, int userId)
    {
        const int initialHandSize = 4;
        for (int i = 0; i < initialHandSize && deck.Count > 0; i++)
        {
            int idx = rand.Next(deck.Count);
            var card = deck[idx];
            deck.RemoveAt(idx);
            hand.Add(card);

            // Aktualisiere die Datenbank: Setze in_deck auf FALSE (Handkarte)
            bool updateSuccess = db.UpdateCardStatus(userId, card.CardId, false);
            if (updateSuccess)
            {
                Console.WriteLine($"User '{userId}' draws card '{card.Name}'.");
            }
            else
            {
                Console.WriteLine($"\r\nError drawing card '{card.Name}' for users '{userId}'.");
            }
        }
    }



    // ---------------------------------------
    // Kampf-Ende: Gewinner klaut eine zufällige Karte vom Verlierer (+10 Coins / +5 Coins)
    private BattleResult EndBattle(Users winner, Users loser, List<Cards> winnerDeck, List<Cards> loserDeck)
    {
        Console.WriteLine($"{winner.Username} has won. {loser.Username} has lost.");

        loserDeck = db.GetAllDeckCards(loser.UserId);

        // 1) Karte klauen
        if (loserDeck.Count > 0)
        {
            var stolenCard = loserDeck[rand.Next(loserDeck.Count)];
            loserDeck.Remove(stolenCard);

            winnerDeck.Add(stolenCard);

            // Übertrage die Karte in die Datenbank
            db.TransferCard(stolenCard.CardId, loser.UserId, winner.UserId);

            battleLog.AppendLine($"   -> {winner.Username} steals {stolenCard.Name} from {loser.Username}");
        }
        else
        {
            battleLog.AppendLine("   -> No card available to steal!");
        }

        // 2) Coins anpassen
        int winnerCoins = db.GetUserCoins(winner.Username);
        int loserCoins = db.GetUserCoins(loser.Username);

        db.UpdateUserCoins(winner.Username, winnerCoins + 10);
        db.UpdateUserCoins(loser.Username, loserCoins + 5);

        battleLog.AppendLine($"   -> {winner.Username} receives 10 coins.");
        battleLog.AppendLine($"   -> {loser.Username} receives 5 coins.");

        // Rückgabe des Ergebnisses 
        return new BattleResult { Winner = winner, Loser = loser };
    }



    // ---------------------------------------
    // Wählt 1 Monster + 0..2 Spells aus der Hand
    private (Cards monster, List<Cards> spells) ChooseMonsterAndSpells(List<Cards> hand)
    {
        if (hand == null || hand.Count == 0)
            return (null, new List<Cards>());

        var monsters = hand.FindAll(c => c.CardType.Equals("monster", StringComparison.OrdinalIgnoreCase));
        var spells = hand.FindAll(c => c.CardType.Equals("spell", StringComparison.OrdinalIgnoreCase));

        if (monsters.Count == 0)
            return (null, new List<Cards>());

        // Wähle ein Monster zufällig
        var monster = monsters[rand.Next(monsters.Count)];

        // Wähle 0..2 Spells zufällig
        int numSpells = rand.Next(3); // 0, 1 oder 2
        var chosenSpells = new List<Cards>();

        for (int i = 0; i < numSpells && spells.Count > 0; i++)
        {
            int idx = rand.Next(spells.Count);
            var chosenSpell = spells[idx];

            // In chosenSpells übernehmen
            chosenSpells.Add(chosenSpell);

            // Spell aus "hand" und "spells" entfernen, damit er nicht wiederverwendet werden kann
            hand.Remove(chosenSpell);
            spells.RemoveAt(idx);
        }

        // Log the chosen spells
        if (chosenSpells.Count > 0)
        {
            string spellsNames = string.Join(", ", chosenSpells.Select(s => s.Name));
            battleLog.AppendLine($"      -> Selected Spells: {spellsNames}");
        }
        else
        {
            battleLog.AppendLine($"      -> No spells chosen.");
        }

        return (monster, chosenSpells);
    }



    // ---------------------------------------
    private double CalculateDamage(Users attacker, Cards attackerCard, List<Cards> attackerSpells, Users defender, Cards defenderCard, List<Cards> defenderSpells)
    {
        double baseDamage = attackerCard.Damage;
        battleLog.AppendLine($"   -> Base damage from {attackerCard.Name}: {baseDamage:F2}");

        // Schaden durch Spells erhöhen (+20% pro Spell)
        if (attackerSpells != null && attackerSpells.Count > 0)
        {
            foreach (var spell in attackerSpells)
            {
                double beforeSpell = baseDamage;
                baseDamage *= 1.2;
                battleLog.AppendLine($"   -> Spell '{spell.Name}' buffet the damage of {beforeSpell:F2} on {baseDamage:F2}");
            }
        }
        else
        {
            battleLog.AppendLine($"   -> No spells played, damage remains {baseDamage:F2}");
        }

        // Effektivitätsmultiplikator anwenden
        double multiplier = Effectiveness.GetDamageMultiplier(attackerCard.ElementType, defenderCard.ElementType);
        double beforeMultiplier = baseDamage;
        baseDamage *= multiplier;
        battleLog.AppendLine($"   -> Effectiveness multiplier ({attackerCard.ElementType} opposite {defenderCard.ElementType}): {multiplier} => Damage: {baseDamage:F2}");

        // Spezialfähigkeiten berücksichtigen
        double finalDamage = ApplySpecialAbilities(attackerCard, defenderCard, baseDamage);
        battleLog.AppendLine($"   -> Final damage after special abilities: {finalDamage:F2}");
        return finalDamage;
    }

        
        

    private double ApplyElementModifier(double dmg, string attackerElem, string defenderElem)
    {
        attackerElem = attackerElem?.ToLower() ?? "";
        defenderElem = defenderElem?.ToLower() ?? "";

        // Beispiel: Fire -> Water = 0.5
        if (attackerElem == "fire" && defenderElem == "water")
            return dmg * 0.5;
        // Water -> Fire = 2.0
        if (attackerElem == "water" && defenderElem == "fire")
            return dmg * 2.0;

        return dmg;
    }
    private double ApplySpecialAbilities(Cards attacker, Cards defender, double damage)
    {
        // Goblins sind zu feige, um gegen Drachen zu kämpfen
        if (attacker.Name == "Goblin" && defender.Name == "Dragon")
        {
            // Goblin kann nicht angreifen
            return 0;
        }

        if (defender.Name == "Dragon" && attacker.Name == "Goblin")
        {
            // Goblin kann nicht angreifen
            return 0;
        }

        // Wizard kann Orks kontrollieren, sodass Orks ihn nicht angreifen können
        if (attacker.Name == "Wizard" && defender.Name == "Ork")
        {
            // Ork kann Wizard nicht angreifen
            return 0;
        }

        if (defender.Name == "Wizard" && attacker.Name == "Ork")
        {
            // Ork kann Wizard nicht angreifen
            return 0;
        }

        // Knights sind durch WaterSpells sofort ertränkt
        if (attacker.CardType == "spell" && attacker.Name == "WaterSpell" && defender.Name == "Knight")
        {
            return double.MaxValue; // Sofortiger Sieg über Knight
        }

        if (defender.CardType == "spell" && defender.Name == "WaterSpell" && attacker.Name == "Knight")
        {
            return double.MaxValue; // Sofortiger Sieg über Knight
        }

        // Kraken ist immun gegen Zauber
        if (defender.Name == "Kraken" && attacker.CardType == "spell")
        {
            return 0; // Kein Schaden
        }

        if (attacker.Name == "Kraken" && defender.CardType == "spell")
        {
            // Kraken kann gegen Zauber angreifen, aber Zauber haben keine Wirkung
            return attacker.Damage;
        }

        // FireElves können Drachen ausweichen
        if (attacker.Name == "FireElves" && defender.Name == "Dragon")
        {
            // Drachen können FireElves nicht treffen
            return double.MaxValue; // FireElves gewinnen die Runde
        }

        if (defender.Name == "FireElves" && attacker.Name == "Dragon")
        {
            // FireElves können Drachen ausweichen
            return double.MaxValue; // FireElves gewinnen die Runde
        }

        return damage;
    }

    private bool HasMonsters(List<Cards> deck)
    {
        return deck.Exists(c => c.CardType.Equals("monster", StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateStats(Users player, bool won)
    {
        var stats = UserStats.LoadOrCreateStats(db, player.UserId);

        if (won)
        {
            stats.Wins++;
            stats.Elo += 10;
        }
        else
        {
            stats.Losses++;
            stats.Elo -= 5;
        }

        stats.SaveToDatabase(db);
    }

    private string ExtractUsernameFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new UnauthorizedAccessException("Authorization token is missing.");

        var parts = token.Split('-');
        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
            return parts[0];

        throw new UnauthorizedAccessException("Invalid token format.");
    }

    // Speichert den aktuellen Stand des Logs in eine Datei
    private void SaveLogsToFile(string filePath)
    {
        try
        {
            // Append anstatt überschreiben
            using (var writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(battleLog.ToString());
                writer.WriteLine("--------------------------------------------------");
            }
            // Anschließend den StringBuilder leeren, falls du nicht jedes Mal alles doppelt willst
            battleLog.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Could not write log to file: {ex.Message}");
        }
    }

}

