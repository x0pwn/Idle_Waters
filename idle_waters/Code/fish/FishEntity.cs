using Sandbox;
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

        // Clone the prefab
        var prefab = ResourceLibrary.Get<PrefabFile>("prefabs/fish.prefab");
        if (prefab == null)
        {
            GameObject.Destroy();
            return;
        }

        FishPrefabInstance = GameObject.Clone(prefab);
        if (FishPrefabInstance == null)
        {
            GameObject.Destroy();
            return;
        }

        // Parent the instance to this GameObject and network it
        FishPrefabInstance.Parent = GameObject;
        FishPrefabInstance.LocalPosition = Vector3.Zero;
        FishPrefabInstance.NetworkMode = NetworkMode.Object;
        FishPrefabInstance.Enabled = true;

        // Explicitly spawn the prefab instance to ensure networking
        FishPrefabInstance.Network.Spawn();

        // Ensure ModelRenderer is visible
        var renderer = FishPrefabInstance.Components.Get<ModelRenderer>();
        if (renderer != null)
        {
            renderer.Enabled = true;
        }

        // Set scale based on fish name
        float scale = FishName switch
        {
            "Smol Carp" => 0.5f,
            "Boot" => 0.5f,
            "Medium Carp" => 1.0f,
            "Large Carp" => 2.0f,
            _ => 1.0f
        };
        FishPrefabInstance.Transform.Scale = new Vector3(scale, scale, scale);

        // Start the catch animation
        _ = OnCaught();
    }

    private async Task OnCaught()
    {
        if (FishPrefabInstance == null || !FishPrefabInstance.IsValid())
        {
            GameObject.Destroy();
            return;
        }

        // Animate the fish (wiggle)
        for (int i = 0; i < 3; i++)
        {
            FishPrefabInstance.LocalRotation = Rotation.FromAxis(Vector3.Up, 30f);
            await Task.Delay(100);
            FishPrefabInstance.LocalRotation = Rotation.FromAxis(Vector3.Up, -30f);
            await Task.Delay(100);
        }

        // Move upward
        FishPrefabInstance.LocalPosition += Vector3.Up * 50f;

        // Destroy after animation
        await Task.DelaySeconds(2f);
        GameObject.Destroy();
    }

    protected override void OnDestroy()
    {
        FishPrefabInstance?.Destroy();
        FishPrefabInstance = null;
    }
}
