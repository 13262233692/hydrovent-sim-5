using Godot;
using HydroventSim.Enums;
using HydroventSim.AI;
using System.Linq;

namespace HydroventSim.Organisms
{
    public partial class Predator : OrganismBase
    {
        [Export] public float SenseRange = 200f;
        [Export] public float AttackRange = 40f;
        [Export] public float AttackDamage = 30f;

        private OrganismBase _targetPrey;

        public override void _Ready()
        {
            OrganismType = OrganismType.Predator;
            TrophicLevel = TrophicLevel.TopPredator;
            MaxEnergy = 200f;
            Energy = 100f;
            Metabolism = 2f;
            ReproductionThreshold = 180f;
            DeathThreshold = 15f;
            OptimalTemperatureMin = 10f;
            OptimalTemperatureMax = 40f;
            MovementSpeed = 80f;
            MaxAge = 150f;
            ChemicalWeight = 0.1f;
            MigrationThreshold = 0.25f;

            SetColor(new Color(0.6f, 0.1f, 0.6f));
            Scale = new Vector2(1f, 1f);

            base._Ready();
            AddToGroup("Predators");
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

            var huntSequence = new SequenceNode();
            huntSequence.AddChild(new ConditionNode(() => Energy < MaxEnergy * 0.6f));
            huntSequence.AddChild(new ConditionNode(FindPrey));
            huntSequence.AddChild(new ActionNode(MoveToPrey));
            huntSequence.AddChild(new ActionNode(AttackPrey));

            var wanderAction = new ActionNode(delta =>
            {
                CurrentState = BehaviorState.Idle;
                RandomWander(delta);
                return NodeStatus.Success;
            });

            root.AddChild(migrationSequence);
            root.AddChild(reproductionSequence);
            root.AddChild(huntSequence);
            root.AddChild(wanderAction);

            BehaviorTree = root;
        }

        private bool FindPrey()
        {
            var shrimp = GetTree().GetNodesInGroup("Shrimp")
                .Cast<Node2D>()
                .Where(n => n is BlindShrimp s && s.IsAlive())
                .Cast<OrganismBase>();

            var tubeworms = GetTree().GetNodesInGroup("TubeWorms")
                .Cast<Node2D>()
                .Where(n => n is TubeWorm t && t.IsAlive())
                .Cast<OrganismBase>();

            var prey = shrimp.Concat(tubeworms)
                .OrderBy(n => Position.DistanceTo(n.Position))
                .FirstOrDefault();

            if (prey != null && Position.DistanceTo(prey.Position) < SenseRange)
            {
                _targetPrey = prey;
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
            GainEnergy(nutrition * 0.85f);
            _targetPrey = null;

            return NodeStatus.Success;
        }

        protected override string GetScenePath()
        {
            return "res://Scenes/Organisms/Predator.tscn";
        }
    }
}
