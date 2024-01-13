using HarmonyLib;
using LethalLib;
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
        static int lastPlacementPlayer = -1;

        public static void Awake() {
            Debug.LogWarning("ShipBuildModeManagerPatch waking up");
        }

        public static void Start() {
            Debug.LogWarning("ShipBuildModeManagerPatch starting up");
        }

        static void PrintChildrenNames(Transform parent, int depth) {
            String indentMinusOne = "";
            String tab = "|   ";
            for (int i = 0; i < depth; i++) {
                indentMinusOne += tab;
            }

            Debug.Log(indentMinusOne + "P " + parent.gameObject.GetType() + " named " + parent.name + "-------------------- P of " + parent.childCount);

            foreach (Transform child in parent) {
                // Print the name of the child

                Debug.Log(indentMinusOne + tab + child.GetType() + " named " + child.name);

                // If the child has further children, recursively call the function
                if (child.childCount > 0) {
                    PrintChildrenNames(child, depth + 1);
                }
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpc")] // SaveObjTransform(string name, Transform trans) checks for if host player
        [HarmonyPostfix]
        public static void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef, int playerWhoMoved) {
            Debug.LogWarning("entering PlaceShipObjectClientRpc patch");
            lastPlacementPlayer = playerWhoMoved;
            Debug.LogError(playerWhoMoved);
            objectRef.TryGet(out NetworkObject netObj);

            PlaceableShipObject placeableObject = netObj.GetComponentInChildren<PlaceableShipObject>();
            if (placeableObject != null) {
                String actualItemName = (placeableObject.transform.parent?.gameObject)?.name;

                if (actualItemName != null && placeableObject.transform != null) {
                    Debug.LogWarning(placeableObject.GetType() + " named: " + actualItemName);

                    Debug.LogWarning("parent pos is: " + placeableObject.transform.parent?.transform.position ?? null);
                    Debug.LogWarning("parent rot is: " + placeableObject.transform.parent?.transform.rotation ?? null);

                    Debug.LogWarning("ShipBuildModeManagerPatch: Saving trans of " + placeableObject.GetType() + " named " + actualItemName + " at pos " + newPosition);

                    Debug.Log("my grandparent: " + placeableObject.transform.parent?.transform.parent?.gameObject.name);
                    Debug.LogError("printing tree");
                    PrintChildrenNames(placeableObject.transform.parent?.transform, 0);

                    Transform placeableObjectTransform = placeableObject.transform;
                    Transform grandparentTransform = placeableObject.transform.parent?.transform.parent?.transform; // the ship

                    Vector3 relativePosition = grandparentTransform.position - placeableObjectTransform.position;

                    Quaternion relativeRotation = Quaternion.Inverse(grandparentTransform.rotation) * placeableObjectTransform.rotation;

                    Transform newTrans = PersistentShipObjects.PosAndRotAsTransform(relativePosition, relativeRotation);
                    PersistentShipObjects.SaveObjTransform(actualItemName, newTrans);
                } else {
                    Debug.LogError("ShipBuildModeManagerPatch: Transform is null");
                }
            } else {
                Debug.LogWarning("ShipBuildModeManagerPatch: placeableObject is null");
            }
            Debug.LogWarning("exiting PlaceShipObjectClientRpc patch & moving to PlaceShipObjectClientRpc");
        }


        // PlaceShipObject code snippet:
        /*		Vector3 rotationOffset = placeableObject.parentObject.rotationOffset;
		StartOfRound.Instance.suckingFurnitureOutOfShip = false;
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedPosition = placementPosition;
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedRotation = placementRotation;
		Debug.Log($"Saving placed position as: {placementPosition}");
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].hasBeenMoved = true;
		if (placeableObject.parentObjectSecondary != null)
		{
			Vector3 position = placeableObject.parentObjectSecondary.transform.position;
			Quaternion rotation = placeableObject.parentObjectSecondary.transform.rotation;
			Quaternion quaternion = Quaternion.Euler(placementRotation) * Quaternion.Inverse(placeableObject.mainMesh.transform.rotation);
			placeableObject.parentObjectSecondary.transform.rotation = quaternion * placeableObject.parentObjectSecondary.transform.rotation;
			placeableObject.parentObjectSecondary.position = placementPosition + (placeableObject.parentObjectSecondary.transform.position - placeableObject.mainMesh.transform.position) + (placeableObject.mainMesh.transform.position - placeableObject.placeObjectCollider.transform.position);
			if (!StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(placeableObject.parentObjectSecondary.transform.position))
			{
				placeableObject.parentObjectSecondary.transform.position = position;
				placeableObject.parentObjectSecondary.transform.rotation = rotation;
			}
		}
		else if (placeableObject.parentObject != null)
		{
			Vector3 position = placeableObject.parentObject.positionOffset;
			Quaternion rotation = placeableObject.parentObject.transform.rotation;
			Quaternion quaternion2 = Quaternion.Euler(placementRotation) * Quaternion.Inverse(placeableObject.mainMesh.transform.rotation);
			placeableObject.parentObject.rotationOffset = (quaternion2 * placeableObject.parentObject.transform.rotation).eulerAngles;
			placeableObject.parentObject.transform.rotation = quaternion2 * placeableObject.parentObject.transform.rotation;
			placeableObject.parentObject.positionOffset = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(placementPosition + (placeableObject.parentObject.transform.position - placeableObject.mainMesh.transform.position) + (placeableObject.mainMesh.transform.position - placeableObject.placeObjectCollider.transform.position));
			if (!StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(placeableObject.parentObject.transform.position))
			{
				placeableObject.parentObject.positionOffset = position;
				placeableObject.parentObject.transform.rotation = rotation;
				placeableObject.parentObject.rotationOffset = rotationOffset;
			}
		}*/

        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObject")]
        [HarmonyPrefix]
        public static void PlaceShipObject(ref Vector3 placementPosition, ref Vector3 placementRotation, PlaceableShipObject placeableObject) {
            Debug.LogWarning("entering PlaceShipObject patch");            
            Transform oldTrans = PersistentShipObjects.PosAndRotAsTransform(placementPosition, Quaternion.Euler(placementRotation));

            //placeableObject.transform.position = oldTrans.position;
            //placeableObject.transform.rotation = oldTrans.rotation;

            Debug.LogWarning("transform before: " + oldTrans.position.ToString() + "; " + oldTrans.rotation.ToString());
            if (placeableObject.transform == null) {
                Debug.LogWarning("placeableObject.transform is null!");
            } else {
                Debug.LogWarning("placeableObject.transform isn't null!");
                String actualItemName = (placeableObject.transform.parent.gameObject)?.name;
                if (actualItemName != null) {
                    Debug.LogWarning(placeableObject.GetType() + " named: " + actualItemName);

                    Debug.LogWarning("parent pos is: " + placeableObject.transform.parent?.transform.position ?? null);
                    Debug.LogWarning("parent rot is: " + placeableObject.transform.parent?.transform.rotation ?? null);

                    Debug.Log("my grandparent: " + placeableObject.transform.parent?.transform.parent?.gameObject.name);
                    Debug.LogWarning("printing tree");
                    PrintChildrenNames(placeableObject.transform.parent?.transform, 0);

                    if (PersistentShipObjects.ObjTransforms?.ContainsKey(actualItemName) ?? false) { // if name in ObjTransforms
                        Debug.LogWarning("item in ObjTransforms!");
                        Transform savedTransOffset = PersistentShipObjects.ObjTransforms[actualItemName];

                        if (savedTransOffset == null) {
                            Debug.LogError("saved trans offset is null!");
                        } else {
                            Debug.LogWarning("transform: " + savedTransOffset.ToString());
                            //placementPosition = savedTransOffset.position - placeableObject.transform.parent.transform.position;

                            Transform placeableObjectTransform = placeableObject.transform;
                            Transform grandparentTransform = placeableObject.transform.parent?.transform.parent?.transform;


                            Quaternion relativeRotation = Quaternion.Inverse(grandparentTransform.rotation) * savedTransOffset.rotation; // todo: does this do???


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
                        Debug.LogWarning("item not in ObjTransforms!");// Saving!");
                                                                        //PersistentShipObjects.SaveObjTransform(actualItemName, oldTrans);
                    }//*/
                } else {
                    Debug.LogWarning("item name is null!");
                }
            }
            Debug.LogWarning("trans before: " + oldTrans.position.ToString() + "; " + oldTrans.rotation.ToString());
            oldTrans = PersistentShipObjects.PosAndRotAsTransform(placementPosition, Quaternion.Euler(placementRotation)) ?? null;
            Debug.LogWarning("trans after: " + oldTrans.position.ToString() + "; " + oldTrans.rotation.ToString());
            Debug.LogWarning("exiting PlaceShipObject patch & moving to PlaceShipObject");
        }


        [HarmonyPatch(typeof(StartOfRound))]
        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        public static void StartGame(){
            lastPlacementPlayer = -1;
        }
    }
}
