using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Timberborn.AreaSelectionSystem;
using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.InputSystem;
using Timberborn.PreviewSystem;
using Timberborn.SelectionSystem;
using Timberborn.UISound;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CopyBuilding
{
    [BepInPlugin("aedenthorn.CopyBuilding", "Copy Building", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        //public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<string> modkey;
        private static KeyboardController keyboardController;
        private static SelectionManager selectionManager;
        private static List<BlockObjectTool> tools = new List<BlockObjectTool>();

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

            InputSystem.onEvent += new Action<InputEventPtr, InputDevice>(OnInputSystemEvent);
        }

        private void OnInputSystemEvent(InputEventPtr inputEventPtr, InputDevice inputDevice)
        {
            if (inputDevice != Keyboard.current || (!inputEventPtr.IsA<StateEvent>() && !inputEventPtr.IsA<DeltaStateEvent>()))
                return;
            if (!keyboardController)
                keyboardController = FindObjectOfType<KeyboardController>();
            if(!selectionManager)
                selectionManager = FindObjectOfType<SelectionManager>();

            if (!keyboardController || !selectionManager)
            {
                Dbgl($"{keyboardController == null} {selectionManager == null}");
                return;
            }

            if (((Keyboard)inputDevice).f11Key.isPressed)
            {
                GameObject selected = (GameObject)AccessTools.Field(typeof(SelectionManager), "_selectedObject").GetValue(selectionManager);
                if (!selected)
                {
                    Dbgl($"null selected");
                    return;
                }
                Dbgl($"clicked with object {selected?.name} selected {selected?.GetComponent<BlockObject>() != null}");

                EntityService es = FindObjectOfType<EntityService>();
                BuildingService bs = new BuildingService();
                es.Instantiate(bs.GetBuildingPrefab(bs.GetPrefabName(selected.GetComponent<Building>())));
            }
        }

        [HarmonyPatch(typeof(BlockObjectTool), new Type[] { typeof(BlockObjectToolDescriber), typeof(InputService), typeof(AreaPickerFactory), typeof(PreviewPlacerFactory), typeof(UISoundController), typeof(BlockObjectPlacerService) })]
        [HarmonyPatch(MethodType.Constructor)]
        static class BlockObjectTool_Patch
        {
            static void Postfix(BlockObjectTool __instance)
            {
                if (!modEnabled.Value)
                    return;
                tools.Add(__instance);
            }
        }
    }
}
