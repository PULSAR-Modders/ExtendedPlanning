using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using static PulsarModLoader.Patches.HarmonyHelpers;

namespace Extended_Planning
{
    [HarmonyPatch(typeof(PLStarmap), "Update")]
    internal class Starmap
    {
        //Skip the starmap interaction and add "UpdateCourseGoals()" instead
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //if (PLNetworkManager.Instance.MainMenu.GetActiveMenuCount() == 0 && !PLTabMenu.Instance.IsDisplayingOrderMenu())
            List<CodeInstruction> targetSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldsfld), //PLNetworkManager.Instance
                new CodeInstruction(OpCodes.Ldfld), //MainMenu
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(PLMainMenu), nameof(PLMainMenu.GetActiveMenuCount))), //GetActiveMenuCount()
                new CodeInstruction(OpCodes.Brtrue), //0

                new CodeInstruction(OpCodes.Ldsfld), //PLTabMenu.Instance
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(PLTabMenu), nameof(PLTabMenu.IsDisplayingOrderMenu))) //IsDisplayingOrderMenu()
                //before new CodeInstruction(OpCodes.Brtrue) //!true
            };

            //SkipCourseSet(!PLTabMenu.Instance.IsDisplayingOrderMenu())
            List<CodeInstruction> patchSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Starmap), nameof(Starmap.SkipCourseSetSection)))
            };

            //if (PLNetworkManager.Instance.MainMenu.GetActiveMenuCount() == 0 && SkipCourseSet(!PLTabMenu.Instance.IsDisplayingOrderMenu()))
            instructions = PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.AFTER, CheckMode.NONNULL);



            //PLStarmap line 2056 - if (this.StarmapRoot.gameObject.activeSelf != this.IsActive)
            targetSequence = new List<CodeInstruction>()
            {
                //after "this."
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PLStarmap), nameof(PLStarmap.StarmapRoot))), //StarmapRoot
                new CodeInstruction(OpCodes.Callvirt), //get_gameObject
                new CodeInstruction(OpCodes.Callvirt), //get_activeSelf
                new CodeInstruction(OpCodes.Ldarg_0), //this
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PLStarmap), nameof(PLStarmap.IsActive))), //IsActive
                new CodeInstruction(OpCodes.Beq_S) //!=
            };

            //Starmap.UpdateCourseGoals(plsectorInfo4, this.currentSectorInfo)
            patchSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_2), //plsectorInfo4
                new CodeInstruction(OpCodes.Ldarg_0), //this
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PLStarmap), "currentSectorInfo")), //currentSectorInfo
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Starmap), nameof(Starmap.UpdateCourseGoals))) //Starmap.UpdateCourseGoals()
            };

            //Starmap.UpdateCourseGoals(plsectorInfo4, this.currentSectorInfo)
            //if (this.StarmapRoot.gameObject.activeSelf != this.IsActive)
            return PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.BEFORE, CheckMode.NONNULL);
        }

        public static bool SkipCourseSetSection(bool skip)
        {
            return true;
        }

        public static void UpdateCourseGoals(PLSectorInfo targetSector, PLSectorInfo currentSector)
        {
            if (PLNetworkManager.Instance.LocalPlayer != null && (PLNetworkManager.Instance.LocalPlayer.GetClassID() == 0 || PLAcademy.Instance != null) &&
                PLServer.Instance != null && targetSector != null)
            {
                //if (PLInput.Instance.GetButtonUp("middle_click"))
                if ((bool) typeof(PLInput).GetMethod("GetButtonUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(PLInput.Instance, new object[] { "middle_click" }))
                {
                    PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, Array.Empty<object>());
                    return;
                }

                //if targetSector is not a current waypoint
                if (!PLServer.Instance.m_ShipCourseGoals.Contains(targetSector.ID))
                {
                    if (targetSector != currentSector)
                    {
                        if (PLInput.Instance.GetButtonUp(PLInputBase.EInputActionName.click))
                        {
                            //remove all waypoints and re-add them with the target waypoint as the first
                            List<int> courseGoals = new List<int>(PLServer.Instance.m_ShipCourseGoals);
                            PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, Array.Empty<object>());
                            PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[] { targetSector.ID });
                            foreach (int i in courseGoals)
                            {
                                PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[] { i });
                            }
                        }

                        if (PLInput.Instance.GetButtonUp(PLInputBase.EInputActionName.right_click))
                        {
                            PLServer.Instance.photonView.RPC("AddCourseGoal", PhotonTargets.All, new object[] { targetSector.ID });
                        }
                    }
                    else //if targetSector is currentSector
                    {
                        if (PLInput.Instance.GetButtonUp(PLInputBase.EInputActionName.click) || PLInput.Instance.GetButtonUp(PLInputBase.EInputActionName.right_click))
                        {
                            PLServer.Instance.photonView.RPC("ClearCourseGoals", PhotonTargets.All, Array.Empty<object>());
                        }
                    }
                }
                else //if targetSector is a current waypoint
                {
                    if (PLInput.Instance.GetButtonUp(PLInputBase.EInputActionName.click) || PLInput.Instance.GetButtonUp(PLInputBase.EInputActionName.right_click))
                    {
                        PLServer.Instance.photonView.RPC("RemoveCourseGoal", PhotonTargets.All, new object[] { targetSector.ID });
                    }
                }
            }
        }
    }
}
