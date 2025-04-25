using Sandbox;
using Sandbox.Physics;
using System;
using System.Collections.Generic;

namespace IdleWaters;

[Title("Fishing Line")]
[Category("Idle Waters")]
[Icon("show_chart")]
public sealed class FishingLine : Component
{
    [Property] public GameObject RodTip { get; set; }
    [Property] public float LineWidth { get; set; } = 0.1f;
    [Property] public Color LineColor { get; set; } = Color.White;
    [Property] public float CastDuration { get; set; } = 0.6f;
    [Property] public float CastArcHeight { get; set; } = 40f;
    [Property] public int LineSegments { get; set; } = 10;
    [Property] public float LineSag { get; set; } = 0.5f;
    [Property] public float CastDistance { get; set; } = 5.0f;
    [Property] public float BobAmount { get; set; } = 0.3f;
    [Property] public float MaxCastDistance { get; set; } = 1000f;
    [Property] public float InitialBackSwingHeight { get; set; } = 30f;
    [Property] public GameObject PlayerEyes { get; set; }

    private LineRenderer lineRenderer;
    private GameObject targetPoint;
    private bool isCasting;
    private bool isAnimatingCast;
    private float castStartTime;
    private Vector3 castStartPos;
    private Vector3 castEndPos;
    private Vector3 castPeakPos;
    private float bobTimer = 0f;
    private bool lineInWater = false;
    private List<GameObject> linePoints = new();

    public void CastWherePlayerLooking()
    {
        Vector3 eyePos;
        Vector3 forward;
        
        if (PlayerEyes != null)
        {
            eyePos = PlayerEyes.Transform.Position;
            forward = PlayerEyes.Transform.Rotation.Forward;
            Log.Info("Using assigned PlayerEyes for view direction");
        }
        else
        {
            var player = Scene.GetAllComponents<PlayerController>()
                .FirstOrDefault(p => p.GameObject.Network.IsOwner);
                
            if (player == null)
            {
                Log.Warning("Cannot find local player for fishing cast");
                return;
            }
            
            eyePos = player.EyePosition;
            forward = player.GameObject.Transform.Rotation.Forward;
        }
        
        // Force Y component to ensure downward cast
        forward.y = Math.Min(forward.y, 0.2f);
        
        Vector3 castStart = RodTip.Transform.Position;
        
        Log.Info($"Casting from {eyePos} in direction {forward}");
        
        var tr = Scene.Trace.Ray(eyePos, eyePos + forward * MaxCastDistance)
            .UseHitboxes()
            .WithoutTags("player")
            .Run();
            
        Vector3 targetPos;
        
        if (tr.Hit)
        {
            targetPos = tr.HitPosition;
            
            Vector3 playerForward = PlayerEyes != null 
                ? PlayerEyes.Transform.Rotation.Forward 
                : Scene.GetAllComponents<PlayerController>().FirstOrDefault()?.GameObject.Transform.Rotation.Forward ?? Vector3.Forward;
                
            Vector3 toTarget = (targetPos - castStart).Normal;
            
            // Ensure target is in front of player
            if (Vector3.Dot(playerForward, toTarget) < 0.2f)
            {
                Log.Warning("Target was behind player, forcing to front");
                targetPos = castStart + playerForward * 200f;
            }
            
            lineInWater = targetPos.y < 1.0f;
        }
        else
        {
            targetPos = castStart + forward * 200f;
            lineInWater = targetPos.y < 1.0f;
        }
        
        StartCasting(targetPos);
    }

    public void StartCasting(Vector3 target)
    {
        Log.Info("StartCasting called with target: " + target);
        
        castStartPos = RodTip.Transform.Position;
        
        var direction = (target - castStartPos).Normal;
        var horizontalDistance = (target - castStartPos).WithY(0).Length * CastDistance;
        
        castEndPos = castStartPos + direction * horizontalDistance;
        castEndPos.y = Math.Min(target.y, 1.0f); // Keep at water level or lower
        
        castPeakPos = Vector3.Lerp(castStartPos, castEndPos, 0.4f);
        castPeakPos.y += CastArcHeight;
        
        targetPoint.Transform.Position = castStartPos;
        
        isAnimatingCast = true;
        castStartTime = Time.Now;
        isCasting = true;
        lineRenderer.Enabled = true;
    }

    public void StopCasting()
    {
        isCasting = false;
        isAnimatingCast = false;
        lineRenderer.Enabled = false;
    }

    private void AnimateCastingLine(float progress)
    {
        Vector3 currentPos;
        
        // 3-stage cast animation: backswing, forward swing, line flying out
        if (progress < 0.2f) 
        {
            // Backswing
            float backswingProgress = progress / 0.2f;
            
            Vector3 backOffset = (castStartPos - castEndPos).Normal * 10f;
            backOffset.y += InitialBackSwingHeight * backswingProgress;
            
            currentPos = castStartPos + backOffset * MathF.Sin(backswingProgress * MathF.PI);
        }
        else if (progress < 0.6f)
        {
            // Forward swing with high arc
            float forwardProgress = (progress - 0.2f) / 0.4f;
            
            Vector3 start = castStartPos; 
            Vector3 peak = castPeakPos;
            Vector3 end = castEndPos;
            
            currentPos = BezierPoint(start, peak, peak, end, forwardProgress);
        }
        else
        {
            // Line flying out and settling
            float flyProgress = (progress - 0.6f) / 0.4f;
            
            float downArc = MathF.Sin(flyProgress * MathF.PI) * 5f;
            currentPos = Vector3.Lerp(castPeakPos, castEndPos, flyProgress);
            currentPos.y -= downArc;
        }
        
        // Add slight horizontal variance for realism
        Vector3 castDirection = (castEndPos - castStartPos).Normal;
        Vector3 right = Vector3.Cross(Vector3.Up, castDirection).Normal;
        float horizontalVariance = MathF.Sin(progress * MathF.PI * 2.0f) * 1.0f * progress;
        
        currentPos += right * horizontalVariance;
        
        targetPoint.Transform.Position = currentPos;
    }

    private Vector3 BezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;
        
        return p;
    }

    protected override void OnDestroy()
    {
        if (targetPoint != null)
            targetPoint.Destroy();
            
        foreach (var point in linePoints)
        {
            if (point != null)
                point.Destroy();
        }
    }

    protected override void OnStart()
    {
        targetPoint = new GameObject();
        targetPoint.Name = "FishingLineTarget";
        targetPoint.NetworkMode = NetworkMode.Object;
        targetPoint.Network.Spawn();
        
        lineRenderer = Components.Create<LineRenderer>();
        lineRenderer.Width = LineWidth;
        lineRenderer.Color = LineColor;
        lineRenderer.Enabled = false;
        
        for (int i = 0; i < LineSegments; i++)
        {
            var point = new GameObject();
            point.Name = $"LinePoint_{i}";
            point.NetworkMode = NetworkMode.Object;
            point.Network.Spawn();
            linePoints.Add(point);
        }
    }

    protected override void OnUpdate()
    {
        if (isAnimatingCast)
        {
            float elapsedTime = Time.Now - castStartTime;
            float normalizedProgress = Math.Min(elapsedTime / CastDuration, 1.0f);
            
            AnimateCastingLine(normalizedProgress);
            
            if (normalizedProgress >= 1.0f)
            {
                isAnimatingCast = false;
                Log.Info("Cast animation complete, now fishing");
                
                if (targetPoint != null)
                {
                    Vector3 finalPos = targetPoint.Transform.Position;
                    Log.Info($"Freezing cast at final position: {finalPos}");
                }
            }
        }
        else if (isCasting)
        {
            if (lineInWater)
            {
                bobTimer += Time.Delta;
                float bobOffset = MathF.Sin(bobTimer * 2f) * BobAmount;
                
                Vector3 bobPos = targetPoint.Transform.Position;
                bobPos.y += bobOffset * Time.Delta;
                targetPoint.Transform.Position = bobPos;
            }
        }
        
        UpdateLine();
    }

    private void UpdateLine()
    {
        if (RodTip == null || targetPoint == null) return;
        
        if (isCasting)
        {
            Vector3 start = RodTip.Transform.Position;
            Vector3 end = targetPoint.Transform.Position;
            float distance = (end - start).Length;
            
            var lineGameObjects = new List<GameObject> { RodTip };
            
            for (int i = 0; i < linePoints.Count; i++)
            {
                float t = (i + 1) / (float)LineSegments;
                Vector3 point = Vector3.Lerp(start, end, t);
                
                float sagFactor = (lineInWater && !isAnimatingCast) ? LineSag : 0.2f;
                float curvature = MathF.Sin(t * MathF.PI) * sagFactor * distance * 0.1f;
                point.y -= curvature;
                
                linePoints[i].Transform.Position = point;
                lineGameObjects.Add(linePoints[i]);
            }
            
            lineGameObjects.Add(targetPoint);
            lineRenderer.Points = lineGameObjects;
        }
        else
        {
            lineRenderer.Points = new List<GameObject> { RodTip, targetPoint };
        }
    }
}
