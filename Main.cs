// using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

// TODO: Make crafting status update after properly crafting stuff.
namespace BalaurBohemianBroken {
    [BepInPlugin("com.balaur.BetterLogic", "BetterInventoryLogic", "0.1.0")]
    public class BetterInventoryLogic : BaseUnityPlugin {
        public static BetterInventoryLogic instance;
        
        public static List<CraftingComparison> comparison_stack = new List<CraftingComparison>() {
            // TODO: Items not in inventory sort.
            new CraftingCompareContainer(true),
            new CraftingCompareWearable(true),
            new CraftingCompareQuality(false),
            new CraftingCompareValue(false),
            new CraftingCompareCondition(true),
        };
        
        public void Awake() {
            instance = this;
            Harmony harmony = new Harmony("com.balaur.BetterLogic");
            MethodInfo target, patch;
            
            target = typeof(Recipe).GetMethod("GetItemsForRecipe");
            patch = typeof(BetterInventoryLogic).GetMethod("PrefixGetItemsForRecipe");
            harmony.Patch(target, prefix: new HarmonyMethod(patch));
            
            target = typeof(Recipe).GetMethod("GetItemsForRecipeThorough");
            patch = typeof(BetterInventoryLogic).GetMethod("PrefixGetItemsForRecipeThorough");
            harmony.Patch(target, prefix: new HarmonyMethod(patch));
        }

        public static bool PrefixGetItemsForRecipe(Recipe __instance, ref List<Item> __result) {
            __result = CraftingLogic(__instance, true);
            return false;
        }
        
        public static bool PrefixGetItemsForRecipeThorough(Recipe __instance, ref List<Item> __result) {
            __result = CraftingLogic(__instance, false);
            return false;
        }

        [CanBeNull]
        public static List<Item> CraftingLogic(Recipe __instance, bool stop_on_null) {
            // TODO: Click on an item in the crafting window to tell recipe to not use it.
            
            // This finding code is largely from Recipe.GetItemsForRecipe
            // TODO: Convert to HashSet when I confirm parity.
            List<Item> itemsForRecipe = new List<Item>();
            List<Item> allItemsThorough = PlayerCamera.main.body.GetAllItemsThorough();
            
            // Find items. Taken and cleaned from decomp.
            Vector2 position = PlayerCamera.main.body.transform.position;
            int mask = LayerMask.GetMask("Item");
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, 10f, mask))  {
                if (collider.TryGetComponent<Item>(out Item component) && PlayerCamera.main.body.DoPickupCheck(component, true))
                    allItemsThorough.Add(component);
            }
            
            // Remove favourited items.
            allItemsThorough.RemoveAll(item => item.favourited);
            
            // Go through each recipe item in order, and try to find the best item for that slot.
            foreach (RecipeItem recipe_item in __instance.items) {
                Item best_item = GetBestItemForSlot(recipe_item, allItemsThorough);
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
        
        [CanBeNull]
        public static Item GetBestItemForSlot(RecipeItem slot, List<Item> available_items) {
            List<Item> candidates = new List<Item>();
            
            // Get all candidates. Pull all matching items until we run out.
            while (true) {
                // TODO: This does make it sort through the whole list many times. Very wasteful.
                // If performance becomes an issue, I can reimplement this function to let it check item-by-item.
                Item candidate = slot.GetMatchingItem(available_items, candidates);
                if (candidate == null)
                    break;
                candidates.Add(candidate);
            }

            // No valid items.
            if (candidates.Count == 0)
                return null;
            
            // Find the best item for this slot
            
            candidates.Sort((x, y) => CompareCraftingDesirability(x, y, slot));
            return candidates.First();
        }

        public static int CompareCraftingDesirability(Item x, Item y, RecipeItem recipe_slot) {
            foreach (CraftingComparison comparison in comparison_stack) {
                int comp_result = comparison.Compare(x, y, recipe_slot);
                instance.Logger.LogInfo(comp_result);
                if (comp_result != 0)
                    return comp_result;
            }

            return 0;
        }
        
        private void StorageLogic() {
            // Store in container with type specification.
            // Store in container with highest reduction.
            // Store in most full container.
            // Store in highest durability container.
            // Store in first container.
        }

        private void SortingLogic() {
            // Sort by weight
            // Sort by value
            // Sort by name
            // Sort by type
            // Default is a blend of all.
        }
    }

    #region Comparison classes/methods
    // I use an interface for this so that I can easily store and sort the methods in a list.
    public abstract class CraftingComparison {
        private bool _reverse = false;
        public bool Reverse {
            get {
                return _reverse;
            }
            set {
                _reverse = value;
                reverse_sign = Reverse ? -1 : 1;
            }
        }
        protected int reverse_sign = 1;  // Used in methods to invert the result if needed.
        
        public CraftingComparison(bool reverse) {
            Reverse = reverse;
        }
        
        public abstract int Compare(Item x, Item y, RecipeItem recipe_slot);
    }

    public class CraftingCompareQuality : CraftingComparison {
        // I don't like that to inherit a constructor, I need to do this.
        // It feels like I must be missing something.
        // I don't care enough to filter through documentation to find out.
        public CraftingCompareQuality(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            // Check how this works with liquids. Source also does this comparison in another branch:
            // (Item.GetQualityThatMeetsCriteria(this.quality, liquidType.GetScaledQualities(liquidStack.amount))
            float x_amount = Item.GetQualityThatMeetsCriteria(recipe_slot.quality, x.Stats.qualities).amount;
            float y_amount = Item.GetQualityThatMeetsCriteria(recipe_slot.quality, y.Stats.qualities).amount;
            return x_amount.CompareTo(y_amount)  * reverse_sign;
        }
    }

    public class CraftingCompareValue : CraftingComparison {
        public CraftingCompareValue(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            return x.Stats.GetValue(x).CompareTo(y.Stats.GetValue(y)) * reverse_sign;
        }
    }

    public class CraftingCompareCondition : CraftingComparison {
        public CraftingCompareCondition(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            return x.condition.CompareTo(y.condition) * reverse_sign;
        }
    }

    public class CraftingCompareContainer : CraftingComparison {
        public CraftingCompareContainer(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            if (x.container == null) {
                if (y.container == null)
                    return 0;
                return -1 * reverse_sign;
            }
            if (y.container == null)
                return 1 * reverse_sign;
            return 0;
            // TODO: Maybe if two containers match, take the one with the smaller capacity?
            // The chance of this, along with all other factors matching, is very small.
            // Maybe it should be its own comparer instead, so players can choose its priority.
        }
    }

    public class CraftingCompareWearable : CraftingComparison {
        public CraftingCompareWearable(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            var xs = x.Stats;
            var ys = y.Stats;
            if (xs.wearable) {
                if (ys.wearable)
                    return 0;
                return 1 * reverse_sign;
            }
            if (ys.wearable)
                return -1 * reverse_sign;
            return 0;
        }
    }
    #endregion
}