using Sandbox;
using Sandbox.UI;

[Library]
public class CatchNotification : PanelComponent
{
    public string FishName { get; set; }
    public int FishValue { get; set; }
    private Label TextLabel { get; set; }
    private float TimeSinceCreated { get; set; }

    protected override void OnStart()
    {
        base.OnStart();
        Panel.AddClass("catch-notification");

        // Create a Label child to display text
        TextLabel = Panel.AddChild<Label>();
        TextLabel.AddClass("catch-text");

        // Set initial text
        UpdateText();
    }

    protected override void OnUpdate()
    {
        TimeSinceCreated += Time.Delta;

        // Destroy after 3 seconds
        if (TimeSinceCreated > 3f)
        {
            GameObject.Destroy();
        }
    }

    private void UpdateText()
    {
        if (TextLabel == null) return;

        if (FishValue > 0)
            TextLabel.Text = $"You caught a {FishName} worth ${FishValue}!";
        else
            TextLabel.Text = "You didn’t catch anything.";
    }
}
