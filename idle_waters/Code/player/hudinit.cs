using Sandbox;
using Sandbox.UI;
using System.Threading.Tasks;

public sealed class HudInitializer : Component
{
	private GameObject hudObject;

	protected override async void OnStart()
	{
		// Wait for local connection to exist
		while (Connection.Local == null)
			await Task.DelayRealtimeSeconds(0.1f);

		Log.Info($"[HUD] Creating HUD for {Connection.Local.DisplayName}");

		// ✅ Create GameObject for HUD
		hudObject = new GameObject();
		hudObject.Name = "HudRoot";
		hudObject.NetworkMode = NetworkMode.Never;

		// ✅ Attach ScreenPanel component
		var screenPanel = hudObject.Components.Create<ScreenPanel>();

		// ✅ Add your PanelComponent Razor HUD to that GameObject
		hudObject.Components.Create<PlayerMoneyHud>();
	}

	protected override void OnDestroy()
	{
		hudObject?.Destroy();
		hudObject = null;
	}
}
