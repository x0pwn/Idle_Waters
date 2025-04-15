using Sandbox;
using System.Threading.Tasks;

public sealed class FishingState : Component
{
    [Rpc.Host]
    public async void RequestCastRpc(Vector3 playerPosition)
    {
        Log.Info($"🎣 [HOST] Cast requested by {Rpc.Caller?.DisplayName} (ID: {Rpc.Caller?.Id}, SteamID: {Rpc.Caller?.SteamId})");

        var spot = FindNearbyFishingSpot(playerPosition);
        if (spot == null)
        {
            Log.Warning("❌ [HOST] No fishing spot in range.");
            NotifyCatchRpc("Nothing", 0);
            return;
        }

        Log.Info($"✅ [HOST] Spot found at {spot.Transform.Position}. Starting fishing...");
        await Task.DelaySeconds(5f);

        var fish = FishLootTable.GenerateFish();
        Log.Info($"🎯 [HOST] Caught {fish.Name} worth ${fish.Value}");

        // Get PlayerState from this GameObject (owned by the caller)
        var playerState = GameObject.Components.Get<PlayerState>();
        Log.Info($"[HOST] FishingState GameObject owned by {GameObject.Network.OwnerConnection?.DisplayName} (ID: {GameObject.Network.OwnerConnection?.Id})");

        if (playerState != null)
        {
            playerState.AddMoney(fish.Value); // Calls RPC
            Log.Info($"💰 [HOST] Requested AddMoney ${fish.Value} for {playerState.Network.OwnerConnection?.DisplayName}'s PlayerState");
        }
        else
        {
            Log.Error($"⚠ [HOST] PlayerState not found on GameObject for {Rpc.Caller?.DisplayName} (ID: {Rpc.Caller?.Id})");
        }

        NotifyCatchRpc(fish.Name, fish.Value);
    }

    [Rpc.Owner]
    public void NotifyCatchRpc(string fishName, int value)
    {
        if (value > 0)
            Log.Info($"🎣 [CLIENT] You caught a {fishName} worth ${value}!");
        else
            Log.Info($"🎣 [CLIENT] You didn’t catch anything.");

        var rod = GameObject.Components.Get<FishingRod>();
        if (rod != null)
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
        foreach (var spot in Scene.GetAllComponents<FishingSpot>())
        {
            var dist = Vector3.DistanceBetween(playerPosition, spot.Transform.Position);
            Log.Info($"[DEBUG] Spot at {spot.Transform.Position}, dist: {dist}");

            if (dist <= spot.CastRadius)
            {
                Log.Info("[DEBUG] Found valid fishing spot!");
                return spot;
            }
        }

        return null;
    }
}
