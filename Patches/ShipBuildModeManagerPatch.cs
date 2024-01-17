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
        const int A_CONCERNING_AMOUNT_OF_NESTING = 30; // arbitrary magic number for DebugPrintDescendants a transform having a 
                                                       // great great great great great great great great great great great great great great great great great great great great great great great great great great great great
                                                       // grandchild is.. probably wrong, if not very dumb.                                                         - viv
        const String TAB = "|   ";

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
            // Why does this keep finding colliders??
            // Last part to fix??
            Debug.Log("MOVING SHIT AROUND!!!");
            foreach (Collider obj in UnityEngine.Object.FindObjectsOfType(typeof(PlaceableShipObject)))
            {
                PlaceableShipObject newobj = obj.GetComponentInParent<PlaceableShipObject>();
                Debug.Log(newobj.name);
                TransformObjects foundObj = PersistentShipObjects.GetObjects(obj.unlockableID);
                if (foundObj != null)
                {
                    Debug.Log("Item Found!: " + foundObj.unlockableName);
                    obj.gameObject.transform.position = foundObj.getPos();
                    obj.gameObject.transform.rotation = UnityEngine.Quaternion.Euler(foundObj.getRotation());
                }
            }
        }
    }
}
