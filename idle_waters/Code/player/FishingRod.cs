using Sandbox;
using IdleWaters;  

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

                var state = GameObject.Components.Get<FishingState>();
                state?.RequestCastRpc(GameObject.Transform.Position, Connection.Local.Id);

                if (fishingLine != null && RodTip != null)
                {
                    // Use the CastWherePlayerLooking method instead of StartCasting
                    Log.Info("FishingRod: Calling CastWherePlayerLooking");
                    fishingLine.CastWherePlayerLooking();
                }
                else
                {
                    Log.Info("FishingRod: Cannot cast - fishingLine: " +
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
