using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using Timberborn.SelectionSystem;
using UnityEngine;

namespace CopyBuilding
{
    [BepInDependency("aedenthorn.CopyBuilding", "0.1.0")]
    [BepInPlugin("aedenthorn.CopyBuilding", "Copy Building", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        //public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<string> modkey;

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

            hotkey = Config.Bind<string>("Options", "HotKey", "v", "Key to press to copy currently selected structure.");
            modkey = Config.Bind<string>("Options", "ModKey", "left ctrl", "Key to hold while pressing hotkey (optional).");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(SelectionManager), "Update")]
        static class SelectionManager_Update_Patch
        {
            static void Postfix(SelectionManager __instance, GameObject ____selectedObject)
            {
                if (!modEnabled.Value)
                    return;

            }
        }
    }
}
