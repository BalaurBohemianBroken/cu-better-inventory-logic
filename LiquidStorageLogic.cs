using System.Collections.Generic;
using System.Linq;

namespace BalaurBohemianBroken {
    public class LiquidStorageLogic {
        public static bool liquids_can_fill_new_bottles = true;
        public static bool liquids_can_mix = false;
        public static List<LiquidStorageComparison> liquid_comparisons_stack = new List<LiquidStorageComparison>();
        public static bool compare_liquid_unmixed_enabled;
        public static bool compare_liquid_stacking_enabled;
        public static bool compare_liquid_weight_enabled;

        public static List<Setting> settings = new List<Setting>() {
            new SettingBool {
                name = "liquids_can_fill_new_bottles",
                value = true,
                apply = delegate {
                    liquids_can_fill_new_bottles =
                        Settings.Get<SettingBool>("liquids_can_fill_new_bottles").value;
                    CreateLiquidComparisonsStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "liquids_can_mix",
                value = false,
                apply = delegate {
                    liquids_can_mix =
                        Settings.Get<SettingBool>("liquids_can_mix").value;
                    CreateLiquidComparisonsStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "compare_liquid_unmixed_enabled",
                value = true,
                apply = delegate {
                    compare_liquid_unmixed_enabled =
                        Settings.Get<SettingBool>("compare_liquid_unmixed_enabled").value;
                    CreateLiquidComparisonsStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "compare_liquid_stacking_enabled",
                value = true,
                apply = delegate {
                    compare_liquid_stacking_enabled =
                        Settings.Get<SettingBool>("compare_liquid_stacking_enabled").value;
                    CreateLiquidComparisonsStack();
                },
                category = Setting.SettingCategory.Game
            },
            new SettingBool {
                name = "compare_liquid_weight_enabled",
                value = true,
                apply = delegate {
                    compare_liquid_weight_enabled =
                        Settings.Get<SettingBool>("compare_liquid_weight_enabled").value;
                    CreateLiquidComparisonsStack();
                },
                category = Setting.SettingCategory.Game
            },
        };
        
        public static void CreateLiquidComparisonsStack() {
            liquid_comparisons_stack = new List<LiquidStorageComparison>();
            if (compare_liquid_unmixed_enabled && liquids_can_mix)
                liquid_comparisons_stack.Add(new CompareLiquidUnmixed());
            if (compare_liquid_stacking_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidStacking());
            if (compare_liquid_weight_enabled)
                liquid_comparisons_stack.Add(new CompareLiquidWeightRatio());
        }

        public static int CompareLiquidContainerDesirability(WaterContainerItem x, WaterContainerItem y, string liquid_id, float amount) {
            foreach (LiquidStorageComparison comparison in liquid_comparisons_stack) {
                int comp_result = comparison.Compare(x, y, liquid_id, amount);
                if (comp_result != 0)
                    return comp_result;
            }

            return 0;
        }
        
        public static WaterContainerItem FindBestBottle(string liquid_id, float liquid_amount) {
            List<WaterContainerItem> candidates = new List<WaterContainerItem>();
            foreach (Item player_item in PlayerCamera.main.body.GetAllItemsThorough()) {
                WaterContainerItem liquid_container;
                if (!player_item.TryGetComponent<WaterContainerItem>(out liquid_container))
                    continue;
                if (liquid_container.SpaceLeft < liquid_amount)
                    continue;
                if (!liquids_can_fill_new_bottles && liquid_container.CurrentTotal == 0)
                    continue;

                if (!liquids_can_mix) {
                    int matched_liquid = liquid_container.HasLiquid(liquid_id) ? 1 : 0;
                    int other_liquids = liquid_container.stack.Count() - matched_liquid;
                    if (other_liquids > 0)
                        continue;
                }
                candidates.Add(liquid_container);
            }

            if (candidates.Count == 0)
                return null;

            candidates.Sort((x, y) => CompareLiquidContainerDesirability(x, y, liquid_id, liquid_amount));
            return candidates.Last();
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
    }
    
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
}