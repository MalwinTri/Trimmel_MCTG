namespace Trimmel_MCTG.helperClass
{
    public static class Effectiveness
    {
        public static double GetDamageMultiplier(string attackerElement, string defenderElement)
        {
            // Effektiv
            if (attackerElement == "water" && defenderElement == "fire")
                return 2.0;
            if (attackerElement == "fire" && defenderElement == "normal")
                return 2.0; // Effektiv
            if (attackerElement == "normal" && defenderElement == "water")
                return 2.0; // Effektiv

            // Nicht effektiv
            if (defenderElement == "water" && attackerElement == "fire")
                return 0.5;
            if (defenderElement == "fire" && attackerElement == "normal")
                return 0.5;
            if (defenderElement == "normal" && attackerElement == "water")
                return 0.5;

            return 1.0; // Keine Wirkung oder gleiche Elemente
        }
    }
}
