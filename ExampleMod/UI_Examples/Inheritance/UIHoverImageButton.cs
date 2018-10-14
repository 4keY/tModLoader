﻿using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ExampleMod.UI_Examples.Inheritance
{
	// This UIHoverImageButton class inherits from UIImageButton. 
	// Inheriting is a great tool for UI design. 
	// By inheriting, we get the Image drawing, MouseOver sound, and fading for free from UIImageButton
	// We've added some code to allow the Button to show a text tooltip while hovered. 
	internal class UIHoverImageButton : UIImageButton
	{
		private string _hoverText;
		public string HoverText
		{
			get => _hoverText;
			set => _hoverText = SetHoverText(value);
		}

		public UIHoverImageButton(Texture2D texture, string hoverText) : base(texture)
		{
			HoverText = hoverText;
		}

		private static string SetHoverText(string newHoverText)
		{
			// Doing it this variant allows you to add custom validation for the string (none here)
			// Alternatively, you place this directly in the property's setter
			return newHoverText;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);
			if (IsMouseHovering)
			{
				// the ?? signifies null-collation (the null coalescing operator)
				// and will fall back to the right-side value if the left-side value is null
				// (string is a nullable therefore it isn't unusual to check for null values)
				Main.hoverItemName = HoverText ?? string.Empty;
			}
		}
	}
}
