using Sandbox;

public sealed class PlayerState : Component
{
    [Sync]
    public int Money { get; set; } = 0;

    [Rpc.Owner]
    public void AddMoney(int amount)
    {
        Log.Info($"[AddMoney RPC] Called for {Network.OwnerConnection?.DisplayName} with ${amount}");
        Money += amount;
        Log.Info($"💰 Player {Network.OwnerConnection?.DisplayName} now has ${Money} (synced)");
    }
}
