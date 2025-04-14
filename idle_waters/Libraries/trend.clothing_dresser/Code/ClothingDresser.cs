
using Sandbox;
using System.Collections.Generic;

[Icon( "checkroom" )]
[Category( "Citizen" )]
public sealed class ClothingDresser : Component, Component.ExecuteInEditor
{
	public Dictionary<GameObject, Clothing> EnabledClothing { get; set; } = new();


	protected override void OnUpdate()
	{

	}
}
