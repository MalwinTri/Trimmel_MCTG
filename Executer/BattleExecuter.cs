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
            // Hier kannst du z.B. den Dateinamen definieren.
            // Oder du nimmst Pfad + Datum, z.B. "battle_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            string logFilePath = "battle_log.txt";

            // -------------------------------------------------------
            // Immer zu Beginn: Clear oder anfügen? => Je nach Wunsch
            // Falls du eine leere Datei haben willst, nimm:
            // File.WriteAllText(logFilePath, string.Empty);

            string username = ExtractUsernameFromToken(requestContext.Token);
            var user = Users.LoadFromDatabase(db, username);

            if (user == null)
            {
                response.Payload = "User not found.";
                response.StatusCode = StatusCode.NotFound;
                return response;
            }

            // Schrei in den Log, wer sich gemeldet hat
            battleLog.AppendLine($"[INFO] {DateTime.Now:HH:mm:ss} - User '{user.Username}' joined battles.");

            lock (waitingPlayers)
            {
                if (waitingPlayers.Count == 0)
                {
                    waitingPlayers.Enqueue(user.UserId);
                    battleLog.AppendLine($"[INFO] {DateTime.Now:HH:mm:ss} - 1st player waiting: {user.Username}");

                    response.Payload = "Warten auf zweiten Spieler.";
                    response.StatusCode = StatusCode.Accepted;

                    // Log in Datei sichern
                    SaveLogsToFile(logFilePath);

                    return response;
                }
                else
                {
                    int opponentId = waitingPlayers.Dequeue();
                    var opponent = Users.LoadFromDatabase(db, opponentId);

                    battleLog.AppendLine($"[INFO] {DateTime.Now:HH:mm:ss} - 2nd player found: {opponent.Username}. Battle starting...");

                    StartBattle(user, opponent);

                    response.Payload = "Kampf gestartet!";
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
            battleLog.AppendLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {ex.Message}");
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
        var result = DoBattle(player1, deck1, player2, deck2);

        Console.WriteLine($"{result.Winner.Username} hat gewonnen!");

        // Stats anpassen (Wins, Losses, Elo)
        UpdateStats(result.Winner, true);
        UpdateStats(result.Loser, false);

        // **Coins und Karten austauschen** (Siehe EndBattle-Logik in DoBattle)
    }

    // ---------------------------------------
    // Das Herzstück der erweiterten Kampf-Logik
    // mit Deck & Hand-Mechanik
    private BattleResult DoBattle(Users p1, List<Cards> deck1, Users p2, List<Cards> deck2)
    {
        // Jede Runde haben die Spieler eine "Hand" mit bis zu 4 Karten
        var hand1 = new List<Cards>();
        var hand2 = new List<Cards>();

        // Vor dem Kampf kann man sicherstellen, dass das Deck max. 20 Karten hat:
        // (Falls du an anderer Stelle sicherstellst, dass es max. 20 sind, kannst du es weglassen.)
        if (deck1.Count > 20) deck1 = deck1.Take(20).ToList();
        if (deck2.Count > 20) deck2 = deck2.Take(20).ToList();

        // Zieht anfangs bis zu 4 Karten auf die Hand
        DrawHand(deck1, hand1);
        DrawHand(deck2, hand2);

        while (true)
        {
            // Check, ob jemand bereits verloren hat (Deck + Hand leer)
            if (deck1.Count == 0 && hand1.Count == 0)
            {
                // p1 hat verloren, p2 gewinnt
                return EndBattle(p2, p1, deck2, deck1);
            }
            if (deck2.Count == 0 && hand2.Count == 0)
            {
                // p2 hat verloren, p1 gewinnt
                return EndBattle(p1, p2, deck1, deck2);
            }

            // Vor jeder Runde erneut auf 4 Handkarten auffüllen (falls im Deck noch was ist)
            DrawHand(deck1, hand1);
            DrawHand(deck2, hand2);

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
                break;
            }

            // 2) Schaden berechnen
            double dmg1 = CalculateDamage(p1Monster, p1Spells);
            double dmg2 = CalculateDamage(p2Monster, p2Spells);

            dmg1 = ApplyElementModifier(dmg1, p1Monster.ElementType, p2Monster.ElementType);
            dmg2 = ApplyElementModifier(dmg2, p2Monster.ElementType, p1Monster.ElementType);

            Console.WriteLine($"-> {p1.Username} spielt {p1Monster.Name} + {p1Spells.Count} Spells => {dmg1:F2}");
            Console.WriteLine($"-> {p2.Username} spielt {p2Monster.Name} + {p2Spells.Count} Spells => {dmg2:F2}");

            // 3) Runden-Ergebnis
            if (Math.Abs(dmg1 - dmg2) < 0.001)
            {
                Console.WriteLine("   -> Unentschieden! Keine Karte wird entfernt.");
            }
            else if (dmg1 > dmg2)
            {
                Console.WriteLine($"   -> {p1.Username} gewinnt die Runde! {p2Monster.Name} wird entfernt.");
                hand2.Remove(p2Monster);
            }
            else
            {
                Console.WriteLine($"   -> {p2.Username} gewinnt die Runde! {p1Monster.Name} wird entfernt.");
                hand1.Remove(p1Monster);
            }

            Console.WriteLine();
        }

        // Falls wir aus while(true) rausfallen, definieren wir einen Default:
        return EndBattle(p1, p2, deck1, deck2);
    }

    // ---------------------------------------
    // Zieht so lange Karten vom Deck, bis Hand 4 Karten hat (oder Deck leer)
    private void DrawHand(List<Cards> deck, List<Cards> hand)
    {
        while (hand.Count < 4 && deck.Count > 0)
        {
            // Ziehe "oberste" Karte (oder random) aus dem Deck
            var card = deck[0];
            deck.RemoveAt(0);
            hand.Add(card);
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

        var rand = new Random();

        // 1) Monster wählen
        var monster = monsters[rand.Next(monsters.Count)];

        // 2) 0..2 Spells
        int numSpells = rand.Next(3); // 0, 1, oder 2
        var chosenSpells = new List<Cards>();

        for (int i = 0; i < numSpells && spells.Count > 0; i++)
        {
            int idx = rand.Next(spells.Count);
            chosenSpells.Add(spells[idx]);
            // Falls du einen Spell nach dem Ausspielen verbrauchen willst, 
            // könntest du ihn hier aus 'hand' entfernen:
            // hand.Remove(spells[idx]);
            // spells.RemoveAt(idx);
        }

        return (monster, chosenSpells);
    }

    // ---------------------------------------
    private double CalculateDamage(Cards monster, List<Cards> spells)
    {
        if (monster == null) return 0;
        double totalDamage = monster.Damage;

        if (spells != null)
        {
            foreach (var s in spells)
            {
                // +20% pro Spell
                totalDamage *= 1.2;
            }
        }
        return totalDamage;
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
