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
        [HarmonyPrefix]
        public static void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef, int playerWhoMoved)
        {
            objectRef.TryGet(out NetworkObject netObj);
            PlaceableShipObject placeableObject = netObj.GetComponentInChildren<PlaceableShipObject>();

            if (placeableObject == null) { Debug.LogError("ShipBuildModeManagerPatch: placeableObject is null"); goto PlaceShipObjectClientRpcEarlyReturn; }
            if (placeableObject.transform == null) { Debug.LogError("ShipBuildModeManagerPatch: placeableObject.transform is null"); goto PlaceShipObjectClientRpcEarlyReturn; }

            GameObject actualObject = placeableObject.transform.parent?.gameObject;

            if (actualObject == null) { Debug.LogError("ShipBuildModeManagerPatch: actualObject is null"); goto PlaceShipObjectClientRpcEarlyReturn;}

            String actualItemName = actualObject.name;

            Debug.Log("ShipBuildModeManagerPatch: Saving trans of " + placeableObject.GetType() + " named " + actualItemName + " at pos " + newPosition);

            Transform parentTrans = placeableObject.transform.parent.transform;

            Debug.Log("old parent position: " + placeableObject.transform.parent.position);

            String thisUnlockableName = StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].unlockableName;

            // todo: if this works I'm eating my own ass

            // todo: it mostly works in place of [HarmonyPatch(typeof(StartOfRound), "OnEnable")]
            /*StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedPosition = newPosition;
            StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedRotation = newRotation;*/


            //Vector3 offset = -placeableObject.transform.position;

            //TransformObject objnew = new TransformObject(parentTrans.position, parentTrans.rotation.eulerAngles, placeableObject.unlockableID, thisUnlockableName);
            TransformObject objnew = new TransformObject(newPosition, newRotation, placeableObject.unlockableID, thisUnlockableName);
            
            Debug.Log(objnew.unlockableName + " ID " + objnew.unlockableID);
            PersistentShipObjects.UpdateObjectManager(objnew);


            PlaceShipObjectClientRpcEarlyReturn:
            Console.WriteLine("early return");
        }

        /*
        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObject")]
        [HarmonyPostfix]
        public static void PlaceShipObject(ref Vector3 placementPosition, ref Vector3 placementRotation, PlaceableShipObject placeableObject) {
            makePSOFuckOff(placeableObject, "PlaceShipObject");
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "ResetShipObjectToDefaultPosition")]
        [HarmonyPostfix]
        public static void ResetShipObjectToDefaultPosition(PlaceableShipObject placeableObject) {
            makePSOFuckOff(placeableObject, "ResetShipObjectToDefaultPosition");
        }//*/




        /*[HarmonyPatch(typeof(ShipBuildModeManager), "Awake")] // some code nabbed from ResetShipObjectToDefaultPosition
        [HarmonyPostfix]
        public static void Awake(ref StartOfRound __instance) {
            makeAllPSOsFuckOff("ShipBuildModeManager Awake()");
        }//*/

        /*[HarmonyPatch(typeof(StartOfRound), "OnEnable")] // some code nabbed from ResetShipObjectToDefaultPosition
        [HarmonyPostfix]
        public static void OnEnable() {
            makeAllPSOsFuckOff("StartOfRound OnEnable()");
        }//*/

        /*[HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectServerRpc")]
        [HarmonyPostfix]
        public static void PlaceShipObjectServerRpc(NetworkObjectReference objectRef) {
            objectRef.TryGet(out NetworkObject netObj);
            makePSOFuckOff(netObj.GetComponentInChildren<PlaceableShipObject>(), "PlaceShipObjectServerRpc");
        }//*/



        public static void makeAllPSOsFuckOff(string strxjksdf) {
            foreach (PlaceableShipObject obj in GameObject.FindObjectsOfType(typeof(PlaceableShipObject))) {
                makePSOFuckOff(obj, strxjksdf);
            }
        }//*/

        public static void makePSOFuckOff(PlaceableShipObject obj, String caller) {
            Debug.Log("sending PSO " + obj.name + " to fuck off land from " + caller + "&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&");

            Vector3 fuckOffLand = new Vector3(1000, 10000, 1000);
            if (obj.parentObjectSecondary != null) {
                obj.parentObjectSecondary.position = fuckOffLand;
            } else if (obj.parentObject != null) {
                //obj.parentObject.positionOffset = fuckOffLand;
                obj.transform.parent.position = fuckOffLand;
            }
        }

                    

        [HarmonyPatch(typeof(StartOfRound), "OnEnable")] // some code nabbed from ResetShipObjectToDefaultPosition
        [HarmonyPostfix]
        public static void Start(ref StartOfRound __instance)
        {
            Vector3 fuckOffLand = new Vector3(1000, 1000, 1000);

            Debug.Log("MOVING SHIT AROUND!!!");
            foreach (PlaceableShipObject obj in GameObject.FindObjectsOfType(typeof(PlaceableShipObject))) {

                PersistentShipObjects.DebugPrintDescendantsWrapper(obj?.transform);
                TransformObject foundObj = PersistentShipObjects.FindObjectIfExists(obj.unlockableID);
                if (foundObj != null) {
                    Debug.Log("    Item Found: " + foundObj.unlockableName + " at pos " + obj.transform.parent.transform.position);

                    Debug.Log("        type: " + obj.GetType());
                    Debug.Log("        name: " + obj.name);
                    Debug.Log("        parent type: " + obj.transform.parent?.gameObject.GetType());
                    Debug.Log("        parent name: " + obj.transform.parent?.name);

                    //makePSOFuckOff(obj, "DO IT DO IT NOW FUCK OFF GO FUCK OFF NOW BITCH GOOOOOOO     AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");


                    //__instance.unlockablesList.unlockables[obj.unlockableID].placedPosition = foundObj.pos.GetVector3();
                    //__instance.unlockablesList.unlockables[obj.unlockableID].placedRotation = foundObj.rot.GetVector3();
                    //__instance.unlockablesList.unlockables[obj.unlockableID].hasBeenMoved = false;

                    //__instance.unlockablesList.unlockables[obj.unlockableID].placedPosition = fuckOffLand;
                    //__instance.unlockablesList.unlockables[obj.unlockableID].placedRotation = foundObj.rot.GetVector3();
                    //*/
                    /*

                    obj.parentObject.startingPosition = foundObj.pos.GetVector3();
                    obj.parentObject.startingRotation = foundObj.rot.GetVector3();//*/

                    obj.parentObject.transform.position = foundObj.pos.GetVector3();
                    obj.parentObject.transform.rotation = Quaternion.Euler(foundObj.rot.GetVector3());//*/



                    //obj.transform.parent.transform.position = foundObj.pos.GetVector3() + new Vector3 (0, 2, 0);
                    //obj.transform.parent.transform.rotation = Quaternion.Euler(foundObj.rot.GetVector3());

                    Debug.Log("        new pos: " + obj.transform.parent.position);
                } else {
                    Debug.Log("    couldn't find " + obj.transform.parent?.name);
                }

                /*Console.WriteLine("sending " + obj.name + " to fuckoff land");
                obj.transform.position = fuckOffLand;
                obj.transform.parent.position = fuckOffLand;
                obj.transform.parent.parent.position = fuckOffLand;*/
            }
        }//*/
    }
}
