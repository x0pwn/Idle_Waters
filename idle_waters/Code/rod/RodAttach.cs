// File: Code/Player/RodController.cs
using Sandbox;
using System.Linq;

namespace IdleWaters
{
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
            // Find the player's camera if not assigned
            if (PlayerCamera == null)
            {
                var camObj = Scene.GetAllObjects(true)
                                 .FirstOrDefault(go => go.Components.Get<CameraComponent>() != null 
                                                    || go.Name.ToLower().Contains("camera"));
                PlayerCamera = camObj;
                if (PlayerCamera == null)
                    Log.Warning("RodController: Camera not found!");
            }
            
            // Attach RightHand to the correct bone if not assigned at edit time
            if (RightHand != null && RightHand.Parent == null)
            {
                var playerPawn = Scene.GetAllObjects(true)
                                      .FirstOrDefault(go => go.Components.Get<PlayerController>() != null);
                if (playerPawn != null)
                {
                    var skinned = playerPawn.Components
                                              .GetAll<SkinnedModelRenderer>()
                                              .FirstOrDefault(m => m.Model?.ResourcePath.Contains("citizen") == true);
                    if (skinned != null)
                    {
                        RightHand.SetParent(skinned.GameObject);
                        RightHand.Transform.LocalPosition = new Vector3(8, 0, 0);
                        Log.Info("RodController: Hand attachment point created");
                    }
                }
            }
        }
        
        protected override void OnUpdate()
        {
            if (!_rodEquipped) return;
            if (RightHand == null || PlayerCamera == null) return;

            var handPos = RightHand.Transform.Position;
            var camRot  = PlayerCamera.Transform.Rotation;

            if (!hasInitializedPosition)
            {
                lastUsedHandPosition  = handPos;
                lastUsedCameraRotation = camRot;
                hasInitializedPosition = true;
            }

            bool movedEnough = (handPos - lastUsedHandPosition).Length > MovementTolerance;
            var diffRot = camRot.Inverse * lastUsedCameraRotation;
            var diffAngles = diffRot.Angles();
            float angleMag = new Vector3(diffAngles.pitch, diffAngles.yaw, diffAngles.roll).Length;
            bool rotatedEnough = angleMag > RotationTolerance;

            if (movedEnough || rotatedEnough)
            {
                lastUsedHandPosition   = handPos;
                lastUsedCameraRotation = camRot;

                var forward = camRot.Forward;
                forward.z *= 0.3f;
                forward = forward.Normal;

                var baseRot = Rotation.LookAt(forward, Vector3.Up) * Rotation.From(RotationOffset);
                var handleOffset = baseRot * new Vector3(-5, 0, 0);

                targetRotation = baseRot;
                targetPosition = handPos - handleOffset + baseRot * PositionOffset;
            }

            var dt = Time.Delta;
            GameObject.Transform.Position = Vector3.Lerp(
                GameObject.Transform.Position,
                targetPosition,
                dt * PositionSmoothSpeed
            );
            GameObject.Transform.Rotation = Rotation.Slerp(
                GameObject.Transform.Rotation,
                targetRotation,
                dt * RotationSmoothSpeed
            );
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
            if (_rodEquipped) UnequipRod();
            else EquipRod();
        }
    }
}
