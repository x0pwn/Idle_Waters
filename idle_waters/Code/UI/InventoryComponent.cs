// File: Code/Player/InventoryComponent.cs
using Sandbox;
using System.Collections.Generic;

namespace IdleWaters
{
    public class InventoryComponent : Component
    {
        [Sync]
        private NetList<GameObject> _items { get; set; } = new();

        /// <summary>Only the host (in single‑player) or the owner sees their fish list.</summary>
        public IReadOnlyList<GameObject> Items
            => Networking.IsHost || !GameObject.IsProxy
               ? _items
               : new List<GameObject>();

        /// <summary>
        /// Host‑only: hide the fish’s visuals and move it into our synced list.
        /// Fails if it’s already there, or if the fish isn’t owned by this pawn.
        /// </summary>
        public bool AddItem(GameObject fish)
        {
            if (!Networking.IsHost || _items.Contains(fish))
                return false;

            // owner check: only add if this pawn’s owner matches the fish’s owner
            if (GameObject.Network.OwnerId != fish.Network.OwnerId)
            {
                Log.Warning("Tried to add fish to another player's inventory!");
                return false;
            }

            // hide its model and detach
            foreach (var r in fish.Components.GetAll<ModelRenderer>())
                r.Enabled = false;

            fish.SetParent(null);

            // add into the list (syncs to the client)
            _items.Add(fish);

            // tell the owner we’ve updated their list
            NotifyInventoryChanged();
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

        /// <summary>
        /// Host→Owner RPC: runs on the owning client so they can refresh their HUD.
        /// </summary>
        [Rpc.Owner]
        public void NotifyInventoryChanged()
        {
            GameObject.Components.Get<InventoryHud>()?.ForceRebuild();
        }
    }
}
