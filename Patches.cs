using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BalaurBohemianBroken {
    [HarmonyPatch(typeof(Recipe))]
    [HarmonyPatch(nameof(Recipe.GetItemsForRecipe))]
    public class Patch_GetItemsForRecipe {
        public static bool Prefix(Recipe __instance, ref List<Item> __result) {
            __result = CraftingLogic.GetItemsForRecipe(__instance, true);
            return false;
        }
    }

    [HarmonyPatch(typeof(Recipe))]
    [HarmonyPatch(nameof(Recipe.GetItemsForRecipeThorough))]
    public class Patch_GetItemsForRecipeThorough {
        public static bool Prefix(Recipe __instance, ref List<Item> __result) {
            __result = CraftingLogic.GetItemsForRecipe(__instance, false);
            return false;
        }
    }

    [HarmonyPatch(typeof(Locale))]
    [HarmonyPatch(nameof(Locale.LoadLanguage))]
    public class Patch_LoadLanguage {
        public static void Postfix() {
            Dictionary<string, string> lines = new Dictionary<string, string> {
                #region Crafting settings
                {"gamesetprefer_on_floor", "<color=purple>Crafting</color>: Prefer on floor"},
                {"gamesetprefer_on_floordsc", "Use materials on the floor before things in your inventory."},
                {"gamesetprefer_not_container", "<color=purple>Crafting</color>: Prefer not containers"},
                {"gamesetprefer_not_containerdsc", "Use containers, like bags, later."},
                {"gamesetprefer_not_wearable", "<color=purple>Crafting</color>: Prefer not wearables"},
                {"gamesetprefer_not_wearabledsc", "Use wearables, like clothing, later."},
                {"gamesetprefer_on_body", "<color=purple>Crafting</color>: Prefer held"},
                {"gamesetprefer_on_bodydsc", "Use materials in your hands/mouth/back before things in your inventory."},
                {"gamesetprefer_high_quality", "<color=purple>Crafting</color>: Prefer higher quality"},
                {"gamesetprefer_high_qualitydsc", "Use items of lower quality, like lower hammering, later."},
                {"gamesetprefer_low_value", "<color=purple>Crafting</color>: Prefer lower value"},
                {"gamesetprefer_low_valuedsc", "Use items of higher trading value later."},
                {"gamesetprefer_low_condition", "<color=purple>Crafting</color>: Prefer lower condition"},
                {"gamesetprefer_low_conditiondsc", "Use items with higher condition later."},
                #endregion
                #region Autopickup rules
                {"gamesetliquids_can_mix", "<color=purple>Storage (Liquid)</color>: Allow stacking into mixed"},
                {"gamesetliquids_can_mixdsc", "Allow a pouring this liquid into a bottle that has other liquids, only if it already has some of this liquid."},
                {"gamesetliquids_can_fill_new_bottles", "<color=purple>Storage (Liquid)</color>: Allow filling new bottles"},
                {"gamesetliquids_can_fill_new_bottlesdsc", "Allow liquids to fill bottles that are empty."},

                {"gamesetcompare_liquid_unmixed_enabled", "<color=purple>Storage (Liquid)</color>: Prefer liquids unmixed"},
                {"gamesetcompare_liquid_unmixed_enableddsc", "If 'Allow liquids to mix' is enabled, fill containers with the fewest different liquids."},
                {"gamesetcompare_liquid_stacking_enabled", "<color=purple>Storage (Liquid)</color>: Prefer liquid stack"},
                {"gamesetcompare_liquid_stacking_enableddsc", "Fill containers that have the highest amount of this liquid first."},
                {"gamesetcompare_liquid_weight_enabled", "<color=purple>Storage (Liquid)</color>: Prefer low weight liquid containers"},
                {"gamesetcompare_liquid_weight_enableddsc", "Prefer containers that have the lowest weight to highest liquid ratio when full."},
                
                {"gamesetstore_in_containers_first", "<color=purple>Storage</color>: Store in containers first"},
                {"gamesetstore_in_containers_firstdsc", "Put crafted items into containers first, rather than in hands/mouth/back"},

                {"gamesetcompare_storage_type", "<color=purple>Storage</color>: Prefer matched container type"},
                {"gamesetcompare_storage_typedsc", "When storing crafted items, choose containers specific to this item type, such as the material pouch for materials."},
                {"gamesetcompare_storage_reduction", "<color=purple>Storage</color>: Prefer higher container reduction"},
                {"gamesetcompare_storage_reductiondsc", "When storing crafted items, prefer containers that have a better encumbrance reduction."},
                {"gamesetcompare_storage_full", "<color=purple>Storage</color>: Prefer fuller container"},
                {"gamesetcompare_storage_fulldsc", "When storing crafted items, prefer containers that are more full."},
                {"gamesetcompare_storage_capacity", "<color=purple>Storage</color>: Prefer larger containers"},
                {"gamesetcompare_storage_capacitydsc", "When storing crafted items, prefer containers that are larger."},
                {"gamesetcompare_storage_best_condition", "<color=purple>Storage</color>: Prefer best condition container"},
                {"gamesetcompare_storage_best_conditiondsc", "When storing crafted items, prefer containers that have higher condition."},
                #endregion
            };

            foreach (KeyValuePair<string, string> line in lines) {
                Locale.currentLang.other.Add(line.Key, line.Value);
            } 
        }
    }

    [HarmonyPatch(typeof(Settings))]
    [HarmonyPatch(nameof(Settings.DefaultSettings))]
    public class Patch_DefaultSettings {
        public static void Postfix(List<Setting> __result) {
            __result.AddRange(CraftingLogic.settings);
            __result.AddRange(StorageLogic.settings);
            __result.AddRange(LiquidStorageLogic.settings);
        }
    }

    [HarmonyPatch(typeof(RecipeResult))]
    [HarmonyPatch(nameof(RecipeResult.SpawnResult))]
    public class Patch_SpawnResult {
        public static bool Prefix(int recipeInt, RecipeResult __instance) {
            // The is largely the same code from decomp, with a different storage logic placed in.
            // I tossed up whether I wanted to do this, or whether I wanted to use a transpiler to patch it.
            // In either case, if the code changes at all, I'd need to rewrite it.
            //
            // A transpiler might be more durable, as I'd just need to change where the hook starts.
            // Plus, it also means I'm not potentially messing with other mods by skipping a function.
            // But this is much faster to develop with.
            // 
            // If mod compatibility becomes an issue, I'll do a transpiler.

            int num1 = PlayerCamera.main.body.skills.INT - recipeInt;
            float crafted_condition = 1f;
            if (num1 < 0 && Random.value < 0.5) {
                switch (num1) {
                    case -3:
                        Body body = PlayerCamera.main.body;
                        body.DoGoreSound();
                        for (int index = 5; index <= 8; index += 3) {
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

            for (int index = 0; index < __instance.amount; ++index) {
                if (__instance.isLiquid) {
                    // TODO: This is the only part of code that is changed! I can do this easily with a transpiler.
                    string item_id = __instance.id;
                    float amount = __instance.resultCondition * crafted_condition;
                    bool try_store = LiquidStorageLogic.StoreLiquid(__instance.id, __instance.resultCondition * crafted_condition);
            
                    // Create temp bottle.
                    if (!try_store) {
                        GameObject gameObject = Utils.Create("craftingbottle",
                            (Vector2)PlayerCamera.main.body.transform.position, 0.0f);
                        Item component = gameObject.GetComponent<Item>();
                        component.condition = __instance.resultCondition;
                        StorageLogic.AutoPickup(component, PlayerCamera.main.body);  // This code is changed.
                        double num4 = (double)component.GetComponent<WaterContainerItem>().AddLiquid(item_id, amount);
                        Object.Destroy((Object)gameObject, 300f);
                    }
                }
                else {
                    Item component3 = Utils
                        .Create(__instance.id, (Vector2)PlayerCamera.main.body.transform.position, 0.0f)
                        .GetComponent<Item>();
                    component3.condition = __instance.resultCondition * crafted_condition;
                    StorageLogic.AutoPickup(component3, PlayerCamera.main.body);  // This code is changed.
                    if ((bool)(Object)component3.battery)
                        component3.battery.UnloadBattery(true);
                    WaterContainerItem component4;
                    if (!__instance.dontDrainResultLiquid &&
                        component3.TryGetComponent<WaterContainerItem>(out component4)) {
                        component4.stack = new List<LiquidStack>();
                        component3.condition = 0.0f;
                    }
                }
            }

            return false;
        }
    }
}