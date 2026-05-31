using Godot;
using HydroventSim.Enums;
using HydroventSim.AI;

namespace HydroventSim.Organisms
{
    public partial class ChemosyntheticBacteria : OrganismBase
    {
        [Export] public float ChemosynthesisRate = 2f;
        [Export] public float DivisionRadius = 20f;

        public override void _Ready()
        {
            OrganismType = OrganismType.ChemosyntheticBacteria;
            TrophicLevel = TrophicLevel.Producer;
            MaxEnergy = 50f;
            Energy = 25f;
            Metabolism = 0.3f;
            ReproductionThreshold = 40f;
            DeathThreshold = 5f;
            OptimalTemperatureMin = 40f;
            OptimalTemperatureMax = 80f;
            MovementSpeed = 5f;
            MaxAge = 60f;
            ChemicalWeight = 0.7f;
            MigrationThreshold = 0.35f;

            SetColor(new Color(0f, 0.8f, 0.4f));
            Scale = new Vector2(0.4f, 0.4f);

            base._Ready();
            AddToGroup("Bacteria");
        }

        protected override void InitializeBehaviorTree()
        {
            var root = new SelectorNode();

            var migrationSequence = new SequenceNode();
            migrationSequence.AddChild(new ConditionNode(ShouldMigrate));
            migrationSequence.AddChild(new ActionNode(ExecuteMigration));

            var reproductionSequence = new SequenceNode();
            reproductionSequence.AddChild(new ConditionNode(() => Energy >= ReproductionThreshold));
            reproductionSequence.AddChild(new ActionNode(_ =>
            {
                Reproduce();
                return NodeStatus.Success;
            }));

            var chemosynthesisAction = new ActionNode(delta =>
            {
                CurrentState = BehaviorState.Feeding;
                PerformChemosynthesis(delta);
                return NodeStatus.Success;
            });

            root.AddChild(migrationSequence);
            root.AddChild(reproductionSequence);
            root.AddChild(chemosynthesisAction);

            BehaviorTree = root;
        }

        private void PerformChemosynthesis(float deltaTime)
        {
            if (Map == null) return;

            float chemicalEnergy = Map.GetChemicalEnergy(Position);
            float temp = Map.GetTemperature(Position);

            float tempFactor = 1f;
            if (temp < OptimalTemperatureMin || temp > OptimalTemperatureMax)
            {
                tempFactor = 0.5f;
            }

            float energyGained = chemicalEnergy * ChemosynthesisRate * deltaTime * 0.01f * tempFactor;

            if (energyGained > 0)
            {
                Map.ConsumeChemicals((int)(Position.X / Map.CellSize), (int)(Position.Y / Map.CellSize), energyGained * 0.1f);
                GainEnergy(energyGained);
            }
        }

        protected override string GetScenePath()
        {
            return "res://Scenes/Organisms/Bacteria.tscn";
        }
    }
}
