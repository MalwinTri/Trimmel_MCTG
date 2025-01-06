namespace Trimmel_MCTG.DB
{
    public static class Effectiveness
    {
        // Gibt den Multiplikator basierend auf den Elementen zurück
        public static double GetDamageMultiplier(string attackerElement, string defenderElement)
        {
            if (attackerElement == "water" && defenderElement == "fire")
                return 2.0; // Effektiv
            if (attackerElement == "fire" && defenderElement == "normal")
                return 2.0; // Effektiv
            if (attackerElement == "normal" && defenderElement == "water")
                return 2.0; // Effektiv

            if (defenderElement == "water" && attackerElement == "fire")
                return 0.5; // Nicht effektiv
            if (defenderElement == "fire" && attackerElement == "normal")
                return 0.5; // Nicht effektiv
            if (defenderElement == "normal" && attackerElement == "water")
                return 0.5; // Nicht effektiv

            return 1.0; // Keine Wirkung oder gleiche Elemente
        }
    }
}
