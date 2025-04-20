using Sandbox;

[Title("Equippable Item")]
[Category("Idle Waters")]
[Icon("sports_martial_arts")]
public sealed class EquippableItem : Component
{
    [Property] public string SlotName { get; set; }
    [Property] public GameObject Owner { get; set; }
    
    // Position and rotation offsets for fine-tuning
    [Property] public Vector3 PositionOffset { get; set; } = Vector3.Zero;
    [Property] public Angles RotationOffset { get; set; } = new Angles(0, 0, 0);
    
    protected override void OnStart()
    {
        // Apply position/rotation offsets for fine-tuning
        GameObject.Transform.LocalPosition = PositionOffset;
        GameObject.Transform.LocalRotation = Rotation.From(RotationOffset);
    }
    
    /// <summary>
    /// Called when the item is equipped
    /// </summary>
    public void OnEquipped(GameObject owner)
    {
        Owner = owner;
        // You can add any functionality here that should happen when equipped
    }
    
    /// <summary>
    /// Called when the item is unequipped
    /// </summary>
    public void OnUnequipped()
    {
        Owner = null;
        // You can add any functionality here that should happen when unequipped
    }
}
