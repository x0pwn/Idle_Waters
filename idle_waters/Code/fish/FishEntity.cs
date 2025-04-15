using Sandbox;
using System;
using System.Threading.Tasks;

[Title("Fish Entity")]
[Category("Idle Waters")]
[Icon("fish")]
public sealed class FishEntity : Component
{
    [Property] public string FishName { get; set; }
    [Property] public int FishValue { get; set; }
    private GameObject FishPrefabInstance { get; set; }

    protected override void OnStart()
    {
        base.OnStart();
        Log.Info($"[FishEntity] OnStart for {FishName} (Value: ${FishValue})");

        // Clone the prefab
        var prefab = ResourceLibrary.Get<PrefabFile>("prefabs/fish.prefab");
        if (prefab == null)
        {
            Log.Error("[FishEntity] Failed to load prefab: prefabs/fish.prefab");
            GameObject.Destroy();
            return;
        }
        Log.Info("[FishEntity] Prefab loaded successfully");

        FishPrefabInstance = GameObject.Clone(prefab);
        if (FishPrefabInstance == null)
        {
            Log.Error("[FishEntity] Failed to spawn prefab instance");
            GameObject.Destroy();
            return;
        }
        Log.Info("[FishEntity] Prefab instance spawned");

        // Parent the instance to this GameObject and network it
        FishPrefabInstance.Parent = GameObject;
        FishPrefabInstance.LocalPosition = Vector3.Zero;
        FishPrefabInstance.NetworkMode = NetworkMode.Object;

        // Ensure visibility
        FishPrefabInstance.Enabled = true;
        var renderer = FishPrefabInstance.Components.Get<ModelRenderer>();
        if (renderer != null)
        {
            renderer.Enabled = true;
            Log.Info("[FishEntity] ModelRenderer found and enabled");
        }
        else
        {
            Log.Warning("[FishEntity] No ModelRenderer found on prefab");
        }

        Log.Info($"[FishEntity] Spawned for {FishName} at {GameObject.Transform.Position}");

        // Start the catch animation
        _ = OnCaught();
    }

    private async Task OnCaught()
    {
        Log.Info("[FishEntity] Starting OnCaught animation");

        if (FishPrefabInstance == null || !FishPrefabInstance.IsValid())
        {
            Log.Warning("[FishEntity] FishPrefabInstance is null or invalid in OnCaught");
            GameObject.Destroy();
            return;
        }

        // Animate the fish (wiggle)
        for (int i = 0; i < 3; i++)
        {
            Log.Info("[FishEntity] Wiggle iteration " + i);
            FishPrefabInstance.LocalRotation = Rotation.FromAxis(Vector3.Up, 30f);
            await Task.Delay(100);
            FishPrefabInstance.LocalRotation = Rotation.FromAxis(Vector3.Up, -30f);
            await Task.Delay(100);
        }

        // Move upward
        Log.Info("[FishEntity] Moving upward");
        FishPrefabInstance.LocalPosition += Vector3.Up * 50f;

        // Destroy after animation
        Log.Info("[FishEntity] Waiting to destroy");
        await Task.DelaySeconds(2f);
        Log.Info("[FishEntity] Destroying");
        GameObject.Destroy();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Log.Info("[FishEntity] OnDestroy called");
        FishPrefabInstance?.Destroy();
        FishPrefabInstance = null;
    }
}
