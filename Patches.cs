﻿using System;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Systems.GameEventSystem;
using HarmonyLib;
using MelonLoader;

namespace BTKUILib
{
    internal class Patches
    {
        private static HarmonyLib.Harmony _modInstance;
        
        internal static void Initialize(HarmonyLib.Harmony modInstance)
        {
            _modInstance = modInstance;
            
            ApplyPatches(typeof(CVRMenuManagerPatch));
            ApplyPatches(typeof(ViewManagerPatches));

            CVRGameEventSystem.Instance.OnConnectionLost.AddListener((message) =>
            {
                try
                {
                    QuickMenuAPI.OnWorldLeave?.Invoke();
                }
                catch (Exception e)
                {
                    BTKUILib.Log.Error(e);
                }
            });

            CVRPlayerManager.Instance.OnPlayerEntityCreated += entity =>
            {
                try
                {
                    QuickMenuAPI.UserJoin?.Invoke(entity);                
                }
                catch (Exception e)
                {
                    BTKUILib.Log.Error(e);
                }
            };
            
            CVRPlayerManager.Instance.OnPlayerEntityRecycled += entity =>
            {
                try
                {
                    QuickMenuAPI.UserLeave?.Invoke(entity);                
                }
                catch (Exception e)
                {
                    BTKUILib.Log.Error(e);
                }
            }; 
            
            BTKUILib.Log.Msg("Applied patches!");
        }
        
        private static void ApplyPatches(Type type)
        {
            MelonDebug.Msg($"Applying {type.Name} patches!");
            try
            {
                _modInstance.PatchAll(type);
            }
            catch (Exception e)
            {
                BTKUILib.Log.Error($"Failed while patching {type.Name}!");
                BTKUILib.Log.Error(e);
            }
        }
    }
    
    [HarmonyPatch(typeof(CVR_MenuManager))]
    class CVRMenuManagerPatch
    {
        [HarmonyPatch("markMenuAsReady")]
        [HarmonyPostfix]
        static void MarkMenuAsReady(CVR_MenuManager __instance)
        {
            try
            {
                QuickMenuAPI.OnMenuRegenerate?.Invoke(__instance);
            }
            catch (Exception e)
            {
                BTKUILib.Log.Error(e);
            }
        }
    }

    [HarmonyPatch(typeof(ViewManager))]
    class ViewManagerPatches
    {
        [HarmonyPatch("SendToWorldUi")]
        [HarmonyPostfix]
        static void SendToWorldUi(string value)
        {
            //Ensure that we check if the keyboard action was used within 3 minutes, this will avoid the next keyboard usage triggering the action
            if (DateTime.Now.Subtract(QuickMenuAPI.TimeSinceKeyboardOpen).TotalMinutes <= 3)
                QuickMenuAPI.OnKeyboardSubmitted?.Invoke(value);

            QuickMenuAPI.OnKeyboardSubmitted = null;
        }
    }
}