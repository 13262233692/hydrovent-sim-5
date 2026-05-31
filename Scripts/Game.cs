using Godot;
using System.Collections.Generic;
using System.Linq;
using HydroventSim.World;
using HydroventSim.Models;
using HydroventSim.Events;
using HydroventSim.Enums;
using HydroventSim.Organisms;

namespace HydroventSim
{
    public partial class Game : Node2D
    {
        [Export] public int MapWidth = 100;
        [Export] public int MapHeight = 80;
        [Export] public float CellSize = 10f;
        [Export] public float SimulationSpeed = 1f;
        [Export] public float MinEruptionInterval = 30f;
        [Export] public float MaxEruptionInterval = 90f;
        [Export] public float EruptionRadius = 100f;
        [Export] public float EruptionIntensity = 1.0f;

        public HydrothermalMap Map { get; private set; }
        public PopulationStats Stats { get; private set; }
        public bool IsPaused { get; private set; } = false;
        public string LastEruptionMessage { get; private set; } = "";

        private Node2D _organismContainer;
        private TextureRect _mapOverlay;
        private bool _showTemperatureOverlay = false;
        private OrganismType _selectedOrganism = OrganismType.ChemosyntheticBacteria;

        private float _statsUpdateTimer = 0f;
        private const float StatsUpdateInterval = 1f;

        private float _eruptionTimer;
        private float _nextEruptionTime;
        private RandomNumberGenerator _rng = new RandomNumberGenerator();

        public override void _Ready()
        {
            _rng.Randomize();
            Map = new HydrothermalMap(MapWidth, MapHeight, CellSize);
            Stats = new PopulationStats();
            _organismContainer = new Node2D();
            AddChild(_organismContainer);
            _organismContainer.Name = "Organisms";

            _eruptionTimer = 0f;
            _nextEruptionTime = _rng.RandfRange(MinEruptionInterval, MaxEruptionInterval);

            CreateMapOverlay();
            SubscribeToEvents();
            SpawnInitialOrganisms();
        }

        private void CreateMapOverlay()
        {
            _mapOverlay = new TextureRect();
            _mapOverlay.Size = new Vector2(MapWidth * CellSize, MapHeight * CellSize);
            _mapOverlay.Modulate = new Color(1, 1, 1, 0.3f);
            _mapOverlay.Visible = false;
            AddChild(_mapOverlay);

            UpdateMapOverlay();
        }

        private void UpdateMapOverlay()
        {
            var image = Image.Create(MapWidth, MapHeight, false, Image.Format.Rgba8);

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    float temp = Map.GetTemperature(x, y);
                    float normalized = Mathf.InverseLerp(2f, 350f, temp);
                    var color = Color.FromHsl(normalized * 0.6f, 0.8f, 0.5f);
                    image.SetPixel(x, y, color);
                }
            }

            var texture = ImageTexture.CreateFromImage(image);
            _mapOverlay.Texture = texture;
        }

        private void SubscribeToEvents()
        {
            EventSystem.Subscribe<OrganismEventArgs>(EventNames.OrganismBorn, OnOrganismBorn);
            EventSystem.Subscribe<OrganismEventArgs>(EventNames.OrganismDied, OnOrganismDied);
        }

        private void OnOrganismBorn(object sender, OrganismEventArgs e)
        {
            UpdateStatistics();
        }

        private void OnOrganismDied(object sender, OrganismEventArgs e)
        {
            UpdateStatistics();
        }

        private void SpawnInitialOrganisms()
        {
            var vents = Map.GetVentPositions();

            foreach (var vent in vents)
            {
                Vector2 worldPos = vent * CellSize;

                for (int i = 0; i < 5; i++)
                {
                    SpawnOrganism(OrganismType.ChemosyntheticBacteria, worldPos + new Vector2(
                        GD.RandfRange(-50, 50), GD.RandfRange(-50, 50)));
                }

                for (int i = 0; i < 2; i++)
                {
                    SpawnOrganism(OrganismType.TubeWorm, worldPos + new Vector2(
                        GD.RandfRange(-30, 30), GD.RandfRange(-30, 30)));
                }
            }
        }

        public void SpawnOrganism(OrganismType type, Vector2 position)
        {
            OrganismBase organism = type switch
            {
                OrganismType.ChemosyntheticBacteria => new ChemosyntheticBacteria(),
                OrganismType.TubeWorm => new TubeWorm(),
                OrganismType.BlindShrimp => new BlindShrimp(),
                OrganismType.Predator => new Predator(),
                _ => null
            };

            if (organism != null)
            {
                organism.Position = position;
                organism.SetMap(Map);
                _organismContainer.AddChild(organism);

                EventSystem.Raise(EventNames.OrganismBorn, this, new OrganismEventArgs(type, 1, organism.Energy));
            }
        }

        public override void _Process(double delta)
        {
            float deltaFloat = (float)delta;

            if (!IsPaused)
            {
                Map.Update(deltaFloat * SimulationSpeed);

                _eruptionTimer += deltaFloat * SimulationSpeed;
                if (_eruptionTimer >= _nextEruptionTime)
                {
                    TriggerRandomEruption();
                    _eruptionTimer = 0f;
                    _nextEruptionTime = _rng.RandfRange(MinEruptionInterval, MaxEruptionInterval);
                }

                _statsUpdateTimer += deltaFloat;
                if (_statsUpdateTimer >= StatsUpdateInterval)
                {
                    _statsUpdateTimer = 0f;
                    UpdateStatistics();
                }
            }
        }

        private void UpdateStatistics()
        {
            var organisms = new Dictionary<OrganismType, List<OrganismData>>();

            foreach (OrganismType type in System.Enum.GetValues(typeof(OrganismType)))
            {
                organisms[type] = new List<OrganismData>();
            }

            foreach (var child in _organismContainer.GetChildren())
            {
                if (child is OrganismBase organism && organism.IsAlive())
                {
                    organisms[organism.OrganismType].Add(new OrganismData
                    {
                        Type = organism.OrganismType,
                        Energy = organism.Energy,
                        Health = organism.Energy / organism.MaxEnergy,
                        State = organism.CurrentState
                    });
                }
            }

            Stats.Update(organisms);

            EventSystem.Raise(EventNames.EcosystemHealthChanged, this,
                new EcosystemEventArgs("Health", $"生态系统健康度: {Stats.EcosystemHealth:F1}%", Stats.EcosystemHealth / 100f));
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    Vector2 worldPos = GetGlobalMousePosition();
                    SpawnOrganism(_selectedOrganism, worldPos);
                }
            }

            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Key1:
                        _selectedOrganism = OrganismType.ChemosyntheticBacteria;
                        break;
                    case Key.Key2:
                        _selectedOrganism = OrganismType.TubeWorm;
                        break;
                    case Key.Key3:
                        _selectedOrganism = OrganismType.BlindShrimp;
                        break;
                    case Key.Key4:
                        _selectedOrganism = OrganismType.Predator;
                        break;
                    case Key.Space:
                        IsPaused = !IsPaused;
                        break;
                    case Key.T:
                        _showTemperatureOverlay = !_showTemperatureOverlay;
                        _mapOverlay.Visible = _showTemperatureOverlay;
                        break;
                    case Key.E:
                        TriggerRandomEruption();
                        break;
                }
            }
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        public void SetSelectedOrganism(OrganismType type)
        {
            _selectedOrganism = type;
        }

        public OrganismType GetSelectedOrganism()
        {
            return _selectedOrganism;
        }

        private void TriggerRandomEruption()
        {
            var vents = Map.GetVentPositions();
            int ventIndex = _rng.RandiRange(0, vents.Length - 1);
            Vector2 ventPos = vents[ventIndex];
            Vector2 eruptionCenter = ventPos * CellSize + new Vector2(
                _rng.RandfRange(-30, 30), _rng.RandfRange(-30, 30));

            float radius = EruptionRadius * _rng.RandfRange(0.7f, 1.3f);
            float intensity = EruptionIntensity * _rng.RandfRange(0.8f, 1.2f);

            Map.TriggerEruption(eruptionCenter, radius, intensity);

            int killedCount = 0;
            var organismsToDamage = _organismContainer.GetChildren()
                .OfType<OrganismBase>()
                .Where(o => o.IsAlive())
                .ToList();

            foreach (var organism in organismsToDamage)
            {
                float distance = organism.Position.DistanceTo(eruptionCenter);
                if (distance < radius)
                {
                    float vulnerability = organism.TrophicLevel == TrophicLevel.Producer ? 1.5f :
                                         organism.TrophicLevel == TrophicLevel.PrimaryConsumer ? 1.0f : 0.5f;

                    float damageRatio = (1f - distance / radius) * intensity * vulnerability;
                    float damage = organism.MaxEnergy * damageRatio * 0.8f;
                    organism.TakeEnergy(damage);

                    if (!organism.IsAlive())
                    {
                        killedCount++;
                    }
                }
            }

            string message = $"黑烟囱喷发！区域({eruptionCenter.X:F0},{eruptionCenter.Y:F0}) 半径{radius:F0} 强度{intensity:F1} 致死{killedCount}个生物";
            LastEruptionMessage = message;

            EventSystem.Raise(EventNames.EruptionEvent, this, new EruptionEventArgs(
                eruptionCenter, radius, intensity, message));

            EventSystem.Raise(EventNames.EcosystemHealthChanged, this,
                new EcosystemEventArgs("Eruption", message, intensity));

            GD.Print(message);
        }

        public override void _ExitTree()
        {
            EventSystem.Clear();
        }
    }
}
