using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace PersistentShipObjects.Patches {
    internal class ShipBuildModeManagerPatch {
        public static void Awake() {
            Debug.Log("ShipBuildModeManagerPatch waking up");
        }

        public static void Start() {
            Debug.Log("ShipBuildModeManagerPatch starting up");
        }

        /*static void PrintChildrenNames(Transform parent, int depth) {
            String indent = "";
            String tab = "  ";
            for (int i = 0; i < depth + 1; i++) {
                indent += tab;
            }

            Debug.Log("E" + indent + "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
            Debug.Log(parent.childCount);

            foreach (Transform child in parent) {
                // Print the name of the child

                Debug.Log(indent + "Child: " + child.name);

                // If the child has further children, recursively call the function
                if (child.childCount > 0) {
                    PrintChildrenNames(child, depth + 1);
                }
            }
        }*/

        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpc")]
        [HarmonyPostfix]
        public static void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef) {
            if (1 == 1) { //RoundManager.Instance.NetworkManager.IsHost) {
                objectRef.TryGet(out NetworkObject netObj);

                PlaceableShipObject placeableShipObj = netObj.GetComponentInChildren<PlaceableShipObject>();

                if (placeableShipObj != null) {
                    String actualItemName = (placeableShipObj.transform.parent?.gameObject)?.name;

                    if (placeableShipObj.transform != null) {
                        Debug.Log("ShipBuildModeManagerPatch: Saving trans of " + placeableShipObj.GetType() + " named " + actualItemName + " at pos " + newPosition);

                        Transform newTrans = PersistentShipObjects.PosAndRotAsTransform(newPosition, Quaternion.Euler(newRotation));
                        PersistentShipObjects.SaveObjTransform(actualItemName, ref newTrans);
                    } else {
                        Debug.LogError("ShipBuildModeManagerPatch: Transform is null");
                    }
                } else {
                    Debug.Log("ShipBuildModeManagerPatch: placeableShipObj is null");
                }
            } else {
                Debug.Log("ShipBuildModeManagerPatch: Not the host client");
            }
        }


        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObject")]
        [HarmonyPrefix]
        public static void PlaceShipObject(ref Vector3 placementPosition, ref Vector3 placementRotation, PlaceableShipObject placeableObject) {
            if (placeableObject.transform == null) {
                Debug.Log("placeableObject.transform is null!");
            } else {
                String actualItemName = (placeableObject.transform.parent.gameObject)?.name;

                if (actualItemName != null && (PersistentShipObjects.ObjTransforms?.ContainsKey(actualItemName) ?? false)) { // if name in config
                    Transform newTrans = PersistentShipObjects.ObjTransforms[actualItemName];
                    placementPosition = newTrans.position;
                    placementRotation = newTrans.rotation.eulerAngles;
                }
            }
        }
    }
}
