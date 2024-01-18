using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace PersistentShipObjects.Patches {
    internal class ShipBuildModeManagerPatch
    {
        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpc")]
        [HarmonyPostfix]
        public static void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef, int playerWhoMoved)
        {
            objectRef.TryGet(out NetworkObject netObj);
            PlaceableShipObject placeableObject = netObj.GetComponentInChildren<PlaceableShipObject>();

            if (placeableObject != null)
            {
                GameObject actualObject = placeableObject.transform.parent?.gameObject;

                if (actualObject != null)
                {
                    String actualItemName = actualObject.name;

                    if (placeableObject.transform != null)
                    {

                        Debug.Log("ShipBuildModeManagerPatch: Saving trans of " + placeableObject.GetType() + " named " + actualItemName + " at pos " + newPosition);

                        Transform parentTrans = placeableObject.transform.parent.transform;
                        String thisUnlockableName = StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].unlockableName;

                        //TransformObject objnew = new TransformObject(parentTrans.position, parentTrans.rotation.eulerAngles, placeableObject.unlockableID, thisUnlockableName);
                        TransformObject objnew = new TransformObject(newPosition, newRotation, placeableObject.unlockableID, thisUnlockableName);
                        Debug.Log(objnew.unlockableName + " ID " + objnew.unlockableID);
                        PersistentShipObjects.UpdateObjectManager(objnew);

                    }
                    else
                    {
                        Debug.LogError("ShipBuildModeManagerPatch: Transform is null");
                    }
                }
                else
                {
                    Debug.LogError("ShipBuildModeManagerPatch: placeableObject's parent object");
                }
            }
            else
            {
                Debug.LogError("ShipBuildModeManagerPatch: placeableObject is null");
            }
        }


        [HarmonyPatch(typeof(StartOfRound), "LoadUnlockables")]
        [HarmonyPostfix]
        public static void LoadUnlockables()
        {
            Debug.Log("MOVING SHIT AROUND!!!");
            foreach (PlaceableShipObject obj in GameObject.FindObjectsOfType(typeof(PlaceableShipObject)))
            {   

                //persistentShipObjects.DebugPrintDescendantsWrapper(obj.transform);
                TransformObject foundObj = PersistentShipObjects.FindObjectIfExists(obj.unlockableID);
                if (foundObj != null) {
                    Debug.Log("    Item Found: " + foundObj.unlockableName + " at pos " + obj.transform.parent.transform.position);

                    Debug.Log("        type: " + obj.GetType());
                    Debug.Log("        name: " + obj.name);
                    Debug.Log("        parent type:" + obj.transform.parent?.gameObject.GetType());
                    Debug.Log("        parent name:" + obj.transform.parent?.name);

                    // todo: GHBB THIS MUST BE RELATIVE TO THE SHIP, THE SHIP FUCKIGN MOVES
                    obj.transform.parent.transform.position = foundObj.pos.GetVector3();
                    obj.transform.parent.transform.rotation = Quaternion.Euler(foundObj.rot.GetVector3());

                    Debug.Log("        new pos: " + obj.transform.parent.transform.position);
                } else {
                    Debug.Log("    couldn't find " + obj.transform.parent?.name);
                }
            }
        }
    }
}
