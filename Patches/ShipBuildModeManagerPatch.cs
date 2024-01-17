using HarmonyLib;
using LethalCompanyTemplate;
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

                        // Updates ObjectManager with new information 
                        TransformObjects objnew = new TransformObjects(newPosition, newRotation, placeableObject.unlockableID, StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].unlockableName);
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
            foreach (PlaceableShipObject obj in UnityEngine.Object.FindObjectsOfType(typeof(PlaceableShipObject)))
            {   
                int thisUnlockableID = obj.unlockableID;
                Debug.Log(obj.GetType());
                Debug.Log(obj.name);
                Debug.Log(obj.transform.parent?.gameObject.GetType());
                Debug.Log(obj.transform.parent?.name);

                //rsistentShipObjects.DebugPrintDescendantsWrapper(obj.transform);

                // should this be GetComponent?
                TransformObjects foundObj = PersistentShipObjects.FindObjectIfExists(obj.unlockableID);
                if (foundObj != null)
                {
                    Debug.Log("Item Found!: " + foundObj.unlockableName);

                    // todo: GHBB THIS MUST BE RELATIVE TO THE SHIP, THE SHIP FUCKING MOVES
                    obj.transform.parent.transform.position = foundObj.getPos();
                    obj.transform.parent.transform.rotation = UnityEngine.Quaternion.Euler(foundObj.getRotation());
                }
            }
        }
    }
}
