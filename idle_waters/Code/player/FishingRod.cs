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
			isFishing = true;
			lastCast = 0;

			var state = GameObject.Components.Get<FishingState>();
			state?.RequestCastRpc(GameObject.Transform.Position); // ✅ Called directly
		}
	}

	public void SetCastingComplete()
	{
		isFishing = false;
	}
}
