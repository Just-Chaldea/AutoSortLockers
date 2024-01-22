using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace AutosortLockers
{
    [Menu("Autosort Lockers")]
    public class Config : ConfigFile
    {
        [Toggle("Easy Build")]
        public bool EasyBuild { get; set; } = false;
        [Slider(DefaultValue = 1, Label = "Sort Interval (seconds)", Min = 1, Max = 60, Step = 1)]
        public float SortInterval { get; set; } = 1.0f;
        [Toggle("Show All Items")]
        public bool ShowAllItems { get; set; } = false;
        [Slider(DefaultValue = 5, Label = "Autosorter Width", Min = 1, Max = 12, Step = 1)]
        public int AutosorterWidth { get; set; } = 5;
        [Slider(DefaultValue = 6, Label = "Autosorter Height", Min = 1, Max = 12, Step = 1)]
        public int AutosorterHeight { get; set; } = 6;
        [Slider(DefaultValue = 6, Label = "Receptacle Width", Min = 1, Max = 12, Step = 1)]
        public int ReceptacleWidth { get; set; } = 6;
        [Slider(DefaultValue = 8, Label = "Receptacle Height", Min = 1, Max = 12, Step = 1)]
        public int ReceptacleHeight { get; set; } = 8;
        [Slider(DefaultValue = 6, Label = "Standing Receptacle Width", Min = 1, Max = 12, Step = 1)]
        public int StandingReceptacleWidth { get; set; } = 6;
        [Slider(DefaultValue = 8, Label = "Standing Receptacle Height", Min = 1, Max = 12, Step = 1)]
        public int StandingReceptacleHeight { get; set; } = 8;
    }
}
