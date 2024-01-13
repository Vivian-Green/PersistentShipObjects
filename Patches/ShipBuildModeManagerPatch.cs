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

        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpc")]
        [HarmonyPostfix]
        public static void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef) {
            //Debug.Log("A");

            if (1 == 1) {//RoundManager.Instance.NetworkManager.IsHost) {
                //Debug.Log("A1");

                static void PrintChildrenNames(Transform parent, int depth) {
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
                }

                NetworkObject netObj;
                objectRef.TryGet(out netObj);

                PlaceableShipObject placeableShipObj = netObj.GetComponentInChildren<PlaceableShipObject>();

                PrintChildrenNames(placeableShipObj.transform, 0);//*/

                if (placeableShipObj != null) {
                    String actualItemName = (placeableShipObj.transform.parent.gameObject)?.name;
                    //Debug.Log("A1a1");

                    Debug.Log("ShipBuildModeManagerPatch: Saving trans of " + placeableShipObj.GetType() + " named " + actualItemName + " at pos " + newPosition);


                    PersistentShipObjects.SaveObjTransform(actualItemName, PersistentShipObjects.PosAndRotAsTransform(newPosition, Quaternion.Euler(newRotation)));
                } else {
                    Debug.Log("ShipBuildModeManagerPatch: A1b1 - this shouldn't print");
                }
            } else {
                Debug.Log("A2");
                Debug.LogError("ShipBuildModeManagerPatch: Not the host client");
            }
        }
    }
}
