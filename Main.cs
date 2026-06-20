// using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

// TODO: This likely is incompatible with QoL. I need to test and report.
// TODO: Make crafting status update after properly crafting stuff.
namespace BalaurBohemianBroken {
    [BepInPlugin("com.balaur.BetterLogic", "BetterInventoryLogic", "0.1.4")]
    public class BetterInventoryLogic : BaseUnityPlugin {
        public static BetterInventoryLogic instance;
        
        public void Awake() {
            instance = this;
            Harmony harmony = new Harmony("com.balaur.BetterLogic");
            harmony.PatchAll();
        }
        
        // TODO: I get a lag spike when crafting. I suspect it comes from the game's RecipeItem.GetMatchingItem being called multiple times.
        public static List<Item> CraftingLogic(Recipe __instance, bool stop_on_null) {
            // TODO: Click on an item in the crafting window to tell recipe to not use it.
            
            // This finding code is largely from Recipe.GetItemsForRecipeThorough
            List<Item> itemsForRecipe = new List<Item>();  // This needs to be List, because the Thorough version of the function uses null to signify no available item.
            List<Item> allItemsThorough = InventoryLogic.GetAvailableItems(true);
            
            // Remove favourited items.
            allItemsThorough.RemoveAll(item => item.favourited);
            
            // Go through each recipe item in order, and try to find the best item for that slot.
            foreach (RecipeItem recipe_item in __instance.items) {
                Item best_item = InventoryLogic.GetBestItemForCraftingSlot(recipe_item, allItemsThorough.ToList());
                if (best_item == null && stop_on_null) {
                    return null;
                }
                    
                itemsForRecipe.Add(best_item);
                // If it gets destroyed, we can't use it multiple times in the recipe!
                // Unless it's liquid I guess. This comes from the source code in Recipe.GetItemsForRecipe
                if (best_item != null && recipe_item.destroyItem && !recipe_item.isLiquid) {
                    allItemsThorough.Remove(best_item);
                }
            }

            return itemsForRecipe;
        }
        
        // TODO: Drop all button for container?
        private void SortingLogic() {
            // Sort by weight
            // Sort by value
            // Sort by name
            // Sort by type
            // Default is a blend of all.
        }
    }
}