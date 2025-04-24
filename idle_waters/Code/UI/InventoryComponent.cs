using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleWaters
{
    public class InventoryComponent : Component
    {
        [Sync(SyncFlags.FromHost)]
        private NetList<GameObject> _items { get; set; } = new();

        /// <summary>All connected clients should see the items in their inventory.</summary>
        public IReadOnlyList<GameObject> Items => _items;

        /// <summary>
        /// Host‑only: hide the fish’s visuals and move it into our synced list.
        /// Fails if it’s already there, or if the fish isn’t owned by this pawn.
        /// </summary>
        public bool AddItem(GameObject fish)
        {
            if (!Networking.IsHost || _items.Contains(fish))
            {
                Log.Warning($"Fish already in inventory: {fish.Name}");
                return false;
            }

            Log.Info($"Fish owner ID: {fish.Network.OwnerId}, Inventory owner ID: {GameObject.Network.OwnerId}, Caller ID: {Rpc.Caller?.Id}");
            
            foreach (var r in fish.Components.GetAll<ModelRenderer>())
                r.Enabled = false;

            fish.SetParent(null);

            int beforeCount = _items.Count;
            _items.Add(fish);
            int afterCount = _items.Count;
            
            Log.Info($"NetList before add: {beforeCount}, after add: {afterCount}");
            
            if (afterCount > beforeCount)
            {
                Log.Info($"Item successfully added to NetList");
            }
            else
            {
                Log.Warning($"Failed to add item to NetList - item may have been rejected");
                
                var tempList = new NetList<GameObject>();
                foreach (var item in _items)
                    tempList.Add(item);
                tempList.Add(fish);
                _items = tempList;
                
                Log.Info($"Created new NetList with {_items.Count} items");
            }
            
            LogInventoryContents("After AddItem");

            NotifyInventoryChangedBroadcast(GameObject.Network.OwnerId);

            var ownerName = GameObject.Network.OwnerConnection?.DisplayName ?? "Unknown";
            Log.Info($"Successfully added fish to {ownerName}'s inventory");

            return true;
        }

        /// <summary>
        /// Client→Host RPC: clients call this to ask the host to run AddItem(...)
        /// </summary>
        [Rpc.Host]
        public void RpcRequestAddItem(GameObject fish)
        {
            AddItem(fish);
        }

        [Rpc.Broadcast]
        public void NotifyInventoryChangedBroadcast(Guid targetClientId)
        {
            if (Connection.Local.Id != targetClientId)
                return;

            Log.Info($"Updating inventory for client: {Connection.Local.DisplayName} ({targetClientId})");

            var hud = Scene.GetAllComponents<InventoryHud>().FirstOrDefault();
            if (hud != null)
            {
                Log.Info($"Found HUD, forcing rebuild for {Connection.Local.DisplayName}");
                hud.ForceRebuild();
            }
            else
            {
                Log.Warning($"Could not find any InventoryHud component for {Connection.Local.DisplayName}!");
            }
        }

        public void LogInventoryContents(string context = "")
        {
            Log.Info($"[{context}] Inventory for {GameObject.Network.OwnerConnection?.DisplayName ?? "Unknown"} (Items: {_items.Count}):");
            foreach (var item in _items)
            {
                var fishData = item.Components.Get<FishData>();
                if (fishData != null)
                {
                    Log.Info($"  - {fishData.FishName} (${fishData.FishValue})");
                }
                else
                {
                    Log.Warning($"  - Item without FishData: {item.Name}");
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            if (!Networking.IsHost && Time.Now % 5 < 0.01f)
            {
                Log.Info($"CLIENT {Connection.Local.DisplayName} Inventory Items: {_items.Count}");
                if (_items.Count > 0)
                {
                    LogInventoryContents("Client periodic check");
                }
            }
        }
    }
}
