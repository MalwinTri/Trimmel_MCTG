namespace Trimmel_MCTG.DB
{
    public class PackageCard
    {
        public int PackageId { get; set; }
        public Guid CardId { get; set; }
        public Package? Package { get; set; }
        public Cards? Card { get; set; }

        public Package Package1
        {
            get => default;
            set
            {
            }
        }

        public PackageCard(int packageId, Guid cardId)
        {
            PackageId = packageId;
            CardId = cardId;
        }

        public override string ToString()
        {
            return $"Package ID: {PackageId}, Card ID: {CardId}";
        }
    }
}
