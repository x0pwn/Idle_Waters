using Sandbox;

namespace IdleWaters;

[Title("Fishing Line")]
[Category("Idle Waters")]
[Icon("show_chart")]
public sealed class FishingLine : Component
{
    [Property] public GameObject RodTip { get; set; } // The rod's tip where the line starts
    [Property] public float LineWidth { get; set; } = 0.1f; // Width of the line
    [Property] public Color LineColor { get; set; } = Color.White; // Color of the line

    private LineRenderer lineRenderer;
    private GameObject targetPoint; // GameObject for the target position
    private bool isCasting;

    protected override void OnStart()
    {
        // Initialize the LineRenderer
        lineRenderer = Components.Create<LineRenderer>();
        lineRenderer.Width = LineWidth;
        lineRenderer.Color = LineColor;
        lineRenderer.Enabled = false; // Hidden by default

        // Create a GameObject for the target point
        targetPoint = new GameObject();
        targetPoint.Name = "FishingLineTargetPoint";
        targetPoint.NetworkMode = NetworkMode.Object; // Ensure networked for multiplayer
        targetPoint.Network.Spawn();

		
    }

	public void StartCasting(Vector3 target)
	{
		Log.Info("StartCasting called with target: " + target);
		targetPoint.Transform.Position = target;
		isCasting = true;
		lineRenderer.Enabled = true;
		UpdateLine();
	}

	public void StopCasting()
	{
		Log.Info("StopCasting called");
		isCasting = false;
		lineRenderer.Enabled = false;
	}

    protected override void OnUpdate()
    {
        if (isCasting)
        {
            UpdateLine();
        }
    }

    private void UpdateLine()
    {
        if (RodTip == null || targetPoint == null) return;

        // Set the line points using GameObjects (RodTip and targetPoint)
        lineRenderer.Points = new List<GameObject> { RodTip, targetPoint };
    }

    protected override void OnDestroy()
    {
        // Clean up the target point GameObject
        if (targetPoint != null)
        {
            targetPoint.Destroy();
        }
    }
}
