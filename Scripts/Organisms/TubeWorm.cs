using Godot;
using HydroventSim.Enums;
using HydroventSim.AI;
using System.Linq;

namespace HydroventSim.Organisms
{
    public partial class TubeWorm : OrganismBase
    {
        [Export] public float FilterRate = 3f;
        [Export] public float FeedingRange = 50f;

        private int _nearbyBacteriaCount = 0;

        public override void _Ready()
        {
            OrganismType = OrganismType.TubeWorm;
            TrophicLevel = TrophicLevel.PrimaryConsumer;
            MaxEnergy = 150f;
            Energy = 75f;
            Metabolism = 0.8f;
            ReproductionThreshold = 120f;
            DeathThreshold = 10f;
            OptimalTemperatureMin = 20f;
            OptimalTemperatureMax = 60f;
            MovementSpeed = 0f;
            MaxAge = 120f;

            SetColor(new Color(1f, 0.3f, 0.1f));
            Scale = new Vector2(0.8f, 1.2f);

            base._Ready();
            AddToGroup("TubeWorms");
        }

        protected override void InitializeBehaviorTree()
        {
            var root = new SelectorNode();

            var reproductionSequence = new SequenceNode();
            reproductionSequence.AddChild(new ConditionNode(() => Energy >= ReproductionThreshold));
            reproductionSequence.AddChild(new ActionNode(_ =>
            {
                Reproduce();
                return NodeStatus.Success;
            }));

            var feedingAction = new ActionNode(delta =>
            {
                CurrentState = BehaviorState.Feeding;
                FilterFeed(delta);
                return NodeStatus.Success;
            });

            root.AddChild(reproductionSequence);
            root.AddChild(feedingAction);

            BehaviorTree = root;
        }

        private void FilterFeed(float deltaTime)
        {
            if (Map == null) return;

            var bacteria = GetTree().GetNodesInGroup("Bacteria")
                .Cast<Node2D>()
                .Where(n => n is ChemosyntheticBacteria bact && bact.IsAlive())
                .Cast<ChemosyntheticBacteria>()
                .ToList();

            _nearbyBacteriaCount = 0;
            float totalEnergyGained = 0f;

            foreach (var bact in bacteria)
            {
                float distance = Position.DistanceTo(bact.Position);
                if (distance < FeedingRange && bact.IsAlive())
                {
                    _nearbyBacteriaCount++;
                    if (bact.Energy > 10f)
                    {
                        float energyToTake = Mathf.Min(bact.Energy * 0.1f, FilterRate * deltaTime);
                        float energyTaken = bact.TakeEnergy(energyToTake);
                        totalEnergyGained += energyTaken * 0.8f;
                    }
                }
            }

            if (totalEnergyGained > 0)
            {
                GainEnergy(totalEnergyGained);
            }
        }

        protected override string GetScenePath()
        {
            return "res://Scenes/Organisms/TubeWorm.tscn";
        }
    }
}
