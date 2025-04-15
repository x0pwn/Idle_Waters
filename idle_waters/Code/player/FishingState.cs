using Sandbox;
using System.Threading.Tasks;

public sealed class FishingState : Component
{
    [Rpc.Host]
    public async void RequestCastRpc(Vector3 playerPosition)
    {
        Log.Info($"🎣 [HOST] Cast requested by {Rpc.Caller?.DisplayName} (ID: {Rpc.Caller?.Id}, SteamID: {Rpc.Caller?.SteamId}) at {playerPosition}");

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

        // Create a GameObject with FishEntity component, spawning near player
        var fishObject = new GameObject();
        fishObject.Name = $"Fish_{fish.Name}";
        // Spawn 50 units in front of player (based on GameObject's rotation) and 10 units up
        var forward = GameObject.Transform.Rotation.Forward; // Player's facing direction
        fishObject.Transform.Position = playerPosition + forward * 50f + Vector3.Up * 10f;
        fishObject.NetworkMode = NetworkMode.Object;
        var fishEntity = fishObject.Components.Create<FishEntity>();
        fishEntity.FishName = fish.Name;
        fishEntity.FishValue = fish.Value;
        Log.Info($"[HOST] Spawned FishEntity for {fish.Name} at {fishObject.Transform.Position}");

        var playerState = GameObject.Components.Get<PlayerState>();
        Log.Info($"[HOST] FishingState GameObject owned by {GameObject.Network.OwnerConnection?.DisplayName} (ID: {GameObject.Network.OwnerConnection?.Id})");

        if (playerState != null)
        {
            playerState.AddMoney(fish.Value);
            Log.Info($"💰 [HOST] Added ${fish.Value} to {playerState.Network.OwnerConnection?.DisplayName}'s PlayerState");
        }
        else
        {
            Log.Error($"⚠ [HOST] PlayerState not found on GameObject for {Rpc.Caller?.DisplayName} (ID: {Rpc.Caller?.Id})");
        }

        Log.Info("[HOST] Calling NotifyCatchRpc");
        NotifyCatchRpc(fish.Name, fish.Value);
    }

    [Rpc.Owner]
    public void NotifyCatchRpc(string fishName, int value)
    {
        Log.Info($"[CLIENT] NotifyCatchRpc called with {fishName} worth ${value}");

        var hudObject = new GameObject();
        hudObject.Name = "CatchNotification";
        hudObject.NetworkMode = NetworkMode.Never;
        var notification = hudObject.Components.Create<CatchNotification>();
        notification.FishName = fishName;
        notification.FishValue = value;
        Log.Info("[CLIENT] Created CatchNotification");

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
        Log.Info("[HOST] Searching for fishing spots...");
        foreach (var spot in Scene.GetAllComponents<FishingSpot>())
        {
            var dist = Vector3.DistanceBetween(playerPosition, spot.Transform.Position);
            Log.Info($"[DEBUG] Spot at {spot.Transform.Position}, dist: {dist}, radius: {spot.CastRadius}");

            if (dist <= spot.CastRadius)
            {
                Log.Info("[DEBUG] Found valid fishing spot!");
                return spot;
            }
        }
        Log.Warning("[HOST] No fishing spots found in range.");
        return null;
    }
}
