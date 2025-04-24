using Sandbox;
using Sandbox.UI;
using IdleWaters;
using System.Threading.Tasks;

namespace IdleWaters;
public sealed class HudInitializer : Component
{
    private GameObject hudObject;

    protected override async void OnStart()
    {
        // Wait for client to exist
        while ( Connection.Local == null )
            await Task.DelayRealtimeSeconds( 0.1f );

        hudObject = new GameObject
        {
            Name        = "HudRoot",
            NetworkMode = NetworkMode.Never
        };

        hudObject.Components.Create<ScreenPanel>();

        // your existing money display
        hudObject.Components.Create<PlayerMoneyHud>();

        // <-- Add your inventory panel here
        hudObject.Components.Create<InventoryHud>();
    }

    protected override void OnDestroy()
    {
        hudObject?.Destroy();
        hudObject = null;
    }
}
