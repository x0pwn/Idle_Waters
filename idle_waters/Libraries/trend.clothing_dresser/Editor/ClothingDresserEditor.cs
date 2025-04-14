using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;
using Sandbox.UI;
using Button = Editor.Button;
using Checkbox = Editor.Checkbox;

[CustomEditor( typeof(ClothingDresser) )]
public class ClothingDresserEditor : ComponentEditorWidget
{
	public ClothingDresserEditor( SerializedObject obj ) : base( obj )
	{
		Layout = Layout.Column();
		Layout.Margin = new Margin( 15, 5 );

		var tabBar = new SegmentedControl( this );
		tabBar.SetSizeMode( SizeMode.Flexible, SizeMode.CanShrink );
		tabBar.FixedHeight = 35f;

		var row = Layout.AddRow();
		row.Add( tabBar );

		var categories = Enum.GetValues<Clothing.ClothingCategory>();

		foreach ( var category in categories )
		{
			tabBar.AddOption( category.ToString() );
		}

		tabBar.OnSelectedChanged += tab =>
		{
			if ( tab == "None" )
			{
				ClothesList.BuildItems( AllClothing );
				return;
			}

			ClothesList.BuildItems( AllClothing.Where( x => x.Category.ToString() == tab ) );
		};


		var dresser = SerializedObject.Targets.FirstOrDefault() as ClothingDresser;

		var secondRow = Layout.AddRow();

		Filter = new LineEdit();
		Filter.PlaceholderText = "Filter..";
		Filter.FixedHeight = 30f;
		Filter.TextEdited += text =>
		{
			if ( !string.IsNullOrEmpty( text ) )
			{
				// Log.Info( text );
				SearchQuery = text;
				ClothesList.BuildItems( AllClothing.Where( x =>
					x.Title.Contains( SearchQuery, StringComparison.OrdinalIgnoreCase ) ) );
				return;
			}

			ClothesList.BuildItems( AllClothing );
		};


		var resetClothing = new Button( "Reset Clothing" );
		resetClothing.Icon = "refresh";
		resetClothing.Tint = new Color( 179, 72, 64 );
		resetClothing.SetStyles( "font-size: 13px; padding: 8px;" );

		resetClothing.Clicked = () =>
		{
			foreach ( var kv in dresser?.EnabledClothing )
			{
				kv.Key.Destroy();
			}

			dresser?.EnabledClothing.Clear();
		};

		var checkbox = new Checkbox();
		checkbox.SetStyles( "padding: 10px;" );
		checkbox.Text = "Show Enabled";
		checkbox.Toggled += () =>
		{
			if ( checkbox.Value )
			{
				ClothesList.BuildItems( AllClothing.Where( x => dresser.EnabledClothing.ContainsValue( x ) ) );
			}
			else
			{
				ClothesList.BuildItems( AllClothing );
			}
		};

		secondRow.Margin = new Margin( 0, 10 );
		secondRow.Add( Filter );
		secondRow.Add( checkbox );
		secondRow.AddStretchCell();
		secondRow.Add( resetClothing );

		ClothesList = new ClothesList( null, dresser, AllClothing );

		var tlayout = Layout.AddColumn();
		tlayout.Spacing = 8;
		tlayout.Add( ClothesList );
	}

	public static List<Clothing> AllClothing => ResourceLibrary.GetAll<Clothing>().ToList();

	public static LineEdit Filter { get; set; }

	public ClothesList ClothesList { get; set; }

	public static string SearchQuery { get; set; } = string.Empty;
}
