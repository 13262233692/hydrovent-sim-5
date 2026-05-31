using Godot;
using HydroventSim.Enums;

namespace HydroventSim.UI
{
    public partial class Toolbar : Control
    {
        private Game _game;
        private Button _bacteriaButton;
        private Button _tubewormButton;
        private Button _shrimpButton;
        private Button _predatorButton;
        private Button _pauseButton;

        public override void _Ready()
        {
            _game = GetTree().Root.GetNode<Game>("Main/Game");
            SetupUI();
        }

        private void SetupUI()
        {
            var hbox = new HBoxContainer();
            hbox.AnchorRight = 1;
            hbox.AnchorBottom = 1;
            hbox.OffsetTop = -50;
            hbox.Alignment = BoxContainer.AlignmentMode.Center;
            AddChild(hbox);

            _bacteriaButton = CreateButton("细菌 (1)", new Color(0f, 0.8f, 0.4f));
            _bacteriaButton.Pressed += () => OnOrganismSelected(OrganismType.ChemosyntheticBacteria);
            hbox.AddChild(_bacteriaButton);

            _tubewormButton = CreateButton("管虫 (2)", new Color(1f, 0.3f, 0.1f));
            _tubewormButton.Pressed += () => OnOrganismSelected(OrganismType.TubeWorm);
            hbox.AddChild(_tubewormButton);

            _shrimpButton = CreateButton("盲虾 (3)", new Color(1f, 0.8f, 0.2f));
            _shrimpButton.Pressed += () => OnOrganismSelected(OrganismType.BlindShrimp);
            hbox.AddChild(_shrimpButton);

            _predatorButton = CreateButton("捕食者 (4)", new Color(0.6f, 0.1f, 0.6f));
            _predatorButton.Pressed += () => OnOrganismSelected(OrganismType.Predator);
            hbox.AddChild(_predatorButton);

            var separator = new VSeparator();
            hbox.AddChild(separator);

            _pauseButton = CreateButton("暂停 (空格)", new Color(0.5f, 0.5f, 0.5f));
            _pauseButton.Pressed += OnPauseToggle;
            hbox.AddChild(_pauseButton);

            OnOrganismSelected(OrganismType.ChemosyntheticBacteria);
        }

        private Button CreateButton(string text, Color color)
        {
            var button = new Button();
            button.Text = text;
            button.CustomMinimumSize = new Vector2(100, 40);
            button.Modulate = color;
            return button;
        }

        private void OnOrganismSelected(OrganismType type)
        {
            _game?.SetSelectedOrganism(type);
            UpdateButtonStates(type);
        }

        private void UpdateButtonStates(OrganismType selected)
        {
            _bacteriaButton.ButtonPressed = selected == OrganismType.ChemosyntheticBacteria;
            _tubewormButton.ButtonPressed = selected == OrganismType.TubeWorm;
            _shrimpButton.ButtonPressed = selected == OrganismType.BlindShrimp;
            _predatorButton.ButtonPressed = selected == OrganismType.Predator;
        }

        private void OnPauseToggle()
        {
            _game?.TogglePause();
            _pauseButton.Text = _game.IsPaused ? "继续 (空格)" : "暂停 (空格)";
        }

        public override void _Process(double delta)
        {
            if (_game != null)
            {
                UpdateButtonStates(_game.GetSelectedOrganism());
                _pauseButton.Text = _game.IsPaused ? "继续 (空格)" : "暂停 (空格)";
            }
        }
    }
}
