using Sandbox;
using System;

public sealed class KillBox : Component, Component.ITriggerListener
{
    [Property] public bool ResetPosition { get; set; } = true;
    [Property] public bool DestroyObjects { get; set; } = false;
    [Property] public string[] TargetTags { get; set; } = new[] { "player" };
    
    // Called when a collider enters our trigger
    public void OnTriggerEnter(Collider other)
    {
        Log.Info($"Object entered kill box: {other.GameObject.Name}");
        
        // Check if this object has any of our target tags
        bool shouldAffect = false;  // Initialize to false
        foreach (var tag in TargetTags)
        {
            if (other.GameObject.Tags.Has(tag))
            {
                shouldAffect = true;
                break;
            }
        }
        
        // Skip if shouldn't affect this object
        if (!shouldAffect)
        {
            Log.Info($"KillBox ignoring {other.GameObject.Name} - no matching tags");
            return;
        }
            
        // Look for player in the entire hierarchy (this object or any parent)
        var player = FindPlayerInHierarchy(other.GameObject);
        
        if (ResetPosition && player != null)
        {
            Log.Info($"Resetting player position: {player.GameObject.Name}");
            // Reset to a spawn point
            var spawnPoint = Scene.GetAllComponents<SpawnPoint>().FirstOrDefault();
            if (spawnPoint != null)
            {
                player.GameObject.Transform.Position = spawnPoint.Transform.Position;
                player.GameObject.Transform.Rotation = spawnPoint.Transform.Rotation;
            }
            else
            {
                // Default reset if no spawn point found
                player.GameObject.Transform.Position = new Vector3(0, 0, 100);
            }
        }
        
        if (DestroyObjects && player == null)
        {
            Log.Info($"Destroying object: {other.GameObject.Name}");
            other.GameObject.Destroy();
        }
    }
    
    // Optionally implement other trigger methods if needed
    public void OnTriggerExit(Collider other)
    {
        Log.Info($"Exited kill box: {other.GameObject.Name}");
    }
    
    // Helper method to find PlayerController in parent hierarchy
    private PlayerController FindPlayerInHierarchy(GameObject obj)
    {
        // Check this object
        var player = obj.Components.Get<PlayerController>();
        if (player != null)
            return player;
        
        // Check parent if available
        if (obj.Parent != null)
            return FindPlayerInHierarchy(obj.Parent);
        
        // Not found
        return null;
    }
}
