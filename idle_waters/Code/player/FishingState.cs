using Sandbox;
using System.Threading.Tasks;

public sealed class FishingState : Component
{
    [Rpc.Broadcast]
    public async void RequestCastRpc()
    {
        if ( Rpc.Caller == null || !Rpc.Caller.IsHost )
            return;

        Log.Info("🎣 [Server] Cast started...");

        await Task.DelaySeconds(5f);

        var fish = FishLootTable.GenerateFish();
        Log.Info($"🎣 [Server] Caught {fish.Name} worth ${fish.Value}");

        NotifyCatchRpc(fish.Name, fish.Value);
    }

	[Rpc.Owner]
	public void NotifyCatchRpc( string fishName, int value )
	{
		Log.Info($"🎣 You caught a {fishName} worth ${value}");

		// Reset fishing
		var rod = GameObject.Components.Get<FishingRod>();
		rod?.SetCastingComplete();

		// Reward player
		var playerState = GameObject.Components.Get<PlayerState>();
		playerState?.AddMoney(value);
	}
}
