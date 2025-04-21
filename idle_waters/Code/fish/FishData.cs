using Sandbox;

namespace IdleWaters
{
    public class FishData : Component
    {
        [Property, Sync] public string FishName { get; set; }
        [Property, Sync] public int FishValue { get; set; }
        [Property, Sync] public string FishIcon { get; set; } = "models/fish2/fish1a.png";
    }
}
