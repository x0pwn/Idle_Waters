using Sandbox;
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
    [Property] public float CastArcHeight { get; set; } = 20f;
    [Property] public int LineSegments { get; set; } = 10;
    [Property] public float LineSag { get; set; } = 0.5f;
    [Property] public float CastDistance { get; set; } = 1.5f;
    [Property] public float BobAmount { get; set; } = 0.3f;

    private LineRenderer lineRenderer;
    private GameObject targetPoint;
    private bool isCasting;
    private bool isAnimatingCast;
    private float castStartTime;
    private Vector3 castStartPos;
    private Vector3 castEndPos;
    private float bobTimer = 0f;
    private bool lineInWater = false;
    
    // Store line segment GameObjects in a list to avoid scene searches
    private List<GameObject> linePoints = new();

    protected override void OnStart()
    {
        // Initialize the LineRenderer
        lineRenderer = Components.Create<LineRenderer>();
        lineRenderer.Width = LineWidth;
        lineRenderer.Color = LineColor;
        lineRenderer.Enabled = false;

        // Create target point once
        targetPoint = new GameObject();
        targetPoint.Name = "FishingLineTargetPoint";
        targetPoint.NetworkMode = NetworkMode.Object;
        targetPoint.Network.Spawn();
        
        // Create line segment GameObjects once and reuse them
        CreateLineSegments();
    }
    
    private void CreateLineSegments()
    {
        // Create all line segment GameObjects up front
        for (int i = 1; i < LineSegments; i++)
        {
            var pointObj = new GameObject();
            pointObj.Name = $"LinePoint_{i}";
            pointObj.NetworkMode = NetworkMode.Object;
            pointObj.Network.Spawn();
            linePoints.Add(pointObj);
        }
    }

    public void StartCasting(Vector3 target)
    {
        Log.Info("StartCasting called with target: " + target);
        
        castStartPos = RodTip.Transform.Position;
        
        // Calculate cast end position
        var direction = (target - castStartPos).Normal;
        var distance = (target - castStartPos).Length * CastDistance;
        castEndPos = castStartPos + direction * distance;
        
        targetPoint.Transform.Position = castStartPos;
        
        isAnimatingCast = true;
        castStartTime = Time.Now;
        isCasting = true;
        lineRenderer.Enabled = true;
        
        lineInWater = target.y < 1.0f;
    }

    public void StopCasting()
    {
        isCasting = false;
        isAnimatingCast = false;
        lineRenderer.Enabled = false;
    }

    protected override void OnUpdate()
    {
        if (!isCasting) return;
        
        UpdateCastState();
        UpdateLine();
    }
    
    private void UpdateCastState()
    {
        if (isAnimatingCast)
        {
            float progress = Math.Min((Time.Now - castStartTime) / CastDuration, 1.0f);
            
            if (progress >= 1.0f)
            {
                isAnimatingCast = false;
                targetPoint.Transform.Position = castEndPos;
            }
            else
            {
                AnimateCastingLine(progress);
            }
        }
        else if (lineInWater)
        {
            UpdateWaterBobbing();
        }
    }
    
    private void UpdateWaterBobbing()
    {
        bobTimer += Time.Delta * 2.0f;
        float bobOffset = MathF.Sin(bobTimer) * BobAmount;
        float horizontalBob = MathF.Cos(bobTimer * 0.7f) * (BobAmount * 0.5f);
        
        Vector3 bobPos = castEndPos;
        bobPos.y += bobOffset;
        bobPos.x += horizontalBob;
        targetPoint.Transform.Position = bobPos;
    }

    private void AnimateCastingLine(float progress)
    {
        // Calculate current target point position based on animation progress
        Vector3 currentPos = Vector3.Lerp(castStartPos, castEndPos, progress);
        
        // Add arc and horizontal variance
        float arcProgress = MathF.Sin(progress * MathF.PI);
        float arcHeight = CastArcHeight * arcProgress * (1.0f - progress * 0.5f);
        float horizontalVariance = MathF.Sin(progress * MathF.PI * 2.0f) * 2.0f * progress;
        
        currentPos.y += arcHeight;
        currentPos.x += horizontalVariance;
        
        targetPoint.Transform.Position = currentPos;
    }

    private void UpdateLine()
    {
        if (RodTip == null || targetPoint == null) return;

        if (isAnimatingCast || lineInWater)
        {
            Vector3 start = RodTip.Transform.Position;
            Vector3 end = targetPoint.Transform.Position;
            float distance = (end - start).Length;
            
            // Build the list of line points
            var lineGameObjects = new List<GameObject> { RodTip };
            
            // Update positions of pre-created line segments
            for (int i = 0; i < linePoints.Count; i++)
            {
                float t = (i + 1) / (float)LineSegments;
                Vector3 point = Vector3.Lerp(start, end, t);
                
                // Calculate sag
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

    protected override void OnDestroy()
    {
        // Clean up GameObjects
        if (targetPoint != null)
            targetPoint.Destroy();
            
        // Clean up line segment GameObjects
        foreach (var point in linePoints)
        {
            if (point != null)
                point.Destroy();
        }
    }
}
