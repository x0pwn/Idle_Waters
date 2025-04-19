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
            "Smol Carp" => 0.05f,
            "Boot" => 0.05f,
            "Medium Carp" => .07f,
            "Large Carp" => .10f,
            _ => 0.20f
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

		// Make the fish face the player/camera
		if (Game.ActiveScene.Camera != null)
		{
			// Get direction from fish to camera
			Vector3 fishPosition = FishPrefabInstance.Transform.Position;
			Vector3 cameraPosition = Game.ActiveScene.Camera.Transform.Position;
			Vector3 directionToCamera = (cameraPosition - fishPosition).Normal;
			
			// Create rotation that looks in that direction (using Up as the up vector)
			Rotation lookRotation = Rotation.LookAt(directionToCamera, Vector3.Up);
			
			// Apply the rotation
			FishPrefabInstance.Transform.Rotation = lookRotation;
		}
		else
		{
			// Fallback: Rotate 180 degrees to face opposite direction
			FishPrefabInstance.Transform.LocalRotation = Rotation.FromYaw(180);
		}

		// Animate the fish (wiggle)
		Rotation baseRotation = FishPrefabInstance.Transform.LocalRotation;
		for (int i = 0; i < 3; i++)
		{
			FishPrefabInstance.Transform.LocalRotation = baseRotation * Rotation.FromAxis(Vector3.Up, 30f);
			await Task.Delay(100);
			FishPrefabInstance.Transform.LocalRotation = baseRotation * Rotation.FromAxis(Vector3.Up, -30f);
			await Task.Delay(100);
		}
		// Reset to base rotation
		FishPrefabInstance.Transform.LocalRotation = baseRotation;

		// Move upward
		FishPrefabInstance.Transform.LocalPosition += Vector3.Up * 50f;

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
