using Sandbox;

[Title("Fishing Rod")]
[Category("Idle Waters")]
[Icon("fishing_hook")]
public sealed class FishingRod : Component
{
	[Property] public float CastCooldown { get; set; } = 2f;

	private bool isFishing = false;
	private TimeSince lastCast;

	protected override void OnUpdate()
	{
		if ( !IsProxy && Input.Pressed("Use") && !isFishing && lastCast > CastCooldown )
		{
			var spot = FindNearbyFishingSpot();
			if ( spot != null )
			{
				Log.Info("🎣 Sending cast request to server...");
				isFishing = true;
				lastCast = 0;

				var state = GameObject.Components.Get<FishingState>();
				state?.RequestCastRpc(); // 🚀 sends to server
			}
			else
			{
				Log.Info("❌ Not near a fishing spot.");
			}
		}
	}

	private FishingSpot FindNearbyFishingSpot()
	{
		foreach ( var spot in Scene.GetAllComponents<FishingSpot>() )
		{
			if ( spot.IsInRange(Transform.Position) )
				return spot;
		}

		return null;
	}

	public void SetCastingComplete()
	{
		isFishing = false;
	}
}
