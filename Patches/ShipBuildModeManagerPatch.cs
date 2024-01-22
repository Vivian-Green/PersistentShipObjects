using HarmonyLib;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace PersistentShipObjects.Patches {
    internal class ShipBuildModeManagerPatch
    {
        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpc")]
        [HarmonyPrefix]
        public static void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef)
        {
            // get placed PSO and its dad (who could beat up your dad) from objectRef
            objectRef.TryGet(out NetworkObject netObj);
            PlaceableShipObject placeableObject = netObj.GetComponentInChildren<PlaceableShipObject>();
            GameObject PSOsDad = placeableObject?.transform.parent?.gameObject;
            
            // ensure it's non-null, err
            if (placeableObject == null) { Debug.LogError("ShipBuildModeManagerPatch: placeableObject is null"); goto EarlyReturn; }
            if (PSOsDad == null) { Debug.LogError("ShipBuildModeManagerPatch: PSOsDad is null"); goto EarlyReturn;}

            // save its transform
            String thisUnlockableName = StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].unlockableName;
            TransformObject transObj = new TransformObject(newPosition, newRotation, placeableObject.unlockableID, thisUnlockableName);

            PersistentShipObjects.UpdateObjectManager(transObj);

            EarlyReturn:;

            //Debug.Log("ShipBuildModeManagerPatch: Saving trans of " + placeableObject.GetType() + " named " + PSOsDad.name + " at pos " + newPosition);
            //Debug.Log("old parent position: " + placeableObject.transform.parent.position);

            // todo: if this works I'm eating my own ass

            // todo: it mostly works in place of [HarmonyPatch(typeof(StartOfRound), "OnEnable")] but my save corrupted right after sooooo no
            /*StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedPosition = newPosition;
            StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedRotation = newRotation;*/

            //Debug.Log(transObj.unlockableName + " ID " + transObj.unlockableID);
        }


        [HarmonyPatch(typeof(StartOfRound), "OnEnable")] // some code nabbed from ResetShipObjectToDefaultPosition
        [HarmonyPostfix]
        public static void OnEnable() {
            //PersistentShipObjects.DebugLog("MOVING SHIT AROUND!!!");
            foreach (PlaceableShipObject obj in UnityEngine.Object.FindObjectsOfType(typeof(PlaceableShipObject)).Cast<PlaceableShipObject>()) {
                //PersistentShipObjects.DebugPrintDescendantsWrapper(obj?.transform);

                TransformObject foundObj = PersistentShipObjects.FindObjectIfExists(obj.unlockableID);
                if (foundObj == null) {
                    continue;
                }

                obj.parentObject.transform.position = foundObj.pos.GetVector3();
                obj.parentObject.transform.rotation = Quaternion.Euler(foundObj.rot.GetVector3());

                Debug.Log("    Item Found: " + foundObj.unlockableName + " at pos " + obj.transform.parent.transform.position);
                Debug.Log("        new pos: " + obj.transform.parent.position);

                /*
                PersistentShipObjects.DebugLog("        type: " + obj.GetType());
                PersistentShipObjects.DebugLog("        name: " + obj.name);
                PersistentShipObjects.DebugLog("        parent type: " + obj.transform.parent?.gameObject.GetType());
                PersistentShipObjects.DebugLog("        parent name: " + obj.transform.parent?.name);
                //*/
            }
        }

        /*public static void makeAllPSOsFuckOff(string strxjksdf) {
            foreach (PlaceableShipObject obj in GameObject.FindObjectsOfType(typeof(PlaceableShipObject))) {
                makePSOFuckOff(obj, strxjksdf);
            }
        }//*/

        /*public static void makePSOFuckOff(PlaceableShipObject obj, String caller) {
            Debug.Log("sending PSO " + obj.name + " to fuck off land from " + caller + "&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&");

            Vector3 fuckOffLand = new Vector3(1000, 10000, 1000);
            if (obj.parentObjectSecondary != null) {
                obj.parentObjectSecondary.position = fuckOffLand;
            } else if (obj.parentObject != null) {
                //obj.parentObject.positionOffset = fuckOffLand;
                obj.transform.parent.position = fuckOffLand;
            }
        }//*/
    }
}
