// File: Code/Player/FishingRod.cs
using Sandbox;
using IdleWaters;  // <— bring in the namespace where FishingState lives

namespace IdleWaters
{
    [Title("Fishing Rod")]
    [Category("Idle Waters")]
    [Icon("fishing_hook")]
    public sealed class FishingRod : Component
    {
        [Property] public float CastCooldown { get; set; } = 2f;
        [Property] public GameObject RodTip { get; set; }

        private bool isFishing = false;
        private TimeSince lastCast;
        private FishingLine fishingLine;

        protected override void OnStart()
        {
            if (RodTip != null)
            {
                fishingLine = RodTip.Components.Get<FishingLine>();
                Log.Info("FishingRod: FishingLine found on RodTip: " + (fishingLine != null));
            }
            else
            {
                Log.Info("FishingRod: RodTip is not assigned!");
            }
        }

        protected override void OnUpdate()
        {
            if (!IsProxy && Input.Pressed("Use") && !isFishing && lastCast > CastCooldown)
            {
                Log.Info("FishingRod: Initiating cast");
                isFishing = true;
                lastCast = 0;

                // Now FishingState is in the same namespace, so this resolves
                var state = GameObject.Components.Get<FishingState>();
                state?.RequestCastRpc(GameObject.Transform.Position);

                if (fishingLine != null && RodTip != null)
                {
                    var forward = Transform.Rotation.Forward;
                    var tempTarget = Transform.Position + forward * 100f;
                    Log.Info("FishingRod: Calling StartCasting with target: " + tempTarget);
                    fishingLine.StartCasting(tempTarget);
                }
                else
                {
                    Log.Info("FishingRod: Cannot call StartCasting - fishingLine: " +
                             (fishingLine == null ? "null" : "set") +
                             ", RodTip: " + (RodTip == null ? "null" : "set"));
                }
            }
        }

        public void SetCastingComplete()
        {
            Log.Info("FishingRod: SetCastingComplete called");
            isFishing = false;
            fishingLine?.StopCasting();
        }
    }
}
