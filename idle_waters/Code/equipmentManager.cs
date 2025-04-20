using Sandbox;
using System.Collections.Generic;

[Title("Equipment Manager")]
[Category("Idle Waters")]
[Icon("inventory")]
public sealed class EquipmentManager : Component
{
    // Dictionary to track equipped items by slot
    private Dictionary<string, GameObject> equippedItems = new Dictionary<string, GameObject>();
    
    // References for attaching equipment
    [Property] public GameObject RightHandAttachment { get; set; }
    [Property] public GameObject LeftHandAttachment { get; set; }
    
    /// <summary>
    /// Equips an item to the specified slot
    /// </summary>
    public GameObject EquipItem(string slotName, string prefabPath)
    {
        // Unequip any existing item in this slot
        UnequipItem(slotName);
        
        // Get the appropriate attachment point
        GameObject attachPoint = GetAttachmentPoint(slotName);
        if (attachPoint == null)
        {
            Log.Warning($"No attachment point found for slot: {slotName}");
            return null;
        }
        
        // Load and create the item
        var prefab = ResourceLibrary.Get<PrefabFile>(prefabPath);
        if (prefab == null)
        {
            Log.Warning($"Could not find prefab: {prefabPath}");
            return null;
        }
        
        // Create the item and parent it
        var item = GameObject.Clone(prefab); // Changed from prefab.InstantiateNow()
        item.SetParent(attachPoint);
        item.LocalPosition = Vector3.Zero;
        item.LocalRotation = Rotation.Identity;
        
        // Create equipment component to handle the item
        var equippable = item.Components.GetOrCreate<EquippableItem>();
        equippable.SlotName = slotName;
        equippable.Owner = GameObject;
        
        // Store reference
        equippedItems[slotName] = item;
        
        Log.Info($"Equipped {prefabPath} to {slotName}");
        return item;
    }
    
    /// <summary>
    /// Unequips an item from the specified slot
    /// </summary>
    public void UnequipItem(string slotName)
    {
        if (equippedItems.TryGetValue(slotName, out var item) && item != null)
        {
            item.Destroy();
            equippedItems.Remove(slotName);
            Log.Info($"Unequipped item from {slotName}");
        }
    }
    
    /// <summary>
    /// Gets the currently equipped item in a slot
    /// </summary>
    public GameObject GetEquippedItem(string slotName)
    {
        if (equippedItems.TryGetValue(slotName, out var item))
            return item;
            
        return null;
    }
    
    /// <summary>
    /// Returns the appropriate attachment point for a given slot
    /// </summary>
    private GameObject GetAttachmentPoint(string slotName)
    {
        return slotName switch
        {
            "RightHand" => RightHandAttachment,
            "LeftHand" => LeftHandAttachment,
            _ => null
        };
    }
}
