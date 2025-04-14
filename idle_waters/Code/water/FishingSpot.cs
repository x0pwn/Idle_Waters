using Sandbox;
using System;

[Title("Fishing Spot")]
[Category("Idle Waters")]
[Icon("waves")]
public sealed class FishingSpot : Component
{
    [Property] public float CastRadius { get; set; } = 150f;

    /// <summary>
    /// Checks if the given world position is within casting range of this spot.
    /// </summary>
    public bool IsInRange(Vector3 position)
    {
        return (position - Transform.Position).Length <= CastRadius;
    }

#if TOOLS
protected override void DrawGizmos()
{
    Log.Info("DrawGizmos called");

    var pos = GameObject.Transform.Position;

    Gizmo.Draw.Color = Color.Red.WithAlpha(0.2f);
    Gizmo.Draw.SolidSphere(pos, CastRadius);
    Gizmo.Draw.Color = Color.Red;
    Gizmo.Draw.Sphere(pos, CastRadius);
}
#endif
}
