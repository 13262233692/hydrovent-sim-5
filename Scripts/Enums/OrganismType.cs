namespace HydroventSim.Enums
{
    public enum OrganismType
    {
        ChemosyntheticBacteria,
        TubeWorm,
        BlindShrimp,
        Predator
    }

    public enum TrophicLevel
    {
        Producer,
        PrimaryConsumer,
        SecondaryConsumer,
        TopPredator
    }

    public enum BehaviorState
    {
        Idle,
        SeekingFood,
        Feeding,
        Reproducing,
        Fleeing,
        Migrating,
        Dead
    }
}
