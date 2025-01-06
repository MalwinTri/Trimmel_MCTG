using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        Console.WriteLine($"Kampf zwischen {player1.Username} und {player2.Username} gestartet.");

        // Hole das Deck beider Spieler aus der DB. 
        // Angenommen, db.GetConfiguredDeck(...) liefert eine Liste von bis zu 20 Karten.
        var deck1 = db.GetConfiguredDeck(player1.Username);
        var deck2 = db.GetConfiguredDeck(player2.Username);

        // Rufe die erweiterte Kampf-Logik auf
        var result = DoBattle(player1, player2);

            
        Console.WriteLine($"{result.Winner.Username} hat gewonnen!");

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
        Console.WriteLine($"Kampf zwischen {player1.Username} und {player2.Username} gestartet.");

        // Lade die konfigurierten Decks beider Spieler
        var deck1 = db.GetConfiguredDeck(player1.Username);
        var deck2 = db.GetConfiguredDeck(player2.Username);

        try
        {
            // Führe den Kampf durch
            var result = ExecuteBattle(player1, deck1, player2, deck2);

            // Aktualisiere ELO-Ratings und Statistiken
            db.UpdateUserStats(result.Winner.UserId, true);
            db.UpdateUserStats(result.Loser.UserId, false);
            db.UpdateElo(result.Winner.UserId, result.Loser.UserId);


            return result;
        }
        catch (Exception ex)
        {
            // Rollback bei Fehler 
            battleLog.AppendLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {ex.Message}");
            throw;
        }
    }

    // ---------------------------------------
    // Das Herzstück der erweiterten Kampf-Logik
    // mit Deck & Hand-Mechanik
    private BattleResult ExecuteBattle(Users p1, List<Cards> deck1, Users p2, List<Cards> deck2)
    {
        // Jede Runde haben die Spieler eine "Hand" mit bis zu 4 Karten
        var hand1 = new List<Cards>();
        var hand2 = new List<Cards>();

        // Vor dem Kampf kann man sicherstellen, dass das Deck max. 20 Karten hat:
        if (deck1.Count > 20) deck1 = deck1.Take(20).ToList();
        if (deck2.Count > 20) deck2 = deck2.Take(20).ToList();

        // Zieht anfangs bis zu 4 Karten auf die Hand
        DrawHand(deck1, hand1, p1.UserId);
        DrawHand(deck2, hand2, p2.UserId);

        while (true)
        {
            // Check, ob jemand bereits verloren hat (Deck + Hand leer)
            if (deck1.Count == 0 && hand1.Count == 0)
            {
                // p1 hat verloren, p2 gewinnt
                var result = EndBattle(p2, p1, deck2, deck1);
                return result;
            }
            if (deck2.Count == 0 && hand2.Count == 0)
            {
                // p2 hat verloren, p1 gewinnt
                var result = EndBattle(p1, p2, deck1, deck2);
                return result;
            }

            // Vor jeder Runde erneut auf 4 Handkarten auffüllen (falls im Deck noch was ist)
            DrawHand(deck1, hand1, p1.UserId);
            DrawHand(deck2, hand2, p2.UserId);

            // Prüfe erneut, ob die Spieler Karten haben
            if (hand1.Count == 0 && deck1.Count == 0)
            {
                var result = EndBattle(p2, p1, deck2, deck1);
                return result;
            }
            if (hand2.Count == 0 && deck2.Count == 0)
            {
                var result = EndBattle(p1, p2, deck1, deck2);
                return result;
            }

            // 1) Jeder wählt 1 Monster + 0..2 Spells von seiner Hand
            var (p1Monster, p1Spells) = ChooseMonsterAndSpells(hand1);
            var (p2Monster, p2Spells) = ChooseMonsterAndSpells(hand2);

            if (p1Monster == null || p2Monster == null)
            {
                // Falls einer kein Monster hat -> checke erneut Deck+Hand?
                // Dann Abbruch.
                if (deck1.Count == 0 && hand1.Count == 0)
                    return EndBattle(p2, p1, deck2, deck1);
                if (deck2.Count == 0 && hand2.Count == 0)
                    return EndBattle(p1, p2, deck1, deck2);

                // Sonst machst du eine alternative Regel. Hier brechen wir den Kampf ab.
                battleLog.AppendLine("   -> Einer der Spieler kann kein Monster spielen. Kampf wird abgebrochen.");
                break;
            }

            // 2) Schaden berechnen
            double dmg1 = CalculateDamage(p1, p1Monster, p1Spells, p2, p2Monster, p2Spells);
            double dmg2 = CalculateDamage(p2, p2Monster, p2Spells, p1, p1Monster, p1Spells);

            // Log the played cards and spells
            string p1SpellsNames = p1Spells.Count > 0 ? string.Join(", ", p1Spells.Select(s => s.Name)) : "keine";
            string p2SpellsNames = p2Spells.Count > 0 ? string.Join(", ", p2Spells.Select(s => s.Name)) : "keine";

            battleLog.AppendLine($"-> {p1.Username} spielt {p1Monster.Name} + {p1Spells.Count} Spells ({p1SpellsNames}) => {dmg1:F2}");
            battleLog.AppendLine($"-> {p2.Username} spielt {p2Monster.Name} + {p2Spells.Count} Spells ({p2SpellsNames}) => {dmg2:F2}");

            // 3) Runden-Ergebnis
            if (Math.Abs(dmg1 - dmg2) < 0.001)
            {
                battleLog.AppendLine("   -> Unentschieden! Keine Karte wird entfernt.");
            }
            else if (dmg1 > dmg2)
            {
                battleLog.AppendLine($"   -> {p1.Username} gewinnt die Runde! {p2Monster.Name} wird entfernt.");
                hand2.Remove(p2Monster);
                db.RemoveCardFromHand(p2.UserId, p2Monster.CardId);
            }
            else
            {
                battleLog.AppendLine($"   -> {p2.Username} gewinnt die Runde! {p1Monster.Name} wird entfernt.");
                hand1.Remove(p1Monster);
                db.RemoveCardFromHand(p1.UserId, p1Monster.CardId);
            }

            battleLog.AppendLine();
        }

        // Falls wir aus while(true) rausfallen, definieren wir einen Default:
        var defaultResult = EndBattle(p1, p2, deck1, deck2);
        return defaultResult;
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

            // Aktualisiere die Datenbank: Setze in_deck auf true (Handkarte)
            db.UpdateCardStatus(userId, card.CardId, true);

            battleLog.AppendLine($"{DateTime.Now:HH:mm:ss} - User '{userId}' zieht Karte '{card.Name}'.");
        }
    }

    // ---------------------------------------
    // Kampf-Ende: Gewinner klaut eine zufällige Karte vom Verlierer (+10 Coins / +5 Coins)
    private BattleResult EndBattle(Users winner, Users loser, List<Cards> winnerDeck, List<Cards> loserDeck)
    {
        Console.WriteLine($"{winner.Username} hat gewonnen. {loser.Username} hat verloren.");

        // 1) Karte klauen
        if (loserDeck.Count > 0)
        {
            var rand = new Random();
            int idx = rand.Next(loserDeck.Count);
            var stolenCard = loserDeck[idx];
            loserDeck.RemoveAt(idx);

            winnerDeck.Add(stolenCard);

            Console.WriteLine($"-> {winner.Username} stiehlt {stolenCard.Name} von {loser.Username}");
        }
        else
        {
            Console.WriteLine("-> Keine Karte zum Stehlen vorhanden!");
        }

        // 2) Coins anpassen (Beispielmethoden: db.GetUserCoins(), db.UpdateUserCoins() musst du selbst definieren)
        int winnerCoins = db.GetUserCoins(winner.Username);
        int loserCoins = db.GetUserCoins(loser.Username);

        db.UpdateUserCoins(winner.Username, winnerCoins + 10);
        db.UpdateUserCoins(loser.Username, loserCoins + 5);

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
            battleLog.AppendLine($"      -> Gewählte Spells: {spellsNames}");
        }
        else
        {
            battleLog.AppendLine($"      -> Keine Spells gewählt.");
        }

        return (monster, chosenSpells);
    }



    // ---------------------------------------
    private double CalculateDamage(Users attacker, Cards attackerCard, List<Cards> attackerSpells, Users defender, Cards defenderCard, List<Cards> defenderSpells)
    {
        double baseDamage = attackerCard.Damage;
        battleLog.AppendLine($"   -> Basis Schaden von {attackerCard.Name}: {baseDamage:F2}");

        // Schaden durch Spells erhöhen (+20% pro Spell)
        if (attackerSpells != null && attackerSpells.Count > 0)
        {
            foreach (var spell in attackerSpells)
            {
                double beforeSpell = baseDamage;
                baseDamage *= 1.2;
                battleLog.AppendLine($"   -> Spell '{spell.Name}' buffet den Schaden von {beforeSpell:F2} auf {baseDamage:F2}");
            }
        }
        else
        {
            battleLog.AppendLine($"   -> Keine Spells gespielt, Schaden bleibt bei {baseDamage:F2}");
        }

        // Effektivitätsmultiplikator anwenden
        double multiplier = Effectiveness.GetDamageMultiplier(attackerCard.ElementType, defenderCard.ElementType);
        double beforeMultiplier = baseDamage;
        baseDamage *= multiplier;
        battleLog.AppendLine($"   -> Effektivitätsmultiplikator ({attackerCard.ElementType} gegenüber {defenderCard.ElementType}): {multiplier} => Schaden: {baseDamage:F2}");

        // Spezialfähigkeiten berücksichtigen
        double finalDamage = ApplySpecialAbilities(attackerCard, defenderCard, baseDamage);
        battleLog.AppendLine($"   -> Endgültiger Schaden nach Spezialfähigkeiten: {finalDamage:F2}");
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

// ---------------------------------------
// Dein BattleResult-Klasse wie gehabt
public class BattleResult
{
    public Users Winner { get; set; }
    public Users Loser { get; set; }
}
