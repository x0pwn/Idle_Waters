using Sandbox;

public sealed class PlayerState : Component
{
    [Sync]
    public int Money { get; set; } = 0;

    protected override void OnStart()
    {
        if (Network.OwnerConnection == null)
        {
            Log.Warning($"PlayerState created with null owner on {GameObject.Name}; destroying component");
            Destroy(); // Destroy only this component, not the GameObject
            return;
        }
        Log.Info($"PlayerState created for {Network.OwnerConnection?.DisplayName} (ID: {Network.OwnerConnection?.Id})");
    }

    [Rpc.Owner]
    public void AddMoney(int amount)
    {
        Log.Info($"[AddMoney RPC] Called for {Network.OwnerConnection?.DisplayName} with ${amount}");
        Money += amount;
        Log.Info($"💰 Player {Network.OwnerConnection?.DisplayName} now has ${Money} (synced)");
    }
}
