using System.Collections.Generic;
using HydroventSim.Enums;

namespace HydroventSim.Models
{
    public class PopulationStats
    {
        public Dictionary<OrganismType, int> PopulationCounts { get; private set; }
        public Dictionary<OrganismType, float> AverageEnergy { get; private set; }
        public Dictionary<OrganismType, float> BirthRates { get; private set; }
        public Dictionary<OrganismType, float> DeathRates { get; private set; }

        public int TotalPopulation { get; private set; }
        public float TotalBiomass { get; private set; }
        public float EcosystemHealth { get; private set; }

        public PopulationStats()
        {
            PopulationCounts = new Dictionary<OrganismType, int>();
            AverageEnergy = new Dictionary<OrganismType, float>();
            BirthRates = new Dictionary<OrganismType, float>();
            DeathRates = new Dictionary<OrganismType, float>();

            foreach (OrganismType type in System.Enum.GetValues(typeof(OrganismType)))
            {
                PopulationCounts[type] = 0;
                AverageEnergy[type] = 0f;
                BirthRates[type] = 0f;
                DeathRates[type] = 0f;
            }
        }

        public void Update(Dictionary<OrganismType, List<OrganismData>> organisms)
        {
            TotalPopulation = 0;
            TotalBiomass = 0f;

            foreach (var kvp in organisms)
            {
                OrganismType type = kvp.Key;
                List<OrganismData> list = kvp.Value;

                PopulationCounts[type] = list.Count;
                TotalPopulation += list.Count;

                float totalEnergy = 0f;
                foreach (var org in list)
                {
                    totalEnergy += org.Energy;
                    TotalBiomass += org.Energy * 0.1f;
                }
                AverageEnergy[type] = list.Count > 0 ? totalEnergy / list.Count : 0f;
            }

            CalculateEcosystemHealth();
        }

        private void CalculateEcosystemHealth()
        {
            bool hasProducers = PopulationCounts[OrganismType.ChemosyntheticBacteria] > 0;
            bool hasPrimary = PopulationCounts[OrganismType.TubeWorm] > 0 || PopulationCounts[OrganismType.BlindShrimp] > 0;
            bool hasPredators = PopulationCounts[OrganismType.Predator] > 0;

            float diversityScore = 0f;
            foreach (var count in PopulationCounts.Values)
            {
                if (count > 0) diversityScore += 1f;
            }
            diversityScore /= 4f;

            float balanceScore = 1f;
            if (hasProducers && hasPrimary)
            {
                float producerCount = PopulationCounts[OrganismType.ChemosyntheticBacteria];
                float primaryCount = PopulationCounts[OrganismType.TubeWorm] + PopulationCounts[OrganismType.BlindShrimp];
                if (primaryCount > 0)
                {
                    float ratio = producerCount / primaryCount;
                    if (ratio > 10f) balanceScore = 10f / ratio;
                    else if (ratio < 2f) balanceScore = ratio / 2f;
                    else balanceScore = 1f;
                }
            }

            EcosystemHealth = (diversityScore * 0.4f + balanceScore * 0.6f) * 100f;
        }
    }

    public class OrganismData
    {
        public OrganismType Type { get; set; }
        public float Energy { get; set; }
        public float Health { get; set; }
        public BehaviorState State { get; set; }
    }
}
