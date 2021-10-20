using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Timberborn.BuildingTools;
using Timberborn.Cutting;
using Timberborn.Gathering;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.Goods;
using Timberborn.InventorySystem;
using Timberborn.ScienceSystem;
using Timberborn.TimeSystem;
using Timberborn.Workshops;
using Timberborn.Yielding;
using UnityEngine;

namespace ResourceAmounts
{
    [BepInPlugin("aedenthorn.ResourceAmounts", "Resource Amounts", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        //public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> buildingCostMult;
        public static ConfigEntry<float> buildingConsumeMult;
        public static ConfigEntry<float> harvestYieldMult;
        public static ConfigEntry<float> factoryFuelMult;
        public static ConfigEntry<float> factoryIngredientMult;
        public static ConfigEntry<float> factoryProductMult;
        public static ConfigEntry<float> scienceGainMult;


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            //nexusID = Config.Bind<int>("General", "NexusID", 88, "Nexus mod ID for updates");

            buildingCostMult = Config.Bind<float>("Options", "BuildingCostMult", 1, "Multiply building resource consumption by this amount.");
            buildingConsumeMult = Config.Bind<float>("Options", "BuildingConsumeMult", 1, "Multiply building resource consumption by this amount.");
            harvestYieldMult = Config.Bind<float>("Options", "HarvestYieldMult", 1, "Multiply cuttable yield by this amount.");
            factoryFuelMult = Config.Bind<float>("Options", "FactoryFuelMult", 1, "Multiply manufactory fuel consumption by this amount.");
            factoryIngredientMult = Config.Bind<float>("Options", "FactoryIngredientMult", 1, "Multiply manufactory ingredient consumption by this amount.");
            factoryProductMult = Config.Bind<float>("Options", "FactoryProductMult", 1, "Multiply manufactory product yield by this amount.");
            scienceGainMult = Config.Bind<float>("Options", "ScienceGainMult", 1, "Multiply science points gained by this amount.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(ScienceService), nameof(ScienceService.AddPoints))]
        static class ScienceService_AddPoints_Patch
        {
            static void Prefix(ref int amount)
            {
                if (!modEnabled.Value)
                    return;
                amount = Mathf.RoundToInt(amount * scienceGainMult.Value);
            }
        }
        [HarmonyPatch(typeof(BuildingPlacer), "AddCost")]
        static class BuildingPlacer_AddCost_Patch
        {
            static void Prefix(ref IEnumerable<GoodAmount> cost)
            {
                if (!modEnabled.Value)
                    return;
                List<GoodAmount> newCost = new List<GoodAmount>();
                for(int i = 0; i < cost.Count(); i++)
                {
                    newCost.Add(new GoodAmount(cost.ElementAt(i).GoodSpecification, Mathf.RoundToInt(cost.ElementAt(i).Amount * buildingCostMult.Value)));
                }
                cost = newCost.AsEnumerable();
            }
        }
        [HarmonyPatch(typeof(InventoryInitializer), nameof(InventoryInitializer.AddAllowedGoods))]
        static class InventoryInitializer_Patch
        {
            static void Prefix(ref int ____capacity, string ____componentName, ref IEnumerable<StorableGoodAmount> goods, Inventory ____inventory)
            {
                if (!modEnabled.Value || ____componentName != "ConstructionSite")
                    return;
                List<StorableGoodAmount> newGoods = new List<StorableGoodAmount>();
                for (int i = 0; i < goods.Count(); i++)
                {
                    newGoods.Add(new StorableGoodAmount(goods.ElementAt(i).StorableGood, Mathf.RoundToInt(goods.ElementAt(i).Amount * buildingCostMult.Value)));
                }
                goods = newGoods;
                ____capacity = newGoods.Sum((StorableGoodAmount good) => good.Amount);
                AccessTools.Property(typeof(Inventory), "Capacity").SetValue(____inventory, ____capacity);
            }
        }

        [HarmonyPatch(typeof(GoodConsumingBuilding), "UpdateConsumption")]
        static class GoodConsumingBuilding_UpdateConsumption_Patch
        {
            static void Prefix(GoodConsumingBuilding __instance, ref float ____supplyLeft, IDayNightCycle ____dayNightCycle)
            {
                if (!modEnabled.Value || ____supplyLeft <= 0)
                    return;
                ____supplyLeft += ____dayNightCycle.FixedDeltaTimeInHours * __instance.GoodPerHour * (1 - buildingConsumeMult.Value);
            }
        }
        [HarmonyPatch(typeof(Yielder), nameof(Yielder.Initialize))]
        static class Yielder_Initialize_Patch
        {
            static void Postfix(Yielder __instance, ref GoodAmount ____yield, ref GoodAmount ____initialYield)
            {
                if (!modEnabled.Value || (!__instance.GetComponent<Cuttable>() && !__instance.GetComponent<Gatherable>()))
                    return;
                ____yield.Amount = Mathf.CeilToInt(____yield.Amount * harvestYieldMult.Value);
                ____initialYield.Amount = Mathf.CeilToInt(____initialYield.Amount * harvestYieldMult.Value);
            }
        }
        [HarmonyPatch(typeof(Yielder), "Awake")]
        static class Yielder_Awake_Patch
        {
            static void Postfix(Yielder __instance, ref GoodAmount ____yield, ref GoodAmount ____initialYield)
            {
                if (!modEnabled.Value || (!__instance.GetComponent<Cuttable>() && !__instance.GetComponent<Gatherable>()))
                    return;
            }
        }
        [HarmonyPatch(typeof(Cuttable), "Awake")]
        static class Cuttable_Awake_Patch
        {
            static void Postfix(Cuttable __instance)
            {
                if (!modEnabled.Value)
                    return;
                __instance.Yield.Amount = Mathf.CeilToInt(__instance.Yield.Amount * harvestYieldMult.Value);
            }
        }
        [HarmonyPatch(typeof(Gatherable), "Awake")]
        static class Gatherable_Awake_Patch
        {
            static void Postfix(Gatherable __instance)
            {
                if (!modEnabled.Value)
                    return;
                __instance.Yield.Amount = Mathf.CeilToInt(__instance.Yield.Amount * harvestYieldMult.Value);
            }
        }
        [HarmonyPatch(typeof(Manufactory), "ConsumeFuel")]
        static class Manufactory_ConsumeFuel_Patch
        {
            static void Prefix(Manufactory __instance)
            {
                if (!modEnabled.Value || !__instance.ProductionRecipe.ConsumesFuel)
                    return;
                AccessTools.Property(typeof(Manufactory), "FuelRemaining").SetValue(__instance, __instance.FuelRemaining + (1f / __instance.ProductionRecipe.CyclesFuelLasts) * (1 - factoryFuelMult.Value));
            }
        }
        [HarmonyPatch(typeof(Inventory), "CheckUnreservedCapacity")]
        static class Inventory_CheckUnreservedCapacity_Patch
        {
            static bool Prefix(Inventory __instance, GoodAmount good, GoodRegistry ____storage, GoodRegistry ____reservedCapacity)
            {
                if (!modEnabled.Value || (bool)AccessTools.Method(typeof(Inventory), "HasCapacity").Invoke(__instance, new object[] { good }))
                    return true;
                int excess = ____storage.TotalAmount + good.Amount - __instance.Capacity;
                if(excess > 0)
                    AccessTools.Property(typeof(Inventory), "Capacity").SetValue(__instance, __instance.Capacity + excess);
                return false;
            }
        }
        [HarmonyPatch(typeof(Inventory), "CheckCapacity")]
        static class Inventory_CheckCapacity_Patch
        {
            static bool Prefix(Inventory __instance, GoodAmount good, GoodRegistry ____storage, GoodRegistry ____reservedCapacity)
            {
                if (!modEnabled.Value || __instance.HasUnreservedCapacity(good))
                    return true;
                int excess = ____storage.TotalAmount + good.Amount + ____reservedCapacity.TotalAmount - __instance.Capacity;
                if(excess > 0)
                    AccessTools.Property(typeof(Inventory), "Capacity").SetValue(__instance, __instance.Capacity + excess);
                return false;
            }
        }
        [HarmonyPatch(typeof(Manufactory), "FinishProduction")]
        static class Manufactory_FinishProduction_Patch
        {
            static void Prefix(Manufactory __instance)
            {
                if (!modEnabled.Value)
                    return;
                foreach (GoodAmount good in __instance.ProductionRecipe.Ingredients)
                {
                    GoodAmount ga = good;
                    float amount = ga.Amount * (1 - factoryIngredientMult.Value);
                    ga.Amount = Mathf.RoundToInt(Math.Abs(amount));
                    if(amount > 0)
                        __instance.Inventory.Give(ga);
                    else if(amount < 0)
                        __instance.Inventory.Take(ga);
                }
                foreach (GoodAmount good in __instance.ProductionRecipe.Products)
                {
                    GoodAmount ga = good;
                    float amount = ga.Amount * (1 - factoryProductMult.Value);
                    ga.Amount = Mathf.RoundToInt(Math.Abs(amount));
                    if (amount < 0)
                        __instance.Inventory.Give(ga);
                    else if (amount > 0)
                        __instance.Inventory.Take(ga);
                }
            }
        }
    }
}
