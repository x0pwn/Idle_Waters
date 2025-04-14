using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;
using Application = Editor.Application;

public class ClothesList : ListView
{
	private readonly ClothingDresser Dresser;


	public ClothesList( Widget parent, ClothingDresser dresser, IEnumerable<Clothing> clothingFilter ) : base( parent )
	{
		Instance = this;

		ItemContextMenu = ShowItemContext;
		ItemClicked = OnItemClicked;
		MinimumHeight = 500;
		//ItemAlign = Sandbox.UI.Align.SpaceBetween;

		Dresser = dresser;

		ItemSize = new Vector2( 96, 114 );

		if ( clothingFilter != null && clothingFilter.Any() )
		{
			BuildItems( clothingFilter );
		}
	}

	public static ClothesList Instance { get; private set; }

	protected void OnItemClicked( object obj )
	{
		if ( obj is not Clothing entry )
		{
			return;
		}

		var citizen = Dresser.Components.Get<SkinnedModelRenderer>();
		var asset = AssetSystem.FindByPath( entry.ResourcePath );

		if ( Dresser.EnabledClothing.ContainsValue( entry ) )
		{
			var clothingEntry = Dresser.EnabledClothing.Where( x => x.Value == entry ).FirstOrDefault();

			clothingEntry.Key.Destroy();
			Dresser.EnabledClothing.Remove( clothingEntry.Key );
			return;
		}

		if ( entry.SlotsUnder == Clothing.Slots.Skin )
		{
			if ( entry.SkinMaterial != null && entry.EyesMaterial != null )
			{
				citizen.SetMaterialOverride( Material.Load( entry.SkinMaterial ), "skin" );
				citizen.SetMaterialOverride( Material.Load( entry.EyesMaterial ), "eyes" );
			}

			return;
		}

		var clonedClothing = Dresser.Scene.CreateObject();
		clonedClothing.Parent = Dresser.GameObject;
		clonedClothing.Name = $"Clothing - {asset.Name}";

		var cloth = clonedClothing.Components.Create<SkinnedModelRenderer>();
		cloth.Model = Model.Load( entry.Model );

		cloth.BoneMergeTarget = citizen;
		cloth.Tags.Add( "clothing" );

		//Log.Info( $"{cloth.GameObject.Id}, {entry.Title}" );
		Dresser?.EnabledClothing.Add( cloth.GameObject, entry );
	}

	private void ShowItemContext( object obj )
	{
		if ( obj is not Clothing entry )
		{
			return;
		}

		var m = new Menu();

		if ( Dresser.EnabledClothing.ContainsValue( entry ) )
		{
			m.AddOption( "Remove Clothing", "checkroom", () =>
			{
				var clothingEntry = Dresser.EnabledClothing.Where( x => x.Value == entry ).FirstOrDefault();

				clothingEntry.Key.Destroy();
				Dresser.EnabledClothing.Remove( clothingEntry.Key );
			} );
		}

		m.AddOption( "Open In Editor", "edit", () =>
		{
			var asset = AssetSystem.FindByPath( entry.ResourcePath );
			asset?.OpenInEditor();
		} );

		m.OpenAt( Application.CursorPosition );
	}

	public void BuildItems( IEnumerable<object> objects )
	{
		SetItems( objects );
	}

	protected override void PaintItem( VirtualWidget item )
	{
		var rect = item.Rect.Shrink( 0, 0, 0, 15 );

		if ( item.Object is not Clothing clothing )
		{
			return;
		}

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		var asset = AssetSystem.FindByPath( clothing.ResourcePath );

		if ( asset is null )
		{
			Paint.SetDefaultFont();
			Paint.SetPen( Color.Red );
			Paint.DrawText( item.Rect.Shrink( 2 ), "<ERROR>" );
			return;
		}

		if ( Dresser.EnabledClothing.ContainsValue( clothing ) )
		{
			Paint.ClearPen();
			Paint.SetBrush( item.Hovered ? Theme.Red.WithAlpha( 0.10f ) : Theme.Green.WithAlpha( 0.10f ) );
			Paint.SetPen( item.Hovered ? Theme.Red.WithAlpha( 0.50f ) : Theme.Green.WithAlpha( 0.90f ) );
			Paint.DrawRect( item.Rect.Shrink( 2 ), 3 );
		}

		if ( Paint.HasMouseOver )
		{
			Paint.SetBrush( Theme.Blue.WithAlpha( item.Selected ? 0.2f : 0.2f ) );
			Paint.ClearPen();
			Paint.DrawRect( item.Rect, 4 );
		}

		var pixmap = asset.GetAssetThumb();

		Paint.ClearPen();
		Paint.SetBrush( Theme.White.WithAlpha( 0.01f ) );
		Paint.SetPen( Theme.White.WithAlpha( 0.05f ) );
		Paint.DrawRect( item.Rect.Shrink( 2 ), 3 );

		Paint.Draw( item.Rect.Shrink( item.Hovered ? 2 : 6 ), pixmap );


		var textRect = rect.Shrink( 4 );
		textRect.Top = textRect.Top + 50;
		textRect.Top = textRect.Top + 25;

		Paint.ClearPen();
		Paint.SetBrush( Theme.Black.WithAlpha( 0.5f ) );
		Paint.DrawRect( textRect, 0.0f );

		Paint.Antialiasing = true;

		Paint.SetPen( Theme.White, 2.0f );
		Paint.ClearBrush();
		Paint.SetFont( "Roboto Condensed", 9, 700 );
		Paint.DrawText( textRect, clothing.Title );
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, 4 );

		base.OnPaint();
	}
}
