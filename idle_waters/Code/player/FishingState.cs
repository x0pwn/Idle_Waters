// File: Code/Player/FishingState.cs
using Sandbox;
using System.Threading.Tasks;

namespace IdleWaters
{
    public sealed class FishingState : Component
    {
        [Rpc.Host]
        public async void RequestCastRpc(Vector3 playerPosition)
        {
            // 1) find the spot
            var spot = FindNearbyFishingSpot(playerPosition);
            if (spot == null)
            {
                NotifyCatchRpc("Nothing", 0);
                return;
            }

            // 2) play casting effect
            GameObject.Components.Get<FishingLine>()?
                      .StartCasting(spot.Transform.Position);

            await Task.DelaySeconds(5f);

            // 3) create the fish object
            var info = FishLootTable.GenerateFish();
            var fishObject = new GameObject
            {
                Name        = $"Fish_{info.Name}",
                NetworkMode = NetworkMode.Object
            };
            fishObject.Transform.Position = playerPosition
                                          + GameObject.Transform.Rotation.Forward * 50f
                                          + Vector3.Up * 10f;

            // 4) add data + visuals
            var data = fishObject.Components.Create<FishData>();
            data.FishName  = info.Name;
            data.FishValue = info.Value;

            var ent = fishObject.Components.Create<FishEntity>();
            ent.FishName  = info.Name;
            ent.FishValue = info.Value;
            ent.FishIcon  = data.FishIcon;

            // 5) spawn it *as* the caller, giving them ownership
            fishObject.Network.SetOwnerTransfer(OwnerTransfer.Takeover);
            bool success = fishObject.NetworkSpawn(Rpc.Caller);
            if (!success)
            {
                Log.Warning("Failed to spawn fish for client");
                return;
            }

            // small delay so the network fully syncs before we operate on it
            await Task.DelaySeconds(0.5f);

            // 6) now that this component lives on the pawn,
            //    GameObject.Components.Get<InventoryComponent>() finds the right inventory
            var inv = GameObject.Components.Get<InventoryComponent>();
            if (inv == null || !inv.AddItem(fishObject))
            {
                Log.Warning("AddItem failed");
            }

            // 7) notify the client’s HUD that they caught something
            NotifyCatchRpc(info.Name, info.Value);
        }

        [Rpc.Owner]
        public void NotifyCatchRpc(string fishName, int value)
        {
            var hudObject = new GameObject { NetworkMode = NetworkMode.Never };
            var notification = hudObject.Components.Create<CatchNotification>();
            notification.FishName  = fishName;
            notification.FishValue = value;

            GameObject.Components.Get<FishingRod>()?
                      .SetCastingComplete();
        }

        private FishingSpot FindNearbyFishingSpot(Vector3 pos)
        {
            foreach (var s in Scene.GetAllComponents<FishingSpot>())
            {
                if (Vector3.DistanceBetween(pos, s.Transform.Position) <= s.CastRadius)
                    return s;
            }
            return null;
        }
    }
}
