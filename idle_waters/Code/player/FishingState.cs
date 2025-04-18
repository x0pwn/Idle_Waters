using Sandbox;
using System.Threading.Tasks;

public sealed class FishingState : Component
{
    [Rpc.Host]
    public async void RequestCastRpc(Vector3 playerPosition)
    {
        var spot = FindNearbyFishingSpot(playerPosition);
        if (spot == null)
        {
            NotifyCatchRpc("Nothing", 0);
            return;
        }

        // Update the fishing line to point to the fishing spot
        var fishingLine = GameObject.Components.Get<FishingLine>();
        if (fishingLine != null)
        {
            fishingLine.StartCasting(spot.Transform.Position);
        }

        await Task.DelaySeconds(5f);

        var fish = FishLootTable.GenerateFish();

        var fishObject = new GameObject();
        fishObject.Name = $"Fish_{fish.Name}";
        var forward = GameObject.Transform.Rotation.Forward;
        fishObject.Transform.Position = playerPosition + forward * 50f + Vector3.Up * 10f;
        fishObject.NetworkMode = NetworkMode.Object;
        var fishEntity = fishObject.Components.Create<FishEntity>();
        fishEntity.FishName = fish.Name;
        fishEntity.FishValue = fish.Value;

        fishObject.Network.Spawn();

        var playerState = GameObject.Components.Get<PlayerState>();
        if (playerState != null)
        {
            playerState.AddMoney(fish.Value);
        }

        NotifyCatchRpc(fish.Name, fish.Value);
    }

    [Rpc.Owner]
    public void NotifyCatchRpc(string fishName, int value)
    {
        var hudObject = new GameObject();
        hudObject.Name = "CatchNotification";
        hudObject.NetworkMode = NetworkMode.Never;
        var notification = hudObject.Components.Create<CatchNotification>();
        notification.FishName = fishName;
        notification.FishValue = value;

        var rod = GameObject.Components.Get<FishingRod>();
        if (rod != null)
        {
            rod.SetCastingComplete();
        }
    }

    private FishingSpot FindNearbyFishingSpot(Vector3 playerPosition)
    {
        foreach (var spot in Scene.GetAllComponents<FishingSpot>())
        {
            var dist = Vector3.DistanceBetween(playerPosition, spot.Transform.Position);
            if (dist <= spot.CastRadius)
            {
                return spot;
            }
        }
        return null;
    }
}
