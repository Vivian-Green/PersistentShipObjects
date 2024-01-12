using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace PersistentShipObjects { // todo: do the objects move server-side?? like fuck idk

    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("evaisa.lethallib", "0.11.1")]
    [BepInProcess("Lethal Company.exe")]
    public class Plugin : BaseUnityPlugin {
        public const string modGUID = "VivianGreen.PersistentShipObjects";
        public const string modName = "PersistentShipObjects";
        public const string modVersion = "0.0.7"; // todo: edit regex in export.py to match this

        public static PersistantShipObjectsConfig persistantShipObjectsConfig { get; internal set; }

        public System.Collections.Generic.Dictionary<string, VivsTrans> VivsTranses;

        public static Plugin Instance;
        internal ManualLogSource mls;

        public readonly Harmony harmony = new Harmony(modGUID);

        public bool safeInjection = true; // fucking.. it being false just makes the transpiling slightly slower- it's a load times thing lmao, definitely default to true

        /*private static void LoadFromManifest() {
            try {
                // Read manifest.json
                string manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VivianGreen-PersistentShipObjects", "manifest.json");
                string manifestJson = File.ReadAllText(manifestPath);
                JObject manifest = JObject.Parse(manifestJson);

                // Retrieve value(s) from manifest.json
                modVersion = (string)manifest["version_number"] ?? "err loading version number from manifest.json";
            } catch (Exception ex) {
                Console.WriteLine($"Error loading manifest.json: {ex.Message}");
            }
        }*/

        private void Awake() {
            print("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAa");
            //LoadFromManifest(); // get version #

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("PSO says is waking up");

            if (Instance == null) {
                Instance = this;
            }
            persistantShipObjectsConfig = new PersistantShipObjectsConfig(base.Config);

            VivsTranses = PersistantShipObjectsConfig.shipObjectVivTransforms.Value; // todo: this is either necessary (need to instantiate here if null - doubt) or will break things (overwrite old configs-), and I'm not 100% sure which, so I'm leaving it: ?? new Dictionary<String, VivsTrans> { { "testName", new VivsTrans(new Vector3(0, 0, 0), Quaternion.identity) } };
        }

        public bool SaveObjTransform(string name, Transform trans) {
            //return true;
            Vector3 thisPos = trans.position;
            Quaternion thisRot = trans.rotation;

            mls.LogWarning("saving trans of obj named: " + name);
            try {
                // Set if exists, else add
                // exists ? set : add
                if (VivsTranses.ContainsKey(name)) {
                    VivsTranses[name] = new VivsTrans(thisPos, thisRot);
                } else {
                    VivsTranses.Add(name, new VivsTrans(thisPos, thisRot));
                }

                try {
                    PersistantShipObjectsConfig.shipObjectVivTransforms.Value = VivsTranses;
                    PersistantShipObjectsConfig.shipObjectVivTransforms.ConfigFile.Save();
                } catch {
                    Debug.Log("FUCK!");
                }

            } catch (Exception ex) {
                // Log the exception and return false
                mls.LogError("Error saving placeable ship object transform: " + ex.Message);
                return false;
            }
            return true;
        }
    }

    public class ShipBuildModeManagerPatch {
        internal ManualLogSource mls;
        void Awake() {
            Debug.LogError("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            mls = Plugin.Instance.mls;
            mls.LogInfo("shipBuildModeManagerPatch is waking up");
            Plugin.Instance.harmony.PatchAll();
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpcPostfix")]
        [HarmonyPostfix]
        public static void PlaceShipObjectClientRpcPostfix(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef, int playerWhoMoved) {
            if (StartOfRound.Instance.localPlayerController.playerClientId > 0) {
                Debug.LogError("ShipBuildModeManagerPatch: Not the host client");
                return;
            }

            NetworkObject actualObject;
            if (!objectRef.TryGet(out actualObject)) {
                Debug.LogError("ShipBuildModeManagerPatch: NetworkObjectReference.TryGet() could not find a matching NetworkObject");
                return;
            }

            Type objType = objectRef.GetType();
            String objName = actualObject.name;
            Transform objTrans = actualObject.transform;

            if (objTrans == null) {
                Debug.LogError("ShipBuildModeManagerPatch: Object transform is null");
                return;
            }

            Debug.Log("ShipBuildModeManagerPatch: Saving trans of " + objType + " named " + objName + " at position " + objTrans.position);
            Plugin.Instance.SaveObjTransform(objName, objTrans);
        }
    }

    public class SpawnUnlockablePatch {
        internal ManualLogSource mls;
        void Awake() {
            Debug.LogError("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCc");
            mls = Plugin.Instance.mls;
            mls.LogInfo("SpawnUnlockablePatch is waking up");
            Plugin.Instance.harmony.PatchAll();
        }

        [HarmonyPatch(typeof(StartOfRound), "SpawnUnlockable")]
        [HarmonyPostfix]
        static void SpawnUnlockable(int unlockableIndex, GameObject __result) {
            if (__result != null && (Plugin.Instance.VivsTranses?.ContainsKey(__result.name) ?? false)) { // if name in config
                // overwrite transform
                VivsTrans newTrans = Plugin.Instance.VivsTranses[__result.name];
                __result.transform.position = newTrans.position;
                __result.transform.rotation = newTrans.rotation;
            }
        }
    }
    
    public class LoadShipGrabbableItemsPatch {
        internal ManualLogSource mls;
        void Awake() {
            Debug.LogError("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDdd");
            mls = Plugin.Instance.mls;
            mls.LogInfo("LoadShipGrabbableItemsPatch is waking up");
            Plugin.Instance.harmony.PatchAll();
        }

        [HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            int tfIndexInLocalVariablesIsHidingMyGotDangComponent = 0;
            var codes = new List<CodeInstruction>(instructions);

            //
            /* ok bitch fucker, look at this shit in "Lethal Company\Lethal Company_Data\Managed\Assembly-CSharp.dll" StartOfRound.LoadShipGrabbableItems() (viewed with ILSpy, IL with C#):

            // GrabbableObject component = UnityEngine.Object.Instantiate(allItemsList.itemsList[array[i]].spawnPrefab, array2[i], Quaternion.identity, elevatorTransform).GetComponent<GrabbableObject>();
            [many skipped IL instructions]
            IL_01ba: callvirt instance !!0 [UnityEngine.CoreModule]UnityEngine.GameObject::GetComponent<class GrabbableObject>()
			IL_01bf: stloc.0
			// component.fallTime = 1f;

            This makes as component, then stloc (pop from stack (which is the return of the expression, an GrabbableObject- well a pointer to one))'s it into the 0th local variable. 
            This is 17 lines before our injection, at the time of writing this comment (Jan 10, 2024)

            You are welcome, whoever is reading this. Should this break (and it likely will, as it's trans(based; and red)piled), maybe someone (future viv-) will know 
            where tf to look to find that index in the future.
            //*/

            Debug.Log("this motherfucker is straight TRANSpiling which is poggies");
            for (int i = 0; i < codes.Count; i++) {
                Debug.Log(codes[i].ExtractLabels());

                if (!(codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("Spawn"))) continue;
                // just before component.NetworkObject.Spawn();

                Debug.Log("yo bitch fucker, i is "+i+" when shit gets insertamalated, we injecting UpdateGrabbableObjTrans() over here at "+i+" fr fr");
                codes.Insert(i, 
                    new CodeInstruction(
                        OpCodes.Ldloc_S, // load to stack from local variable at index of
                        tfIndexInLocalVariablesIsHidingMyGotDangComponent // this
                    )
                );
                codes.Insert(i + 1, 
                    new CodeInstruction(
                        OpCodes.Call, 
                        AccessTools.Method(
                            typeof(LoadShipGrabbableItemsPatch),
                            "UpdateGrabbableObjTrans"
                        )
                    )
                );
            }

            return codes.AsEnumerable();
        }

        public static void UpdateGrabbableObjTrans(GrabbableObject component) {
            Debug.Log("ayo this fucker just got injected lmao, also " + component.name + " says go fuck yourself");

            // todo: check if this name of obj has like, been moved already?
            if (Plugin.Instance.VivsTranses.ContainsKey(component.name)) { // if name in config, overwrite trans
                VivsTrans savedTrans = Plugin.Instance.VivsTranses[component.name];
                component.transform.position = savedTrans.position;
                component.transform.rotation = savedTrans.rotation;
            }
        }
    }
}