using Godot;
using HydroventSim.Enums;
using HydroventSim.Models;
using HydroventSim.Events;

namespace HydroventSim.UI
{
    public partial class StatsPanel : Control
    {
        private Label _titleLabel;
        private Label _bacteriaCountLabel;
        private Label _tubewormCountLabel;
        private Label _shrimpCountLabel;
        private Label _predatorCountLabel;
        private Label _totalPopulationLabel;
        private Label _totalBiomassLabel;
        private Label _ecosystemHealthLabel;
        private Label _eruptionEventLabel;

        private Game _game;
        private float _eruptionMessageTimer = 0f;
        private const float EruptionMessageDuration = 8f;

        public override void _Ready()
        {
            SetupUI();
            _game = GetTree().Root.GetNode<Game>("Main/Game");

            EventSystem.Subscribe<EruptionEventArgs>(EventNames.EruptionEvent, OnEruptionEvent);
        }

        private void SetupUI()
        {
            var background = new ColorRect();
            background.Color = new Color(0, 0, 0, 0.7f);
            background.AnchorRight = 1;
            background.AnchorBottom = 0;
            background.OffsetRight = 250;
            background.OffsetBottom = 260;
            AddChild(background);

            var vbox = new VBoxContainer();
            vbox.Position = new Vector2(10, 10);
            AddChild(vbox);

            _titleLabel = CreateLabel("=== 种群统计 ===", new Color(0.8f, 0.9f, 1f));
            _titleLabel.AddThemeFontSizeOverride("font_size", 16);
            vbox.AddChild(_titleLabel);

            _bacteriaCountLabel = CreateLabel("细菌: 0", new Color(0f, 0.8f, 0.4f));
            vbox.AddChild(_bacteriaCountLabel);

            _tubewormCountLabel = CreateLabel("管虫: 0", new Color(1f, 0.3f, 0.1f));
            vbox.AddChild(_tubewormCountLabel);

            _shrimpCountLabel = CreateLabel("盲虾: 0", new Color(1f, 0.8f, 0.2f));
            vbox.AddChild(_shrimpCountLabel);

            _predatorCountLabel = CreateLabel("捕食者: 0", new Color(0.6f, 0.1f, 0.6f));
            vbox.AddChild(_predatorCountLabel);

            var separator1 = new HSeparator();
            vbox.AddChild(separator1);

            _totalPopulationLabel = CreateLabel("总种群: 0", Colors.White);
            vbox.AddChild(_totalPopulationLabel);

            _totalBiomassLabel = CreateLabel("总生物量: 0", Colors.White);
            vbox.AddChild(_totalBiomassLabel);

            _ecosystemHealthLabel = CreateLabel("生态健康: 0%", Colors.Green);
            vbox.AddChild(_ecosystemHealthLabel);

            var separator2 = new HSeparator();
            vbox.AddChild(separator2);

            _eruptionEventLabel = CreateLabel("", new Color(1f, 0.4f, 0f));
            _eruptionEventLabel.AddThemeFontSizeOverride("font_size", 12);
            _eruptionEventLabel.Visible = false;
            vbox.AddChild(_eruptionEventLabel);

            var helpLabel = CreateLabel("\n快捷键:\n1-4: 选择生物\n左键: 放置\n空格: 暂停\nT: 温度图\nE: 手动触发喷发", new Color(0.7f, 0.7f, 0.7f));
            helpLabel.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(helpLabel);
        }

        private Label CreateLabel(string text, Color color)
        {
            var label = new Label();
            label.Text = text;
            label.Modulate = color;
            return label;
        }

        public override void _Process(double delta)
        {
            if (_game?.Stats != null)
            {
                UpdateStats(_game.Stats);
            }

            if (_eruptionEventLabel.Visible)
            {
                _eruptionMessageTimer -= (float)delta;
                if (_eruptionMessageTimer <= 0f)
                {
                    _eruptionEventLabel.Visible = false;
                }
                else
                {
                    float alpha = Mathf.Min(1f, _eruptionMessageTimer / 2f);
                    _eruptionEventLabel.Modulate = new Color(1f, 0.4f, 0f, alpha);
                }
            }
        }

        private void UpdateStats(PopulationStats stats)
        {
            _bacteriaCountLabel.Text = $"细菌: {stats.PopulationCounts[OrganismType.ChemosyntheticBacteria]}";
            _tubewormCountLabel.Text = $"管虫: {stats.PopulationCounts[OrganismType.TubeWorm]}";
            _shrimpCountLabel.Text = $"盲虾: {stats.PopulationCounts[OrganismType.BlindShrimp]}";
            _predatorCountLabel.Text = $"捕食者: {stats.PopulationCounts[OrganismType.Predator]}";
            _totalPopulationLabel.Text = $"总种群: {stats.TotalPopulation}";
            _totalBiomassLabel.Text = $"总生物量: {stats.TotalBiomass:F1}";

            float health = stats.EcosystemHealth;
            _ecosystemHealthLabel.Text = $"生态健康: {health:F1}%";
            _ecosystemHealthLabel.Modulate = health > 70f ? Colors.Green : health > 40f ? Colors.Yellow : Colors.Red;
        }

        private void OnEruptionEvent(object sender, EruptionEventArgs e)
        {
            _eruptionEventLabel.Text = $"⚠ {e.Message}";
            _eruptionEventLabel.Visible = true;
            _eruptionMessageTimer = EruptionMessageDuration;
            _eruptionEventLabel.Modulate = new Color(1f, 0.4f, 0f, 1f);
        }

        public override void _ExitTree()
        {
            EventSystem.Unsubscribe<EruptionEventArgs>(EventNames.EruptionEvent, OnEruptionEvent);
        }
    }
}
