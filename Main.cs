using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// TODO: Make crafting status update after properly crafting stuff.
// TODO: Click on an item in the crafting window to tell recipe to not use it.
// TODO: For liquids, I should choose to craft with mixed liquids first.
// TODO: Option to use things not in inventory first.
// TODO: Allow users to organize the stack themselves.
// TODO: Message that states where the crafted item was placed?
// TODO: Fill bottle, and move on to next bottle i  f there is any left over.
// TODO: Allow recipes to use filled containers.
namespace BalaurBohemianBroken {
    [BepInPlugin("com.balaur.BetterLogic", "BetterInventoryLogic", "1.0.0")]
    public class BetterInventoryLogic : BaseUnityPlugin {
        public static BetterInventoryLogic instance;
        
        public void Awake() {
            instance = this;
            Harmony harmony = new Harmony("com.balaur.BetterLogic");
            harmony.PatchAll();
        }

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
    }
}