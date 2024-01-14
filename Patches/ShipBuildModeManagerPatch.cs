using HarmonyLib;
using LethalLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace PersistentShipObjects.Patches {
    internal class ShipBuildModeManagerPatch {
        const int A_CONCERNING_AMOUNT_OF_NESTING = 30; // arbitrary magic number for DebugPrintDescendants a transform having a 
                                                       // great great great great great great great great great great great great great great great great great great great great great great great great great great great great
                                                       // grandchild is.. probably wrong, if not very dumb.                                                         - viv
        const String TAB = "|   ";

        static int lastPlacementPlayer = -1;

        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpc")]
        [HarmonyPostfix]
        public static void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef, int playerWhoMoved) {
            // SaveObjTransform(string name, Transform trans) checks for if host player, check isn't needed here
            // todo: but move it here anyway

            PersistentShipObjects.DebugLog("entering PlaceShipObjectClientRpc patch");

            lastPlacementPlayer = playerWhoMoved;
            objectRef.TryGet(out NetworkObject netObj);
            PlaceableShipObject placeableObject = netObj.GetComponentInChildren<PlaceableShipObject>();

            //----------------------------------------------------------------------------------------------------------------------------------- start checks
            if (placeableObject != null) {
            GameObject actualObject = placeableObject.transform.parent?.gameObject;

                if (actualObject != null) {
                    String actualItemName = actualObject.name;

                    if (placeableObject.transform != null) {
                        //----------------------------------------------------------------------------------------------------------------------- end checks
                        //----------------------------------------------------------------------------------------------------------------------- start debug
                        PersistentShipObjects.DebugLog(placeableObject.GetType() + " named: " + actualItemName);

                        PersistentShipObjects.DebugLog("parent pos is: " + placeableObject.transform.parent?.transform.position ?? null);
                        PersistentShipObjects.DebugLog("parent rot is: " + placeableObject.transform.parent?.transform.rotation ?? null);

                        Debug.Log("ShipBuildModeManagerPatch: Saving trans of " + placeableObject.GetType() + " named " + actualItemName + " at pos " + newPosition);

                        PersistentShipObjects.DebugLog("my grandparent: " + placeableObject.transform.parent?.transform.parent?.gameObject.name);
                        PersistentShipObjects.DebugLog("printing tree");
                        DebugPrintDescendantsWrapper(placeableObject.transform.parent?.transform);
                        //----------------------------------------------------------------------------------------------------------------------- end debug

                        Transform placeableObjectTransform = placeableObject.transform;
                        Transform shipTransform = placeableObject.transform.parent?.transform.parent?.transform;

                        Vector3 relativePosition = shipTransform.position - placeableObjectTransform.position;

                        Quaternion relativeRotation = Quaternion.Inverse(shipTransform.rotation) * placeableObjectTransform.rotation;

                        Transform newTrans = PersistentShipObjects.PosAndRotAsTransform(relativePosition, relativeRotation);
                        PersistentShipObjects.SaveObjTransform(actualItemName, newTrans);
                    } else {
                        Debug.LogError("ShipBuildModeManagerPatch: Transform is null");
                    }
                } else {
                    Debug.LogError("ShipBuildModeManagerPatch: placeableObject's parent object");
                }
            } else {
                Debug.LogError("ShipBuildModeManagerPatch: placeableObject is null");
            }
            PersistentShipObjects.DebugLog("exiting PlaceShipObjectClientRpc patch & moving to PlaceShipObjectClientRpc");
        }

        static void DebugPrintDescendantsWrapper(Transform parent) {
            if (PersistentShipObjects.doDebugPrints.Value == false) return;
            DebugPrintDescendants(parent, "");
        }


        // I wonder if there's a way to pass indentMinusOne as a ref to avoid making 500 copies of it                                                               -viv
        static void DebugPrintDescendants(Transform parent, string indentMinusOne) { 
            /*String indentMinusOne = "";                 // leaving O(n^2) code here as a reminder to not concat strings like this.
            for (int i = 0; i < depth; i++) {           // Shouldn't matter without deep nesting, which is now Concerning(tm) anyway
                indentMinusOne += TAB;                  // but also this function is recursive as hell, soooooooo- try                                              -viv
            }//*/

            indentMinusOne += TAB;
            if (indentMinusOne.Length > A_CONCERNING_AMOUNT_OF_NESTING * TAB.Length) {
                Debug.LogWarning("DebugPrintDescendants: depth is "+ (indentMinusOne.Length/4).ToString()+", which is probably very wrong. If this is a mod conflict, got dang they are nesting too hard. Have probably a comical amount of error:");
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                Debug.LogError(stackTrace);
                return;
            }

            Debug.Log(indentMinusOne + "P " + parent.gameObject.GetType() + " named " + parent.name + "-------------------- P of " + parent.childCount);

            foreach (Transform child in parent) {
                PersistentShipObjects.DebugLog(indentMinusOne + TAB + child.GetType() + " named " + child.name);

                if (child.childCount > 0) {
                    DebugPrintDescendants(child, indentMinusOne);
                }
            }
        }


        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObject")]
        [HarmonyPrefix]
        public static void PlaceShipObject(ref Vector3 placementPosition, ref Vector3 placementRotation, PlaceableShipObject placeableObject) {
            PersistentShipObjects.DebugLog("entering PlaceShipObject patch");
            Transform oldTrans = PersistentShipObjects.PosAndRotAsTransform(placementPosition, Quaternion.Euler(placementRotation));

            //----------------------------------------------------------------------------------------------------------------------------------- start checks
            if (placeableObject.transform == null) {
                PersistentShipObjects.DebugLog("placeableObject.transform is null!");
            
            } else {
                PersistentShipObjects.DebugLog("placeableObject.transform isn't null!");
                String actualItemName = (placeableObject.transform.parent.gameObject)?.name;
            
                if (actualItemName != null) {
                    PersistentShipObjects.DebugLog(placeableObject.GetType() + " named: " + actualItemName);

                    PersistentShipObjects.DebugLog("parent pos is: " + placeableObject.transform.parent?.transform.position ?? null);
                    PersistentShipObjects.DebugLog("parent rot is: " + placeableObject.transform.parent?.transform.rotation ?? null);

                    Debug.Log("my grandparent: " + placeableObject.transform.parent?.transform.parent?.gameObject.name);
                    PersistentShipObjects.DebugLog("printing tree");
                    DebugPrintDescendantsWrapper(placeableObject.transform.parent?.transform);

                    if (PersistentShipObjects.ObjTransforms?.ContainsKey(actualItemName) ?? false) {
                        PersistentShipObjects.DebugLog("item in ObjTransforms!");
                        Transform savedTransOffset = PersistentShipObjects.ObjTransforms[actualItemName];

                        if (savedTransOffset == null) {
                            Debug.LogError("saved trans offset is null!");
                
                        } else {
                            //--------------------------------------------------------------------------------------------------------------------- end checks
                            PersistentShipObjects.DebugLog("transform: " + savedTransOffset.ToString());

                            Transform placeableObjectTransform = placeableObject.transform;
                            Transform grandparentTransform = placeableObject.transform.parent?.transform.parent?.transform;

                            Quaternion relativeRotation = Quaternion.Inverse(grandparentTransform.rotation) * savedTransOffset.rotation;

                            if (lastPlacementPlayer > -1) {
                                Debug.LogError("last placed object was by player " + lastPlacementPlayer + "; not updating placement transform after player placement");
                                lastPlacementPlayer = -1;
                        
                            } else {
                                Debug.LogError("last placed object was not by a player, updating transform");
                                placementRotation = relativeRotation.eulerAngles;
                                placementPosition = savedTransOffset.position - grandparentTransform.position;
                            }
                        }
                    } else {
                        PersistentShipObjects.DebugLog("item not in ObjTransforms!");
                 
                    }
                } else {
                    PersistentShipObjects.DebugLog("item name is null!");
             
                }
            }
            PersistentShipObjects.DebugLog("trans before: " + oldTrans.position.ToString() + "; " + oldTrans.rotation.ToString());
            oldTrans = PersistentShipObjects.PosAndRotAsTransform(placementPosition, Quaternion.Euler(placementRotation)) ?? null;
            PersistentShipObjects.DebugLog("trans after: " + oldTrans.position.ToString() + "; " + oldTrans.rotation.ToString());
            PersistentShipObjects.DebugLog("exiting PlaceShipObject patch & moving to PlaceShipObject");
        }


        [HarmonyPatch(typeof(StartOfRound), "StartGame")]
        [HarmonyPostfix]
        public static void StartGame() {
            lastPlacementPlayer = -1;
        }
    }
}
