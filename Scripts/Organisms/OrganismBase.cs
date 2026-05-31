using Godot;
using HydroventSim.Enums;
using HydroventSim.World;
using HydroventSim.AI;
using HydroventSim.Events;

namespace HydroventSim.Organisms
{
    public abstract class OrganismBase : Node2D
    {
        [Export] public float MaxEnergy { get; protected set; } = 100f;
        [Export] public float Energy { get; protected set; } = 50f;
        [Export] public float Metabolism { get; protected set; } = 1f;
        [Export] public float ReproductionThreshold { get; protected set; } = 80f;
        [Export] public float DeathThreshold { get; protected set; } = 5f;
        [Export] public float OptimalTemperatureMin { get; protected set; } = 20f;
        [Export] public float OptimalTemperatureMax { get; protected set; } = 100f;
        [Export] public float MovementSpeed { get; protected set; } = 20f;
        [Export] public float ChemicalWeight { get; protected set; } = 0.3f;
        [Export] public float MigrationThreshold { get; protected set; } = 0.3f;

        public OrganismType OrganismType { get; protected set; }
        public TrophicLevel TrophicLevel { get; protected set; }
        public BehaviorState CurrentState { get; protected set; }

        protected BehaviorNode BehaviorTree;
        protected HydrothermalMap Map;
        protected Vector2 TargetPosition;
        protected Vector2 MigrationTarget;
        protected float Age = 0f;
        protected float MaxAge = 100f;
        protected RandomNumberGenerator Rng = new RandomNumberGenerator();
        protected float HabitatCheckCooldown = 0f;

        private Sprite2D _sprite;
        private Color _organismColor;

        public override void _Ready()
        {
            Rng.Randomize();
            SetupSprite();
            InitializeBehaviorTree();
            CurrentState = BehaviorState.Idle;
            TargetPosition = Position;
        }

        protected virtual void SetupSprite()
        {
            _sprite = new Sprite2D();
            AddChild(_sprite);

            var texture = new ImageTexture();
            var image = Image.Create(32, 32, false, Image.Format.Rgba8);
            image.Fill(_organismColor);
            texture.SetImage(image);
            _sprite.Texture = texture;
        }

        protected void SetColor(Color color)
        {
            _organismColor = color;
            if (_sprite != null)
            {
                var texture = new ImageTexture();
                var image = Image.Create(32, 32, false, Image.Format.Rgba8);
                image.Fill(color);
                texture.SetImage(image);
                _sprite.Texture = texture;
            }
        }

        public void SetMap(HydrothermalMap map)
        {
            Map = map;
        }

        protected abstract void InitializeBehaviorTree();

        public override void _Process(double delta)
        {
            float deltaFloat = (float)delta;

            Age += deltaFloat;
            if (Age >= MaxAge)
            {
                Die();
                return;
            }

            ConsumeEnergy(Metabolism * deltaFloat);
            ApplyTemperatureEffect(deltaFloat);

            HabitatCheckCooldown = Mathf.Max(0, HabitatCheckCooldown - deltaFloat);

            if (Energy <= DeathThreshold)
            {
                Die();
                return;
            }

            BehaviorTree?.Execute(deltaFloat);
        }

        protected virtual void ConsumeEnergy(float amount)
        {
            Energy = Mathf.Max(0, Energy - amount);
        }

        protected virtual void GainEnergy(float amount)
        {
            Energy = Mathf.Min(MaxEnergy, Energy + amount);
        }

        private void ApplyTemperatureEffect(float deltaTime)
        {
            float temp = Map?.GetTemperature(Position) ?? 20f;
            float stress = 0f;

            if (temp < OptimalTemperatureMin)
            {
                stress = (OptimalTemperatureMin - temp) * 0.01f;
            }
            else if (temp > OptimalTemperatureMax)
            {
                stress = (temp - OptimalTemperatureMax) * 0.005f;
            }

            ConsumeEnergy(stress * deltaTime);
        }

        protected void MoveTowards(Vector2 target, float deltaTime)
        {
            Vector2 direction = (target - Position).Normalized();
            Position += direction * MovementSpeed * deltaTime;
        }

        protected void RandomWander(float deltaTime)
        {
            if (Position.DistanceTo(TargetPosition) < 10f)
            {
                TargetPosition = new Vector2(
                    Rng.RandfRange(50, Map.Width * Map.CellSize - 50),
                    Rng.RandfRange(50, Map.Height * Map.CellSize - 50)
                );
            }
            MoveTowards(TargetPosition, deltaTime);
        }

        protected virtual void Reproduce()
        {
            Energy *= 0.5f;

            var scene = GD.Load<PackedScene>(GetScenePath());
            if (scene != null)
            {
                var child = scene.Instantiate<OrganismBase>();
                child.Position = Position + new Vector2(Rng.RandfRange(-30, 30), Rng.RandfRange(-30, 30));
                child.Energy = Energy * 0.5f;
                child.SetMap(Map);
                GetParent().AddChild(child);

                EventSystem.Raise(EventNames.OrganismBorn, this, new OrganismEventArgs(OrganismType, 1, Energy));
            }
        }

        protected abstract string GetScenePath();

        protected virtual void Die()
        {
            CurrentState = BehaviorState.Dead;
            EventSystem.Raise(EventNames.OrganismDied, this, new OrganismEventArgs(OrganismType, 1, Energy));
            QueueFree();
        }

        public virtual float GetNutritionalValue()
        {
            return Energy * 0.8f;
        }

        public void BeEaten()
        {
            Die();
        }

        public float TakeEnergy(float amount)
        {
            float actualAmount = Mathf.Min(Energy, amount);
            Energy = Mathf.Max(0, Energy - actualAmount);

            if (Energy <= DeathThreshold)
            {
                Die();
            }

            return actualAmount;
        }

        public bool IsAlive()
        {
            return CurrentState != BehaviorState.Dead && IsInstanceValid(this);
        }

        public float EvaluateCurrentHabitat()
        {
            if (Map == null) return 0.5f;
            return Map.EvaluateHabitatAt(Position, OptimalTemperatureMin, OptimalTemperatureMax, ChemicalWeight);
        }

        public bool ShouldMigrate()
        {
            if (MovementSpeed <= 0f) return false;
            if (HabitatCheckCooldown > 0f) return false;
            HabitatCheckCooldown = 3f;
            return EvaluateCurrentHabitat() < MigrationThreshold;
        }

        public NodeStatus ExecuteMigration(float deltaTime)
        {
            CurrentState = BehaviorState.Migrating;

            if (MigrationTarget == Vector2.Zero || Position.DistanceTo(MigrationTarget) < 15f)
            {
                if (Map != null)
                {
                    MigrationTarget = Map.GetBestHabitatPosition(
                        Position, OptimalTemperatureMin, OptimalTemperatureMax, ChemicalWeight);
                }
                else
                {
                    MigrationTarget = Position + new Vector2(Rng.RandfRange(-200, 200), Rng.RandfRange(-200, 200));
                }
            }

            MoveTowards(MigrationTarget, deltaTime);

            if (EvaluateCurrentHabitat() > MigrationThreshold + 0.2f)
            {
                MigrationTarget = Vector2.Zero;
                CurrentState = BehaviorState.Idle;
                return NodeStatus.Success;
            }

            return NodeStatus.Running;
        }

        public void ReceiveEruptionDamage(Vector2 eruptionCenter, float eruptionRadius, float intensity)
        {
            float distance = Position.DistanceTo(eruptionCenter);
            if (distance >= eruptionRadius) return;

            float damageRatio = (1f - distance / eruptionRadius) * intensity;
            float damage = MaxEnergy * damageRatio * 0.8f;
            TakeEnergy(damage);
        }
    }
}
