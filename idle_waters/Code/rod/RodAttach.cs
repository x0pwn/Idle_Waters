using Sandbox;
using System.Linq; 

[Title("Rod Controller")]
[Category("Idle Waters")]
[Icon("fishing_hook")]
public sealed class RodController : Component
{
    [Property] public GameObject PlayerCamera { get; set; }
    [Property] public GameObject RightHand { get; set; }
    
    [Property] public Vector3 PositionOffset { get; set; } = new Vector3(-9, -2, -39);
    [Property] public Angles RotationOffset { get; set; } = new Angles(0, 88, 34);
    
    [Property] public bool FollowCamera { get; set; } = true;

    [Property, Group("Smoothing")] 
    public float PositionSmoothSpeed { get; set; } = 10.0f;

    [Property, Group("Smoothing")] 
    public float RotationSmoothSpeed { get; set; } = 8.0f;

    [Property, Group("Smoothing")] 
    public float MovementTolerance { get; set; } = 0.5f;

    [Property, Group("Smoothing")]
    public float RotationTolerance { get; set; } = 1.0f;

    private Vector3 targetPosition;
    private Rotation targetRotation;
    
    private bool _rodEquipped = true;

    private Vector3 lastUsedHandPosition;
    private Rotation lastUsedCameraRotation;
    private bool hasInitializedPosition = false;
    
    protected override void OnStart()
    {
        if (PlayerCamera == null)
        {
            var cameras = Scene.GetAllObjects(true);
            foreach (var go in cameras)
            {
                if (go.Name.Contains("camera") || go.Components.Get<CameraComponent>() != null)
                {
                    PlayerCamera = go;
                    break;
                }
            }
                
            if (PlayerCamera == null)
                Log.Warning("Rod Controller: Camera not found!");
        }
        
        if (RightHand == null)
        {
            var players = Scene.GetAllObjects(true);
            GameObject player = null;
            
            foreach (var go in players)
            {
                if (go.Components.Get<PlayerController>() != null)
                {
                    player = go;
                    break;
                }
            }
            
            if (player != null)
            {
                SkinnedModelRenderer playerModel = null;
                var models = player.Components.GetAll<SkinnedModelRenderer>();
                
                foreach (var model in models)
                {
                    if (model.Model != null && model.Model.ResourcePath.Contains("citizen"))
                    {
                        playerModel = model;
                        break;
                    }
                }
                
                if (playerModel != null)
                {
                    RightHand.SetParent(playerModel.GameObject);
                    
                    RightHand.Transform.LocalPosition = new Vector3(8, 0, 0);
                    Log.Info("Created hand attachment point");
                }
            }
        }
    }
    
    protected override void OnUpdate()
    {
        if (!_rodEquipped) return;
        
        if (RightHand != null && PlayerCamera != null)
        {
            var handPosition = RightHand.Transform.Position;
            var cameraRotation = PlayerCamera.Transform.Rotation;
            
            // On first update, initialize positions
            if (!hasInitializedPosition)
            {
                lastUsedHandPosition = handPosition;
                lastUsedCameraRotation = cameraRotation;
                hasInitializedPosition = true;
            }
            
            // Check if movement exceeds tolerance
            bool updatePosition = (handPosition - lastUsedHandPosition).Length > MovementTolerance;
            
            // Check if rotation exceeds tolerance (angle difference)
            Rotation diff = cameraRotation.Inverse * lastUsedCameraRotation;
            Angles eulerAngles = diff.Angles();
            // Fix: Create a Vector3 from the individual Angles components
            Vector3 angles = new Vector3(eulerAngles.pitch, eulerAngles.yaw, eulerAngles.roll);
            float rotDiff = angles.Length; // Magnitude of angular difference

            bool updateRotation = rotDiff > RotationTolerance;
            
            // Only update if movement or rotation exceeds tolerance
            if (updatePosition || updateRotation)
            {
                // Update the values we use for calculating
                lastUsedHandPosition = handPosition;
                lastUsedCameraRotation = cameraRotation;
                
                // Proceed with normal calculation using these values
                var cameraForward = lastUsedCameraRotation.Forward;
                
                var horizontalForward = cameraForward;
                horizontalForward.z *= 0.3f; 
                horizontalForward = horizontalForward.Normal;
                
                var targetRot = Rotation.LookAt(horizontalForward, Vector3.Up);
                // Fix: Pass the RotationOffset directly instead of creating a Vector3
                targetRot *= Rotation.From(RotationOffset);
                
                var handleOffset = new Vector3(-5, 0, 0);
                var rotatedHandleOffset = targetRot * handleOffset;
                
                // Calculate the target position
                targetPosition = lastUsedHandPosition - rotatedHandleOffset + targetRot * PositionOffset;
                targetRotation = targetRot;
            }
            
            // Rest of smoothing code stays the same
            float deltaTime = Time.Delta;
            
            // Smoothly move toward target position
            Vector3 smoothedPosition = Vector3.Lerp(
                GameObject.Transform.Position, 
                targetPosition, 
                deltaTime * PositionSmoothSpeed
            );
            
            // Smoothly rotate toward target rotation
            Rotation smoothedRotation = Rotation.Slerp(
                GameObject.Transform.Rotation,
                targetRotation,
                deltaTime * RotationSmoothSpeed
            );
            
            // Apply the smoothed values
            GameObject.Transform.Position = smoothedPosition;
            GameObject.Transform.Rotation = smoothedRotation;
        }
    }
    
    public void EquipRod()
    {
        GameObject.Enabled = true;
        _rodEquipped = true;
    }
    
    public void UnequipRod()
    {
        GameObject.Enabled = false;
        _rodEquipped = false;
    }
    
    public void ToggleRod()
    {
        if (_rodEquipped)
            UnequipRod();
        else
            EquipRod();
    }
}
