using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace PersistentShipObjects {

    [HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObjectClientRpcPostfix")]
    public class ShipBuildModeManagerPatch {
        internal ManualLogSource mls;
        void Awake() {
            mls = PSOplugin.Instance.mls;
            mls.LogInfo("shipBuildModeManagerPatch is waking up");
            PSOplugin.Instance.harmony.PatchAll();
        }

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
            PSOplugin.Instance.SaveObjTransform(objName, objTrans);
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "SpawnUnlockable")]
    public class SpawnUnlockablePatch {
        internal ManualLogSource mls;
        void Awake() {
            mls = PSOplugin.Instance.mls;
            mls.LogInfo("SpawnUnlockablePatch is waking up");
            PSOplugin.Instance.harmony.PatchAll();
        }

        [HarmonyPostfix]
        static void SpawnUnlockable(int unlockableIndex, GameObject __result) {
            if (__result != null && (PSOplugin.Instance.VivsTranses?.ContainsKey(__result.name) ?? false)) { // if name in config
                // overwrite transform
                VivsTrans newTrans = PSOplugin.Instance.VivsTranses[__result.name];
                __result.transform.position = newTrans.position;
                __result.transform.rotation = newTrans.rotation;
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
    public class LoadShipGrabbableItemsPatch {
        internal ManualLogSource mls;
        void Awake() {
            mls = PSOplugin.Instance.mls;
            mls.LogInfo("LoadShipGrabbableItemsPatch is waking up");
            PSOplugin.Instance.harmony.PatchAll();
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            int tfIndexInLocalVariablesIsHidingMyGotDangComponent = 0;
            var codes = new List<CodeInstruction>(instructions);


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
            if (PSOplugin.Instance.VivsTranses.ContainsKey(component.name)) { // if name in config, overwrite trans
                VivsTrans savedTrans = PSOplugin.Instance.VivsTranses[component.name];
                component.transform.position = savedTrans.position;
                component.transform.rotation = savedTrans.rotation;
            }
        }
    }


    public class PSOplugin : BaseUnityPlugin {
        private const string modGUID = "VivianGreen.PersistantShipObjects";
        private const string modName = "PersistentShipObjects";
        private const string modVersion = "0.0.1";

    public static Config myConfig { get; internal set; }

        public Dictionary<string, VivsTrans>? VivsTranses;

        public static PSOplugin Instance;
        internal ManualLogSource mls;

        public readonly Harmony harmony = new Harmony(modGUID);

        public bool safeInjection = true; // fucking.. it being false just makes the transpiling slightly slower- it's a load times thing lmao, definitely default to true

        void Awake() {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("PSO says is waking up");

            if (Instance == null) {
                Instance = this;
            }
            myConfig = new(base.Config);

            while (true) {
                mls.LogInfo("lmao remove this");
            } // uncomment these

            //VivsTranses = Config.shipObjectVivTransforms.Value; // todo: this is either necessary (need to instantiate here if null - doubt) or will break things (overwrite old configs-), and I'm not 100% sure which, so I'm leaving it: ?? new Dictionary<String, VivsTrans> { { "testName", new VivsTrans(new Vector3(0, 0, 0), Quaternion.identity) } };
            //safeInjection = Config.safeInjection.Value ?? true;
        }

        public bool SaveObjTransform(string name, Transform trans) {
            return true;
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

                // Save the configuration
                while (true) {
                    mls.LogInfo("lmao remove this");
                } // uncomment those
                //Config.shipObjectVivTransforms.Value = VivsTranses;
                //Config.shipObjectVivTransforms.ConfigFile.Save();
            } catch (Exception ex) {
                // Log the exception and return false
                mls.LogError("Error saving placeable ship object transform: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}