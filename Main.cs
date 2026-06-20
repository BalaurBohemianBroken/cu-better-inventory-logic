// using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

// TODO: This likely is incompatible with QoL. I need to test and report.
// TODO: Make crafting status update after properly crafting stuff.
namespace BalaurBohemianBroken {
    [BepInPlugin("com.balaur.BetterLogic", "BetterInventoryLogic", "0.1.2")]
    public class BetterInventoryLogic : BaseUnityPlugin {
        public static BetterInventoryLogic instance;

        public static bool compare_container_enabled;
        public static bool compare_wearable_enabled;
        public static bool compare_quality_enabled;
        public static bool compare_value_enabled;
        public static bool compare_condition_enabled;

        public static bool compare_liquid_unmixed_enabled;
        public static bool compare_liquid_stacking_enabled;
        public static bool compare_liquid_weight_enabled;

        public static List<CraftingComparison> crafting_comparison_stack = new List<CraftingComparison>();
        public static List<LiquidStorageComparison> liquid_comparisons_stack = new List<LiquidStorageComparison>();
        
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
            
            target = typeof(RecipeResult).GetMethod("SpawnResult");
            patch = typeof(BetterInventoryLogic).GetMethod("PrefixSpawnResult");
            harmony.Patch(target, prefix: new HarmonyMethod(patch));

            target = typeof(Settings).GetMethod("DefaultSettings");
            patch = typeof(BetterInventoryLogic).GetMethod("PostfixDefaultSettings");
            harmony.Patch(target, postfix: new HarmonyMethod(patch));

            target = typeof(Locale).GetMethod("LoadLanguage");
            patch = typeof(BetterInventoryLogic).GetMethod("PostfixLoadLanguage");
            harmony.Patch(target, postfix: new HarmonyMethod(patch));
        }

        private static void CreateCraftingComparisonStack() {
            // TODO: Allow users to organize the stack themselves.
            crafting_comparison_stack = new List<CraftingComparison>();

            if (compare_container_enabled) {
                crafting_comparison_stack.Add(new CraftingCompareContainer(true));
            }
            
            if (compare_wearable_enabled) {
                crafting_comparison_stack.Add(new CraftingCompareWearable(true));
            }
            
            if (compare_quality_enabled) {
                crafting_comparison_stack.Add(new CraftingCompareQuality(false));
            }
            
            if (compare_value_enabled) {
                crafting_comparison_stack.Add(new CraftingCompareValue(true));
            }
            
            if (compare_condition_enabled) {
                crafting_comparison_stack.Add(new CraftingCompareCondition(true));
            }
        }

        private static void CreateLiquidComparisonsStack() {
            liquid_comparisons_stack = new List<LiquidStorageComparison>();
            if (compare_liquid_unmixed_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidUnmixed());
            if (compare_liquid_stacking_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidStacking());
            if (compare_liquid_weight_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidWeightRatio());
        }
        
        #region Patches
        public static bool PrefixGetItemsForRecipe(Recipe __instance, ref List<Item> __result) {
            __result = CraftingLogic(__instance, true);
            return false;
        }
        
        public static bool PrefixGetItemsForRecipeThorough(Recipe __instance, ref List<Item> __result) {
            __result = CraftingLogic(__instance, false);
            return false;
        }

        public static void PostfixLoadLanguage() {
            Locale.currentLang.other.Add("gamesetcompare_container_enabled", "Compare container");
            Locale.currentLang.other.Add("gamesetcompare_container_enableddsc", "Use containers later.");
            
            Locale.currentLang.other.Add("gamesetcompare_wearable_enabled", "Compare wearable");
            Locale.currentLang.other.Add("gamesetcompare_wearable_enableddsc", "Use wearables later.");
            
            Locale.currentLang.other.Add("gamesetcompare_quality_enabled", "Compare quality");
            Locale.currentLang.other.Add("gamesetcompare_quality_enableddsc", "Use items of lower quality, like lower hammering, later.");
            
            Locale.currentLang.other.Add("gamesetcompare_value_enabled", "Compare value");
            Locale.currentLang.other.Add("gamesetcompare_value_enableddsc", "Use items of higher trading value later.");
            
            Locale.currentLang.other.Add("gamesetcompare_condition_enabled", "Compare condition");
            Locale.currentLang.other.Add("gamesetcompare_condition_enableddsc", "Use items with higher condition later.");
        }

        public static void PostfixDefaultSettings(List<Setting> __result) {
            List<Setting> my_settings = new List<Setting> {
                new SettingBool {
                    name = "compare_container_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_container_enabled = Settings.Get<SettingBool>("compare_container_enabled").value;
                        CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_wearable_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_wearable_enabled = Settings.Get<SettingBool>("compare_wearable_enabled").value;
                        CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_quality_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_quality_enabled = Settings.Get<SettingBool>("compare_quality_enabled").value;
                        CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_value_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_value_enabled = Settings.Get<SettingBool>("compare_value_enabled").value;
                        CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_condition_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_condition_enabled = Settings.Get<SettingBool>("compare_condition_enabled").value;
                        CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                
                new SettingBool {
                    name = "compare_liquid_unmixed_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_liquid_unmixed_enabled = Settings.Get<SettingBool>("compare_liquid_unmixed_enabled").value;
                        CreateLiquidComparisonsStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_liquid_stacking_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_liquid_stacking_enabled = Settings.Get<SettingBool>("compare_liquid_stacking_enabled").value;
                        CreateLiquidComparisonsStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_liquid_weight_enabled",
                    value = true,
                    apply = delegate {
                        BetterInventoryLogic.compare_liquid_weight_enabled = Settings.Get<SettingBool>("compare_liquid_weight_enabled").value;
                        CreateLiquidComparisonsStack();
                    },
                    category = Setting.SettingCategory.Game
                },
            };
            __result.AddRange(my_settings);
        }

        public static bool PrefixSpawnResult(int recipeInt, RecipeResult __instance) {
            // The is largely the same code from decomp, with a different storage logic placed in.
            // I tossed up whether I wanted to do this, or whether I wanted to use a transpiler to patch it.
            // In either case, if the code changes at all, I'd need to rewrite it.
            //
            // A transpiler might be more durable, as I'd just need to change where the hook starts.
            // Plus, it also means I'm not potentially messing with other mods by skipping a function.
            // But this is much faster to develop with.
            // 
            // If mod compatibility becomes an issue, I'll do a transpiler.

            // TODO: Message that states where the crafted item was placed?
            int num1 = PlayerCamera.main.body.skills.INT - recipeInt;
            float crafted_condition = 1f;
            if (num1 < 0 && Random.value < 0.5)
            {
              switch (num1)
              {
                case -3:
                  Body body = PlayerCamera.main.body;
                  body.DoGoreSound();
                  for (int index = 5; index <= 8; index += 3)
                  {
                    body.limbs[index].pain += 40f;
                    body.limbs[index].skinHealth -= 15f;
                    body.limbs[index].bleedAmount += Random.Range(2f, 5f);
                  }
                  return false;
                case -1:
                  crafted_condition = Random.Range(0.2f, 0.9f);
                  break;
                default:
                  return false;
              }
            }
            for (int index = 0; index < __instance.amount; ++index)
            {
              if (__instance.isLiquid) {
                  StoreCraftedLiquid(__instance, crafted_condition);
                  return false;
              }
              else
              {
                Item component3 = Utils.Create(__instance.id, (Vector2) PlayerCamera.main.body.transform.position, 0.0f).GetComponent<Item>();
                component3.condition = __instance.resultCondition * crafted_condition;
                PlayerCamera.main.body.AutoPickUpItem(component3);
                if ((bool) (Object) component3.battery)
                  component3.battery.UnloadBattery(true);
                WaterContainerItem component4;
                if (!__instance.dontDrainResultLiquid && component3.TryGetComponent<WaterContainerItem>(out component4))
                {
                  component4.stack = new List<LiquidStack>();
                  component3.condition = 0.0f;
                }
              }
            }

            return false;
        }
        #endregion
        
        #region Crafting logic
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
            return candidates.Last();
        }

        public static int CompareCraftingDesirability(Item x, Item y, RecipeItem recipe_slot) {
            foreach (CraftingComparison comparison in crafting_comparison_stack) {
                int comp_result = comparison.Compare(x, y, recipe_slot);
                if (comp_result != 0)
                    return comp_result;
            }

            return 0;
        }
        #endregion

        public static void StoreCraftedLiquid(RecipeResult __instance, float condition) {
            // Find suitable liquid containers.
            var best_bottle = FindBestBottle(__instance.id, __instance.resultCondition * condition);
            if (best_bottle != null) {
                best_bottle.AddLiquid(__instance.id, __instance.resultCondition * condition);
                return;
            }
            
            // Create temp bottle.
            GameObject gameObject = Utils.Create("craftingbottle", (Vector2) PlayerCamera.main.body.transform.position, 0.0f);
            Item component = gameObject.GetComponent<Item>();
            component.condition = __instance.resultCondition;
            PlayerCamera.main.body.AutoPickUpItem(component);
            double num4 = (double) component.GetComponent<WaterContainerItem>().AddLiquid(__instance.id, __instance.resultCondition * condition);
            Object.Destroy((Object) gameObject, 300f);
        }

        public static WaterContainerItem FindBestBottle(string liquid_id, float liquid_amount) {
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

            // Liquid logic:
            candidates.Sort((x, y) => CompareLiquidContainerDesirability(x, y, liquid_id, liquid_amount));
            return candidates.Last();
        }
        
        public static int CompareLiquidContainerDesirability(WaterContainerItem x, WaterContainerItem y, string liquid_id, float amount) {
            List<LiquidStorageComparison> comparisons = new List<LiquidStorageComparison>() {
                new CompareLiquidUnmixed(),
                new CompareLiquidStacking(),
                new CompareLiquidWeightRatio(),
            };
            
            foreach (LiquidStorageComparison comparison in comparisons) {
                int comp_result = comparison.Compare(x, y, liquid_id, amount);
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

    #region Crafting comparison classes/methods
    // I use a class for this so that I can easily store and sort the methods in a list.
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
    
    #region Storage comparison classes
    
    // I use a class for this so that I can easily store and sort the methods in a list.
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
            FieldInfo field_info = typeof(WaterContainerItem).GetField("item", BindingFlags.NonPublic | BindingFlags.Instance);
            
            Item x_item = (Item)field_info.GetValue(x);
            float x_max_weight = x_item.Stats.weight;
            float x_weight_per_capacity = x_max_weight / x.Capacity;
            
            Item y_item = (Item)field_info.GetValue(y);
            float y_max_weight = y_item.Stats.weight;
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
}