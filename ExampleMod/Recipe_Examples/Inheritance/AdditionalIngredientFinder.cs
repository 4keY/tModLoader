﻿using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ExampleMod.Recipe_Examples.Inheritance
{
	// Is a RecipeFinder, but will find a recipe always with a item*stack as ingredient
	public class AdditionalIngredientFinder : RecipeFinder
	{
		private readonly int _itemType;
		private readonly int _itemStack;

		public AdditionalIngredientFinder(int itemType, int itemStack = 1)
		{
			_itemType = itemType;
			_itemStack = itemStack;
		}

		// By using the new keyword, we can hide the original method
		// (this sort of functions as an override in a way)

		public new Recipe FindExactRecipe()
		{
			AddIngredient(_itemType, _itemStack);
			return base.FindExactRecipe();
		}

		public new List<Recipe> SearchRecipes()
		{
			AddIngredient(_itemType, _itemStack);
			return base.SearchRecipes();
		}
	}
}
