using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BalaurBohemianBroken {
    [HarmonyPatch(typeof(Recipe))]
    [HarmonyPatch(nameof(Recipe.GetItemsForRecipe))]
    public class Patch_GetItemsForRecipe {
        public static bool Prefix(Recipe __instance, ref List<Item> __result) {
            __result = BetterInventoryLogic.CraftingLogic(__instance, true);
            return false;
        }
    }

    [HarmonyPatch(typeof(Recipe))]
    [HarmonyPatch(nameof(Recipe.GetItemsForRecipeThorough))]
    public class Patch_GetItemsForRecipeThorough {
        public static bool Prefix(Recipe __instance, ref List<Item> __result) {
            __result = BetterInventoryLogic.CraftingLogic(__instance, false);
            return false;
        }
    }

    [HarmonyPatch(typeof(Locale))]
    [HarmonyPatch(nameof(Locale.LoadLanguage))]
    public class Patch_LoadLanguage {
        public static void Postfix() {
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
    }

    [HarmonyPatch(typeof(Settings))]
    [HarmonyPatch(nameof(Settings.DefaultSettings))]
    public class Patch_DefaultSettings {
        public static void Postfix(List<Setting> __result) {
            List<Setting> my_settings = new List<Setting> {
                #region Crafting comparison
                new SettingBool {
                    name = "compare_container_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_container_enabled =
                            Settings.Get<SettingBool>("compare_container_enabled").value;
                        InventoryLogic.CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_wearable_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_wearable_enabled =
                            Settings.Get<SettingBool>("compare_wearable_enabled").value;
                        InventoryLogic.CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_quality_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_quality_enabled =
                            Settings.Get<SettingBool>("compare_quality_enabled").value;
                        InventoryLogic.CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_value_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_value_enabled =
                            Settings.Get<SettingBool>("compare_value_enabled").value;
                        InventoryLogic.CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_condition_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_condition_enabled =
                            Settings.Get<SettingBool>("compare_condition_enabled").value;
                        InventoryLogic.CreateCraftingComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                #endregion
                #region Liquid storage comparison
                new SettingBool {
                    name = "compare_liquid_unmixed_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_liquid_unmixed_enabled =
                            Settings.Get<SettingBool>("compare_liquid_unmixed_enabled").value;
                        InventoryLogic.CreateLiquidComparisonsStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_liquid_stacking_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_liquid_stacking_enabled =
                            Settings.Get<SettingBool>("compare_liquid_stacking_enabled").value;
                        InventoryLogic.CreateLiquidComparisonsStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_liquid_weight_enabled",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_liquid_weight_enabled =
                            Settings.Get<SettingBool>("compare_liquid_weight_enabled").value;
                        InventoryLogic.CreateLiquidComparisonsStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                #endregion
                #region Storage comparison
                new SettingBool {
                    name = "store_in_containers_first",
                    value = true,
                    apply = delegate {
                        InventoryLogic.store_in_containers_first =
                            Settings.Get<SettingBool>("store_in_containers_first").value;
                        // InventoryLogic.CreateStorageComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_storage_type",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_storage_type =
                            Settings.Get<SettingBool>("compare_storage_type").value;
                        InventoryLogic.CreateStorageComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_storage_reduction",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_storage_reduction =
                            Settings.Get<SettingBool>("compare_storage_reduction").value;
                        InventoryLogic.CreateStorageComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_storage_full",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_storage_full =
                            Settings.Get<SettingBool>("compare_storage_full").value;
                        InventoryLogic.CreateStorageComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_storage_capacity",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_storage_capacity =
                            Settings.Get<SettingBool>("compare_storage_capacity").value;
                        InventoryLogic.CreateStorageComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                new SettingBool {
                    name = "compare_storage_best_condition",
                    value = true,
                    apply = delegate {
                        InventoryLogic.compare_storage_best_condition =
                            Settings.Get<SettingBool>("compare_storage_best_condition").value;
                        InventoryLogic.CreateStorageComparisonStack();
                    },
                    category = Setting.SettingCategory.Game
                },
                #endregion
            };
            __result.AddRange(my_settings);
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

            // TODO: Message that states where the crafted item was placed?
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
                    bool try_store = InventoryLogic.StoreLiquid(__instance.id, __instance.resultCondition * crafted_condition);
            
                    // Create temp bottle.
                    if (!try_store) {
                        GameObject gameObject = Utils.Create("craftingbottle",
                            (Vector2)PlayerCamera.main.body.transform.position, 0.0f);
                        Item component = gameObject.GetComponent<Item>();
                        component.condition = __instance.resultCondition;
                        PlayerCamera.main.body.AutoPickUpItem(component);
                        double num4 = (double)component.GetComponent<WaterContainerItem>().AddLiquid(item_id, amount);
                        Object.Destroy((Object)gameObject, 300f);
                    }
                }
                else {
                    Item component3 = Utils
                        .Create(__instance.id, (Vector2)PlayerCamera.main.body.transform.position, 0.0f)
                        .GetComponent<Item>();
                    component3.condition = __instance.resultCondition * crafted_condition;
                    PlayerCamera.main.body.AutoPickUpItem(component3);
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

    [HarmonyPatch(typeof(Body))]
    [HarmonyPatch(nameof(Body.AutoPickUpItem))]
    public class Patch_AutoPickUpItem {
        public static bool Prefix(Item item, Body __instance) {
            if (item.Stats.HasTag("noautopickup"))
                return false;
            if (!item.Stats.wearable)
            {
                // TODO: This is the only part of this code I change. I could transpile this.
                InventoryLogic.PickUpItem(item);
            }
            else
            {
                Item wearableBySlotId = __instance.GetWearableBySlotID(item.Stats.wearSlotId);
                if ((bool) (UnityEngine.Object) wearableBySlotId)
                    __instance.DropItem(wearableBySlotId);
                __instance.WearWearable(item);
                PlayerCamera.main.UpdateWearables();
            }
            return false;
        }
    }
}