using System.Collections.Generic;
using System.Linq;

namespace BalaurBohemianBroken {
    public class CraftingLogic {
        public static List<ItemComparison> crafting_comparison_stack = new List<ItemComparison>();
        public static bool compare_container_enabled;
        public static bool compare_wearable_enabled;
        public static bool compare_quality_enabled;
        public static bool compare_value_enabled;
        public static bool compare_condition_enabled;

        public static List<Setting> settings = new List<Setting>() {
            new SettingBool {
                name = "compare_container_enabled",
                value = true,
                apply = delegate {
                    compare_container_enabled =
                        Settings.Get<SettingBool>("compare_container_enabled").value;
                    CreateCraftingComparisonStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "compare_wearable_enabled",
                value = true,
                apply = delegate {
                    compare_wearable_enabled =
                        Settings.Get<SettingBool>("compare_wearable_enabled").value;
                    CreateCraftingComparisonStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "compare_quality_enabled",
                value = true,
                apply = delegate {
                    compare_quality_enabled =
                        Settings.Get<SettingBool>("compare_quality_enabled").value;
                    CreateCraftingComparisonStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "compare_value_enabled",
                value = true,
                apply = delegate {
                    compare_value_enabled =
                        Settings.Get<SettingBool>("compare_value_enabled").value;
                    CreateCraftingComparisonStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "compare_condition_enabled",
                value = true,
                apply = delegate {
                    compare_condition_enabled =
                        Settings.Get<SettingBool>("compare_condition_enabled").value;
                    CreateCraftingComparisonStack();
                },
                category = Setting.SettingCategory.Game
            },
        };
        
        // TODO: I STILL get a lag spike when crafting. I've verified however, that it's not because of this mod. It happens regardless. Investigate more?
        public static List<Item> GetItemsForRecipe(Recipe __instance, bool stop_on_null) {
            // This finding code is largely from Recipe.GetItemsForRecipeThorough
            List<Item> itemsForRecipe = new List<Item>();  // This needs to be List, because the Thorough version of the function uses null to signify no available item.
            List<Item> allItemsThorough = BetterInventoryLogic.GetAvailableItems(true);
            
            // Remove favourited items.
            allItemsThorough.RemoveAll(item => item.favourited);
            
            // Go through each recipe item in order, and try to find the best item for that slot.
            foreach (RecipeItem recipe_item in __instance.items) {
                Item best_item = GetBestItemForCraftingSlot(recipe_item, allItemsThorough.ToList());
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

        public static Item GetBestItemForCraftingSlot(RecipeItem slot, List<Item> available_items) {
            List<Item> candidates = new List<Item>();
            
            foreach (Item item in available_items) {
                // I am not completely certain that this has parity with RecipeItem.GetMatchingItem.
                // I don't know why the functions are completely separated as they are.
                if (slot.DoesUseItemType(item))
                    candidates.Add(item);
            }

            // No valid items.
            if (candidates.Count == 0)
                return null;
            
            // Find the best item for this slot
            candidates.Sort((x, y) => CompareCraftingDesirability(x, y, slot));
            return candidates.Last();
        }
        
        public static void CreateCraftingComparisonStack() {
            crafting_comparison_stack = new List<ItemComparison>();

            if (compare_container_enabled) {
                crafting_comparison_stack.Add(new ItemCompareContainer(true));
            }
            
            if (compare_wearable_enabled) {
                crafting_comparison_stack.Add(new ItemCompareWearable(true));
            }
            
            if (compare_quality_enabled) {
                crafting_comparison_stack.Add(new ItemCompareQuality(false));
            }
            
            if (compare_value_enabled) {
                crafting_comparison_stack.Add(new ItemCompareValue(true));
            }
            
            if (compare_condition_enabled) {
                crafting_comparison_stack.Add(new ItemCompareCondition(true));
            }
        }
        
        public static int CompareCraftingDesirability(Item x, Item y, RecipeItem recipe_slot) {
            foreach (ItemComparison comparison in crafting_comparison_stack) {
                int comp_result = comparison.Compare(x, y, recipe_slot);
                if (comp_result != 0)
                    return comp_result;
            }

            return 0;
        }
    }
    
    #region Item comparison classes
    // I use a class for this so that I can easily store and sort the methods in a list.
    public abstract class ItemComparison {
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
        
        public ItemComparison(bool reverse) {
            Reverse = reverse;
        }
        
        public abstract int Compare(Item x, Item y, RecipeItem recipe_slot);
    }

    public class ItemCompareQuality : ItemComparison {
        // I don't like that to inherit a constructor, I need to do this.
        // It feels like I must be missing something.
        // I don't care enough to filter through documentation to find out.
        public ItemCompareQuality(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            // Check how this works with liquids. Source also does this comparison in another branch:
            // (Item.GetQualityThatMeetsCriteria(this.quality, liquidType.GetScaledQualities(liquidStack.amount))
            if (recipe_slot.quality == null)
                return 0;

            if (recipe_slot.isLiquid)
                return CompareLiquids(x, y, recipe_slot);
            return CompareSolids(x, y, recipe_slot);
        }

        private int CompareSolids(Item x, Item y, RecipeItem recipe_slot) {
            // Things without a quality can be passed in if a quality isn't required for the recipe.
            var x_qual = Item.GetQualityThatMeetsCriteria(recipe_slot.quality, x.Stats.qualities);
            if (x_qual == null)
                return 0;
            var y_qual = Item.GetQualityThatMeetsCriteria(recipe_slot.quality, y.Stats.qualities);
            if (y_qual == null)
                return 0;
            float x_amount = Item.GetQualityThatMeetsCriteria(recipe_slot.quality, x.Stats.qualities).amount;
            float y_amount = Item.GetQualityThatMeetsCriteria(recipe_slot.quality, y.Stats.qualities).amount;
            return x_amount.CompareTo(y_amount)  * reverse_sign;
        }

        private int CompareLiquids(Item x, Item y, RecipeItem recipe_slot) {
            // Largely adapted from decomp in RecipeItem.GetMatchingItem
            WaterContainerItem x_liquid;
            x.TryGetComponent<WaterContainerItem>(out x_liquid);
            WaterContainerItem y_liquid;
            y.TryGetComponent<WaterContainerItem>(out y_liquid);
            if (x_liquid == null) {
                if (y_liquid == null) {
                    return 0;
                }
                return -1;
            }
            if (y_liquid == null) {
                return 1;
            }

            if (recipe_slot.specific)
                return x_liquid.AmountOf(recipe_slot.specificId).CompareTo(y_liquid.AmountOf(recipe_slot.specificId));

            float x_amount = 0, y_amount = 0;
            foreach (LiquidStack liquid_stack in x_liquid.stack) {
                LiquidType liquid_type = Liquids.Registry[liquid_stack.liquidId];
                var qual = Item.GetQualityThatMeetsCriteria(recipe_slot.quality,  liquid_type.GetScaledQualities(liquid_stack.amount));
                if (qual == null)
                    break;
                x_amount = qual.amount;
            }
            
            foreach (LiquidStack liquid_stack in y_liquid.stack) {
                LiquidType liquid_type = Liquids.Registry[liquid_stack.liquidId];
                var qual = Item.GetQualityThatMeetsCriteria(recipe_slot.quality,  liquid_type.GetScaledQualities(liquid_stack.amount));
                if (qual == null)
                    break;
                y_amount = qual.amount;
            }

            return x_amount.CompareTo(y_amount);
        }
    }

    public class ItemCompareValue : ItemComparison {
        public ItemCompareValue(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            return x.Stats.GetValue(x).CompareTo(y.Stats.GetValue(y)) * reverse_sign;
        }
    }

    public class ItemCompareCondition : ItemComparison {
        public ItemCompareCondition(bool reverse) : base(reverse) {
            
        }
        
        public override int Compare(Item x, Item y, RecipeItem recipe_slot) {
            return x.condition.CompareTo(y.condition) * reverse_sign;
        }
    }

    public class ItemCompareContainer : ItemComparison {
        public ItemCompareContainer(bool reverse) : base(reverse) {
            
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
        }
    }

    public class ItemCompareWearable : ItemComparison {
        public ItemCompareWearable(bool reverse) : base(reverse) {
            
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