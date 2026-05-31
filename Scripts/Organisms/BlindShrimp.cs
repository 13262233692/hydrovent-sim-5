using Godot;
using HydroventSim.Enums;
using HydroventSim.AI;
using System.Linq;

namespace HydroventSim.Organisms
{
    public partial class BlindShrimp : OrganismBase
    {
        [Export] public float SenseRange = 150f;
        [Export] public float AttackRange = 30f;
        [Export] public float AttackRate = 5f;

        private OrganismBase _targetPrey;

        public override void _Ready()
        {
            OrganismType = OrganismType.BlindShrimp;
            TrophicLevel = TrophicLevel.PrimaryConsumer;
            MaxEnergy = 100f;
            Energy = 50f;
            Metabolism = 1.2f;
            ReproductionThreshold = 80f;
            DeathThreshold = 8f;
            OptimalTemperatureMin = 15f;
            OptimalTemperatureMax = 50f;
            MovementSpeed = 60f;
            MaxAge = 80f;
            ChemicalWeight = 0.3f;
            MigrationThreshold = 0.3f;

            SetColor(new Color(1f, 0.8f, 0.2f));
            Scale = new Vector2(0.6f, 0.6f);

            base._Ready();
            AddToGroup("Shrimp");
        }

        protected override void InitializeBehaviorTree()
        {
            var root = new SelectorNode();

            var fleeSequence = new SequenceNode();
            fleeSequence.AddChild(new ConditionNode(IsPredatorNearby));
            fleeSequence.AddChild(new ActionNode(FleeFromPredator));

            var migrationSequence = new SequenceNode();
            migrationSequence.AddChild(new ConditionNode(ShouldMigrate));
            migrationSequence.AddChild(new ActionNode(ExecuteMigration));

            var reproductionSequence = new SequenceNode();
            reproductionSequence.AddChild(new ConditionNode(() => Energy >= ReproductionThreshold));
            reproductionSequence.AddChild(new ConditionNode(() => !IsPredatorNearby()));
            reproductionSequence.AddChild(new ActionNode(_ =>
            {
                Reproduce();
                return NodeStatus.Success;
            }));

            var huntSequence = new SequenceNode();
            huntSequence.AddChild(new ConditionNode(() => Energy < MaxEnergy * 0.7f));
            huntSequence.AddChild(new ConditionNode(FindPrey));
            huntSequence.AddChild(new ActionNode(MoveToPrey));
            huntSequence.AddChild(new ActionNode(AttackPrey));

            var wanderAction = new ActionNode(delta =>
            {
                CurrentState = BehaviorState.Idle;
                RandomWander(delta);
                return NodeStatus.Success;
            });

            root.AddChild(fleeSequence);
            root.AddChild(migrationSequence);
            root.AddChild(reproductionSequence);
            root.AddChild(huntSequence);
            root.AddChild(wanderAction);

            BehaviorTree = root;
        }

        private bool IsPredatorNearby()
        {
            var predators = GetTree().GetNodesInGroup("Predators")
                .Cast<Node2D>()
                .Where(n => n is Predator p && p.IsAlive());

            foreach (var predator in predators)
            {
                if (Position.DistanceTo(predator.Position) < SenseRange * 1.5f)
                {
                    return true;
                }
            }
            return false;
        }

        private NodeStatus FleeFromPredator(float deltaTime)
        {
            CurrentState = BehaviorState.Fleeing;
            var predators = GetTree().GetNodesInGroup("Predators")
                .Cast<Node2D>()
                .Where(n => n is Predator p && p.IsAlive());

            Vector2 fleeDirection = Vector2.Zero;
            foreach (var predator in predators)
            {
                float distance = Position.DistanceTo(predator.Position);
                if (distance < SenseRange * 1.5f)
                {
                    fleeDirection += (Position - predator.Position).Normalized() * (SenseRange * 1.5f - distance);
                }
            }

            if (fleeDirection != Vector2.Zero)
            {
                fleeDirection = fleeDirection.Normalized();
                Position += fleeDirection * MovementSpeed * 1.5f * deltaTime;
            }

            return NodeStatus.Running;
        }

        private bool FindPrey()
        {
            var bacteria = GetTree().GetNodesInGroup("Bacteria")
                .Cast<Node2D>()
                .Where(n => n is ChemosyntheticBacteria bact && bact.IsAlive() && bact.Energy > 10f)
                .OrderBy(n => Position.DistanceTo(n.Position))
                .FirstOrDefault();

            if (bacteria != null && Position.DistanceTo(bacteria.Position) < SenseRange)
            {
                _targetPrey = bacteria as OrganismBase;
                return true;
            }
            return false;
        }

        private NodeStatus MoveToPrey(float deltaTime)
        {
            if (_targetPrey == null || !_targetPrey.IsAlive())
            {
                _targetPrey = null;
                return NodeStatus.Failure;
            }

            CurrentState = BehaviorState.SeekingFood;
            MoveTowards(_targetPrey.Position, deltaTime);
            return Position.DistanceTo(_targetPrey.Position) < AttackRange ? NodeStatus.Success : NodeStatus.Running;
        }

        private NodeStatus AttackPrey(float deltaTime)
        {
            if (_targetPrey == null || !_targetPrey.IsAlive())
            {
                _targetPrey = null;
                return NodeStatus.Failure;
            }

            CurrentState = BehaviorState.Feeding;
            float nutrition = _targetPrey.GetNutritionalValue();
            _targetPrey.BeEaten();
            GainEnergy(nutrition * 0.9f);
            _targetPrey = null;

            return NodeStatus.Success;
        }

        protected override string GetScenePath()
        {
            return "res://Scenes/Organisms/Shrimp.tscn";
        }
    }
}
