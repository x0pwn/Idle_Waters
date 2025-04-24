// File: Code/Player/FishingState.cs
using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdleWaters
{
    public sealed class FishingState : Component
    {
        [Rpc.Host]
        public async void RequestCastRpc(Vector3 playerPosition, Guid clientId)
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

             // 5) spawn it with the client who caught it as the owner
            fishObject.Network.SetOwnerTransfer(OwnerTransfer.Takeover);
            
            // Find the client connection by ID instead of using Rpc.Caller
            var clientConnection = Connection.All.FirstOrDefault(c => c.Id == clientId);
            
            // Log for debugging
            if (clientConnection != null)
            {
                Log.Info($"Found connection for client: {clientConnection.DisplayName} (ID: {clientConnection.Id})");
            }
            else
            {
                Log.Warning($"Could not find connection for client ID: {clientId}");
            }
            
            // Use the client connection instead of Rpc.Caller
            bool success = fishObject.NetworkSpawn(clientConnection ?? Rpc.Caller);
            
            if (!success)
            {
                Log.Warning("Failed to spawn fish for client");
                return;
            }

            // small delay so the network fully syncs before we operate on it
            await Task.DelaySeconds(0.5f);

            // 6) Find the specific player's inventory who caught the fish, using the caller's player object
            Log.Info($"Finding inventory for client ID: {clientId}");
            
            var playerObjects = Scene.GetAllObjects(true)
                .Where(go => go.Components.Get<InventoryComponent>() != null)
                .ToList();
            
            // Additional logging to diagnose issues
            Log.Info($"Found {playerObjects.Count} objects with inventory components");
            foreach (var obj in playerObjects)
            {
                Log.Info($"Object: {obj.Name}, Owner ID: {obj.Network.OwnerId}, Owner Name: {obj.Network.OwnerConnection?.DisplayName ?? "None"}");
            }
            
            // Use the connection's ID for precise matching instead of display name
            var playerObject = playerObjects.FirstOrDefault(go => 
                go.Network.OwnerId == clientId);
            
            if (playerObject != null)
            {
                Log.Info($"FOUND correct inventory for player: {clientId}");
                Log.Info($"Selected Player Object: {playerObject.Name}, Owner ID: {playerObject.Network.OwnerId}");
                
                var inventory = playerObject.Components.Get<InventoryComponent>();
                if (inventory != null)
                {
                    if (!inventory.AddItem(fishObject))
                    {
                        Log.Warning($"AddItem failed for player {Rpc.Caller.DisplayName}");
                    }
                    else
                    {
                        Log.Info($"Successfully added fish to {Rpc.Caller.DisplayName}'s inventory");
                    }
                }
                else
                {
                    Log.Warning($"No inventory component found on player object for {Rpc.Caller.DisplayName}");
                }
            }
            else
            {
                Log.Warning($"Could not find player object for {Rpc.Caller.DisplayName}");
            }

            // 7) notify the client's HUD that they caught something
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
