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
    
    private bool _rodEquipped = true;
    
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
            
            var cameraForward = PlayerCamera.Transform.Rotation.Forward;
            
            var horizontalForward = cameraForward;
            horizontalForward.z *= 0.3f; 
            horizontalForward = horizontalForward.Normal;
            
            var targetRotation = Rotation.LookAt(horizontalForward, Vector3.Up);
            targetRotation *= Rotation.From(RotationOffset);
            
            var handleOffset = new Vector3(-5, 0, 0);
            var rotatedHandleOffset = targetRotation * handleOffset;
            
            var finalPosition = handPosition - rotatedHandleOffset + targetRotation * PositionOffset;
            
            GameObject.Transform.Position = finalPosition;
            GameObject.Transform.Rotation = targetRotation;
            
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
