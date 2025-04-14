using Sandbox;
using System.Threading.Tasks;

public sealed class FishingState : Component
{
	[Rpc.Host] // ✅ Only runs on host when client calls it
	public async void RequestCastRpc(Vector3 playerPosition)
	{
		Log.Info($"🎣 [HOST] Cast requested by {Rpc.Caller?.DisplayName} at {playerPosition}");

		var spot = FindNearbyFishingSpot(playerPosition);
		if ( spot == null )
		{
			Log.Warning("❌ [HOST] No fishing spot in range.");
			NotifyCatchRpc("Nothing", 0); // Still notify to reset casting state
			return;
		}

		Log.Info($"✅ [HOST] Spot found at {spot.Transform.Position}. Starting fishing...");
		await Task.DelaySeconds(5f);

		var fish = FishLootTable.GenerateFish();
		Log.Info($"🎯 [HOST] Caught {fish.Name} worth ${fish.Value}");

		var playerState = GameObject.Components.Get<PlayerState>();
		if ( playerState != null )
		{
			playerState.AddMoney(fish.Value);
			Log.Info($"💰 [HOST] Added ${fish.Value} to PlayerState");
		}
		else
		{
			Log.Warning("⚠ [HOST] PlayerState not found on GameObject.");
		}

		NotifyCatchRpc(fish.Name, fish.Value);
	}

	[Rpc.Owner] // ✅ Sent only to owner of this component’s GameObject
	public void NotifyCatchRpc(string fishName, int value)
	{
		if ( value > 0 )
			Log.Info($"🎣 [CLIENT] You caught a {fishName} worth ${value}!");
		else
			Log.Info($"🎣 [CLIENT] You didn’t catch anything.");

		var rod = GameObject.Components.Get<FishingRod>();
		if ( rod != null )
		{
			rod.SetCastingComplete();
			Log.Info("[CLIENT] Fishing rod state reset.");
		}
		else
		{
			Log.Warning("⚠ [CLIENT] FishingRod not found.");
		}
	}

	private FishingSpot FindNearbyFishingSpot(Vector3 playerPosition)
	{
		foreach ( var spot in Scene.GetAllComponents<FishingSpot>() )
		{
			var dist = Vector3.DistanceBetween(playerPosition, spot.Transform.Position);
			Log.Info($"[DEBUG] Spot at {spot.Transform.Position}, dist: {dist}");

			if ( dist <= spot.CastRadius )
			{
				Log.Info("[DEBUG] Found valid fishing spot!");
				return spot;
			}
		}

		return null;
	}
}
