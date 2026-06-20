using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BalaurBohemianBroken {
    public class InventoryLogic {
        public static List<ItemComparison> crafting_comparison_stack = new List<ItemComparison>();
        public static bool compare_container_enabled;
        public static bool compare_wearable_enabled;
        public static bool compare_quality_enabled;
        public static bool compare_value_enabled;
        public static bool compare_condition_enabled;

        public static List<LiquidStorageComparison> liquid_comparisons_stack = new List<LiquidStorageComparison>();
        public static bool compare_liquid_unmixed_enabled;
        public static bool compare_liquid_stacking_enabled;
        public static bool compare_liquid_weight_enabled;
        
        public static bool store_in_containers_first;
        public static List<StorageComparison> storage_comparison_stack = new List<StorageComparison>();
        public static bool compare_storage_type;
        public static bool compare_storage_reduction;
        public static bool compare_storage_full;
        public static bool compare_storage_capacity;
        public static bool compare_storage_best_condition;
        
        public static List<Item> GetAvailableItems(bool include_pickups) {
            List<Item> all_items = PlayerCamera.main.body.GetAllItemsThorough();

            if (!include_pickups)
                return all_items;
            // Find items. Taken and cleaned from decomp.
            Vector2 position = PlayerCamera.main.body.transform.position;
            int mask = LayerMask.GetMask("Item");
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, 10f, mask))  {
                if (collider.TryGetComponent<Item>(out Item component) && PlayerCamera.main.body.DoPickupCheck(component, true))
                    all_items.Add(component);
            }

            return all_items;
        }
        
        public static bool StoreLiquid(string item_id, float amount) {
            // Find suitable liquid containers.
            var best_bottle = FindBestBottle(item_id, amount);
            if (best_bottle != null) {
                best_bottle.AddLiquid(item_id, amount);
                return true;
            }
            return false;
        }
        
        public static void PickUpItem(Item item) {
            // Based on Body.AutoPickUpItem
            Body p = PlayerCamera.main.body;
            
            // Try pick item up.
            if (!store_in_containers_first) {
                var slot = p.FirstEmptySlot();
                if (slot != null) {
                    p.PickUpItem(item, slot.Value, true);
                    return;
                }
            }

            // Try store item.
            List<Container> candidates = new List<Container>();
            foreach (Item surfaceInventoryItem in p.GetSurfaceInventoryItems()) {
                Container c = surfaceInventoryItem.container; 
                if (surfaceInventoryItem.container == null)
                    continue;
                if (!c.CanHoldItem(item))
                    continue;
                candidates.Add(c);
            }
            
            // Try pick item up.
            if (candidates.Any()) {
                candidates.Sort((x, y) => CompareContainerDesirability(x, y, item));
                Container container = candidates.Last();
                container.LoadItem(item);
                return;
            }

            if (store_in_containers_first) {
                var slot = p.FirstEmptySlot();
                if (slot != null) {
                    p.PickUpItem(item, slot.Value, true);
                }
            }
        }
        
        public static WaterContainerItem FindBestBottle(string liquid_id, float liquid_amount) {
            // TODO: These to settings.
            bool can_fill_new_bottles = true;
            bool prefer_better_weight_ratio = true;
            // TODO: These
            // bool prefer_not_falling_from_inventory = true;
            // bool prevent_falling_from_inventory = true;
            
            // TODO: Fill bottle, and move on to next bottle if there is any left over.
            List<WaterContainerItem> candidates = new List<WaterContainerItem>();
            foreach (Item player_item in PlayerCamera.main.body.GetAllItemsThorough()) {
                WaterContainerItem liquid_container;
                if (!player_item.TryGetComponent<WaterContainerItem>(out liquid_container))
                    continue;
                if (liquid_container.SpaceLeft < liquid_amount)
                    continue;
                candidates.Add(liquid_container);
            }

            if (candidates.Count == 0)
                return null;

            candidates.Sort((x, y) => CompareLiquidContainerDesirability(x, y, liquid_id, liquid_amount));
            return candidates.Last();
        }
        
        public static Item GetBestItemForCraftingSlot(RecipeItem slot, List<Item> available_items) {
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
            return candidates.Last();
        }
        
        public static int CompareLiquidContainerDesirability(WaterContainerItem x, WaterContainerItem y, string liquid_id, float amount) {
            foreach (LiquidStorageComparison comparison in liquid_comparisons_stack) {
                int comp_result = comparison.Compare(x, y, liquid_id, amount);
                if (comp_result != 0)
                    return comp_result;
            }

            return 0;
        }

        public static int CompareCraftingDesirability(Item x, Item y, RecipeItem recipe_slot) {
            foreach (ItemComparison comparison in crafting_comparison_stack) {
                int comp_result = comparison.Compare(x, y, recipe_slot);
                if (comp_result != 0)
                    return comp_result;
            }

            return 0;
        }

        public static int CompareContainerDesirability(Container x, Container y, Item item) {
            foreach (StorageComparison comparison in storage_comparison_stack) {
                int comp_result = comparison.Compare(x, y, item);
                if (comp_result != 0)
                    return comp_result;
            }
            return 0;
        }
        
        public static void CreateLiquidComparisonsStack() {
            liquid_comparisons_stack = new List<LiquidStorageComparison>();
            if (compare_liquid_unmixed_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidUnmixed());
            if (compare_liquid_stacking_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidStacking());
            if (compare_liquid_weight_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidWeightRatio());
        }

        public static void CreateCraftingComparisonStack() {
            // TODO: Allow users to organize the stack themselves.
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
        
        public static void CreateStorageComparisonStack() {
            // TODO: Allow users to organize the stack themselves.
            storage_comparison_stack = new List<StorageComparison>();

            if (compare_storage_type) {
                storage_comparison_stack.Add(new StorageCompareType());
            }
            if (compare_storage_reduction) {
                storage_comparison_stack.Add(new StorageCompareWeightReduction());
            }
            if (compare_storage_full) {
                storage_comparison_stack.Add(new StorageCompareMostFull());
            }
            if (compare_storage_capacity) {
                storage_comparison_stack.Add(new StorageCompareLargest());
            }
            if (compare_storage_best_condition) {
                storage_comparison_stack.Add(new StorageCompareBestCondition());
            }
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
            // TODO: Maybe if two containers match, take the one with the smaller capacity?
            // The chance of this, along with all other factors matching, is very small.
            // Maybe it should be its own comparer instead, so players can choose its priority.
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
    
    #region Liquid comparison classes
    public abstract class LiquidStorageComparison {
        public abstract int Compare(WaterContainerItem x, WaterContainerItem y, string liquid_id, float quantity);
    }

    public class CompareLiquidStacking : LiquidStorageComparison {
        public override int Compare(WaterContainerItem x, WaterContainerItem y, string liquid_id, float quantity) {
            float x_amount = x.AmountOf(liquid_id);
            float y_amount = y.AmountOf(liquid_id);
            return x_amount.CompareTo(y_amount);
        }
    }

    public class CompareLiquidWeightRatio : LiquidStorageComparison {
        public override int Compare(WaterContainerItem x, WaterContainerItem y, string liquid_id, float quantity) {
            // Which container is going to increase our weight the least by stacking in to it?
            // This doesn't account for containers that don't scale their weight.
            // The reasoning for this is that a container with a lower ratio of weight when filled is always more desirable.
            // e.g. filling a canteen doesn't increase your weight at all, but canteens are pretty mediocre liquid storage.
            float x_max_weight = x.item.Stats.weight;
            float x_weight_per_capacity = x_max_weight / x.Capacity;
            
            float y_max_weight = y.item.Stats.weight;
            float y_weight_per_capacity = y_max_weight / y.Capacity;

            // Inverted because lower value is more desirable.
            return x_weight_per_capacity.CompareTo(y_weight_per_capacity) * -1;
        }
    }

    public class CompareLiquidUnmixed : LiquidStorageComparison {
        public override int Compare(WaterContainerItem x, WaterContainerItem y, string liquid_id, float quantity) {
            int x_unmatched_liquids = x.stack.Count;
            int y_unmatched_liquids = y.stack.Count;
            if (x.HasLiquid(liquid_id))
                x_unmatched_liquids--;
            if (y.HasLiquid(liquid_id))
                y_unmatched_liquids--;
            
            return x_unmatched_liquids.CompareTo(y_unmatched_liquids) * -1;
        }
    }
    #endregion
    
    #region Storage comparison classes
    public abstract class StorageComparison {
        public abstract int Compare(Container x, Container y, Item item);
    }

    public class StorageCompareType : StorageComparison {
        public override int Compare(Container x, Container y, Item item) {
            bool x_has_same_restriction = false, y_has_same_restriction = false;
            if (x.tagRestriction.Length > 0)
                x_has_same_restriction = x.tagRestriction.Intersect(item.Stats.GetTags()).Any();
            if (y.tagRestriction.Length > 0)
                y_has_same_restriction = y.tagRestriction.Intersect(item.Stats.GetTags()).Any();

            if (x_has_same_restriction == y_has_same_restriction)
                return 0;
            if (x_has_same_restriction)
                return 1;
            return -1;
        }
    }

    public class StorageCompareWeightReduction : StorageComparison {
        public override int Compare(Container x, Container y, Item item) {
            return x.encumberanceMult.CompareTo(y.encumberanceMult) * -1;
        }
    }

    public class StorageCompareMostFull : StorageComparison {
        public override int Compare(Container x, Container y, Item item) {
            float x_space_left = x.maxWeight - x.GetHoldingWeight();
            float y_space_left = y.maxWeight - y.GetHoldingWeight();
            return x_space_left.CompareTo(y_space_left);
        }
    }

    public class StorageCompareLargest : StorageComparison {
        public override int Compare(Container x, Container y, Item item) {
            return x.maxWeight.CompareTo(y.maxWeight);
        }
    }
    
    // TODO: Maybe maybe this a generic item comparison.
    public class StorageCompareBestCondition : StorageComparison {
        public override int Compare(Container x, Container y, Item item) {
            return x.mItem.condition.CompareTo(y.mItem.condition);
        }
    }
    #endregion
}