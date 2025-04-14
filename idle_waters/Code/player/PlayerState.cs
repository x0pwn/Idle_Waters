using Sandbox;

public sealed class PlayerState : Component
{
	[Property] public int Money { get; private set; } = 0;

	public void AddMoney( int amount )
	{
		Money += amount;
		Log.Info($"💰 Player now has ${Money}");
	}
}
