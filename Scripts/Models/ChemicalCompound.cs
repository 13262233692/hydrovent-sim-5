namespace HydroventSim.Models
{
    public class ChemicalCompound
    {
        public float HydrogenSulfide { get; set; }
        public float Methane { get; set; }
        public float Ammonia { get; set; }
        public float Oxygen { get; set; }

        public ChemicalCompound()
        {
            HydrogenSulfide = 0f;
            Methane = 0f;
            Ammonia = 0f;
            Oxygen = 0f;
        }

        public ChemicalCompound(float h2s, float ch4, float nh3, float o2)
        {
            HydrogenSulfide = h2s;
            Methane = ch4;
            Ammonia = nh3;
            Oxygen = o2;
        }

        public static ChemicalCompound operator +(ChemicalCompound a, ChemicalCompound b)
        {
            return new ChemicalCompound(
                a.HydrogenSulfide + b.HydrogenSulfide,
                a.Methane + b.Methane,
                a.Ammonia + b.Ammonia,
                a.Oxygen + b.Oxygen
            );
        }

        public static ChemicalCompound operator *(ChemicalCompound a, float scalar)
        {
            return new ChemicalCompound(
                a.HydrogenSulfide * scalar,
                a.Methane * scalar,
                a.Ammonia * scalar,
                a.Oxygen * scalar
            );
        }

        public float GetTotalEnergy()
        {
            return (HydrogenSulfide * 10f) + (Methane * 8f) + (Ammonia * 5f);
        }
    }
}
