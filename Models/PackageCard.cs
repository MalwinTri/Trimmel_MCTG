namespace Trimmel_MCTG.DB
{
    public class PackageCard
    {
        // Eigenschaften
        public int PackageId { get; set; }
        public Guid CardId { get; set; }            // <-- statt int

        public Package? Package { get; set; }             // Verknüpftes Paket
        public Cards? Card { get; set; }                   // Verknüpfte Karte

        // Konstruktoren
        public PackageCard() { }

        public PackageCard(int packageId, Guid cardId)
        {
            PackageId = packageId;
            CardId = cardId;
        }

        // Methode zur Ausgabe der Paketkarteninformationen
        public override string ToString()
        {
            return $"Package ID: {PackageId}, Card ID: {CardId}";
        }
    }
}
