using Sandbox;
using Sandbox.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

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
    [Property] public float RodBendAmount { get; set; } = 20.0f;
    [Property] public float RodBendSpeed { get; set; } = 10.0f;
    [Property] public float RodReturnSpeed { get; set; } = 5.0f;
    [Property] public float RodFlexibility { get; set; } = 5.0f;

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
    private Angles originalRodRotation;
    private Angles currentRodRotation;
    private float rodBendAngle = 0f;

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
        
        forward = forward.Normal;
        
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
        var horizontalDirection = direction.WithY(0).Normal;
        var horizontalDistance = (target - castStartPos).WithY(0).Length * CastDistance;
        float verticalFactor = MathF.Max(0.2f, 1.0f + direction.y);
        
        castEndPos = castStartPos + horizontalDirection * horizontalDistance;
        
        float yTarget = target.y;
        if (direction.y < 0)
        {
            castEndPos.y = MathF.Min(yTarget, 1.0f);
        }
        else
        {
            float heightOffset = direction.y * horizontalDistance * 0.3f;
            castEndPos.y = MathF.Max(yTarget, castStartPos.y + heightOffset);
        }
        
        float peakOffset = direction.y > 0 ? 0.6f : 0.4f;
        castPeakPos = Vector3.Lerp(castStartPos, castEndPos, peakOffset);
        castPeakPos.y += CastArcHeight * verticalFactor;
        
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
        
        if (progress < 0.2f) 
        {
            float backswingProgress = progress / 0.2f;
            
            Vector3 castDir = (castEndPos - castStartPos).Normal;
            Vector3 backDirection = -castDir;
            
            Vector3 rightVec = Vector3.Cross(Vector3.Up, castDir).Normal;
            
            Vector3 backOffset = backDirection * 10f;
            backOffset.y += InitialBackSwingHeight * backswingProgress;
            
            backOffset += rightVec * backswingProgress * 5f;
            
            currentPos = castStartPos + backOffset * MathF.Sin(backswingProgress * MathF.PI);
        }
        else if (progress < 0.6f)
        {
            float forwardProgress = (progress - 0.2f) / 0.4f;
            
            Vector3 start = castStartPos;
            Vector3 end = castEndPos;
            
            Vector3 castDir2 = (castEndPos - castStartPos).Normal;
            float heightFactor = 1.0f + MathF.Max(0, castDir2.y);
            
            Vector3 control1 = Vector3.Lerp(start, castPeakPos, 0.7f);
            Vector3 control2 = Vector3.Lerp(castPeakPos, end, 0.3f);
            
            currentPos = BezierPoint(start, control1, control2, end, forwardProgress);
        }
        else
        {
            float flyProgress = (progress - 0.6f) / 0.4f;
            
            Vector3 castDir3 = (castEndPos - castStartPos).Normal;
            
            float verticalArc;
            
            if (castDir3.y >= 0)
            {
                verticalArc = -MathF.Sin(flyProgress * MathF.PI) * 15f * flyProgress * flyProgress;
            }
            else
            {
                verticalArc = -MathF.Sin(flyProgress * MathF.PI) * 5f * flyProgress;
            }
            
            currentPos = Vector3.Lerp(castPeakPos, castEndPos, flyProgress);
            currentPos.y += verticalArc;
            
            float horizontalDamping = 1.0f - (flyProgress * flyProgress * 0.3f);
            currentPos = Vector3.Lerp(currentPos, castEndPos, 1.0f - horizontalDamping);
        }
        
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
        
        if (RodTip != null)
            originalRodRotation = RodTip.Transform.Rotation.Angles();
        
        currentRodRotation = originalRodRotation;
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
        UpdateRodBending();
    }

    private void UpdateRodBending()
    {
        if (RodTip == null) return;
        
        if (isAnimatingCast)
        {
            float progress = (Time.Now - castStartTime) / CastDuration;
            
            if (progress < 0.2f)
            {
                float bendFactor = progress / 0.2f;
                rodBendAngle = -RodBendAmount * MathF.Sin(bendFactor * MathF.PI);
            }
            else if (progress < 0.4f)
            {
                float bendFactor = (progress - 0.2f) / 0.2f;
                rodBendAngle = RodBendAmount * MathF.Sin(bendFactor * MathF.PI);
            }
            else
            {
                float bendFactor = (progress - 0.4f) / 0.6f;
                rodBendAngle = RodBendAmount * (1 - bendFactor) * 0.5f * MathF.Cos(bendFactor * MathF.PI * 4);
            }
        }
        else
        {
            rodBendAngle = rodBendAngle * (1.0f - Time.Delta * RodReturnSpeed);
        }
        
        Angles targetAngles = originalRodRotation;
        targetAngles.pitch += rodBendAngle;
        
        currentRodRotation = Angles.Lerp(currentRodRotation, targetAngles, Time.Delta * RodBendSpeed);
        RodTip.Transform.Rotation = Rotation.From(currentRodRotation);
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
            
            Vector3 lineDirection = (end - start).Normal;
            Vector3 gravity = Vector3.Down;
            
            for (int i = 0; i < linePoints.Count; i++)
            {
                float t = (i + 1) / (float)LineSegments;
                
                Vector3 point = Vector3.Lerp(start, end, t);
                
                float sagFactor;
                
                if (lineInWater && !isAnimatingCast)
                {
                    sagFactor = LineSag * (1.0f + MathF.Sin(t * MathF.PI * 2 + bobTimer) * 0.2f);
                }
                else if (isAnimatingCast)
                {
                    sagFactor = 0.1f + 0.1f * t;
                }
                else
                {
                    sagFactor = LineSag * 0.7f;
                }
                
                float curvature = MathF.Sin(t * MathF.PI) * sagFactor * distance * 0.1f;
                
                float gravityEffect = Vector3.Dot(-lineDirection, gravity) * 0.5f + 0.5f;
                Vector3 sagDirection = Vector3.Lerp(gravity, -lineDirection, 0.3f).Normal;
                
                point += sagDirection * curvature * gravityEffect;
                
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
