using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using PersistentShipObjects.Patches;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;




// todo: ConfigEntry cannot be dict
// for now uh... persistence is just uhhhh... after dying- lmao




namespace PersistentShipObjects {
    //[BepInDependency("evaisa.lethallib", "0.12.1")]

    [BepInPlugin(GUID, NAME, VERSION)]
    public class PersistentShipObjects : BaseUnityPlugin {
        public static ManualLogSource mls { get; private set; }

        private const string GUID = "VivianGreen.PersistentShipObjects";
        private const string NAME = "PersistentShipObjects";
        private const string VERSION = "0.0.9";

        private readonly Harmony harmony = new Harmony(GUID);
        public static PersistentShipObjects instance;

        //public static GameObject ShipPrefab;
        //public static GameObject ShipNetworkerPrefab;
        //public static TerminalNode ShipFile;

        public static Dictionary<string, Transform> ObjTransforms;
        //public static Config ShipConfig;

        //public static ConfigEntry<Dictionary<string, Transform>> ShipObjectTransforms;


        public void Awake() {
            mls = Logger; 
            mls.LogInfo("PersistentShipObjects instantiated!");

            ObjTransforms = new Dictionary<string, Transform> { { "testName", PersistentShipObjects.PosAndRotAsTransform(Vector3.zero, Quaternion.identity) } };

            mls.LogInfo("AA");
            if (instance == null) {
                instance = this;
            }
            mls.LogInfo("AAAA");


            /*ShipObjectTransforms = Config.Bind(
                "General",
                "shipObjectTransforms",
                new Dictionary<string, Transform> { { "testName", PersistentShipObjects.PosAndRotAsTransform(Vector3.zero, Quaternion.identity) } },
                "Object transforms configuration"
            );//*/
            //Config.Save();

            mls.LogInfo("Configuration Initialized.");
            //Harmony.CreateAndPatchAll(GetType().Assembly);

            Harmony.CreateAndPatchAll(typeof(ShipBuildModeManagerPatch));
            mls.LogInfo("ShipBuildModeManager patched.");

            mls.LogInfo("PersistentShipObjects: harmony.PatchAll() DIDN'T immediately crash!");

            /*var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types) {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods) {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0) {
                        method.Invoke(null, null);
                    }
                }
            }*/

            // Handle configs
            //ShipConfig = new Config(Config);
        }

        public static bool SaveObjTransform(string name, ref Transform trans) {
            mls.LogWarning("saving trans of obj named: " + name);

            if (!RoundManager.Instance.NetworkManager.IsHost) {
                Debug.LogWarning("something was moved, but this isn't my ship! So I'll just pretend I didn't see that.");
                return false;
            }
            if (trans == null) {
                Debug.LogWarning("received transform is null!");
                return false;
            }

            try {
                // exists ? set : add

                if (ObjTransforms.ContainsKey(name)) {
                    ObjTransforms[name] = PosAndRotAsTransform(trans.position, trans.rotation);
                } else {
                    ObjTransforms.Add(name, PosAndRotAsTransform(trans.position, trans.rotation));
                }

                /*try {
                    ShipObjectTransforms.Value = ObjTransforms;
                    ShipObjectTransforms.ConfigFile.Save();
                } catch {
                    mls.LogInfo("FUCK!");
                }//*/

            } catch (Exception ex) {
                // Log the exception and return false
                mls.LogError("Error saving placeable ship object transform: " + ex.Message);
                return false;
            }//*/
            return true;
        }

        public static Transform PosAndRotAsTransform(Vector3 position, Quaternion rotation) {
            GameObject go = new GameObject("transformHolder");
            if (go == null) {
                Debug.LogError("Failed to create GameObject");
                return null;
            }

            Transform trans = go.transform;
            if (trans == null) {
                Debug.LogError("Failed to get transform");
                return null; // or handle the failure appropriately
            }


            trans.position = position;
            trans.rotation = rotation;
            trans.localScale = Vector3.one;
            return trans;
        }
    }
}
/*
public class LoadShipGrabbableItemsPatch {
    internal ManualLogSource mls;
    void Awake() {
        mls.LogInfoError("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDdd");
        mls = PersistentShipObjects.instance.mls;
        mls.LogInfoInfo("LoadShipGrabbableItemsPatch is waking up");
        PersistentShipObjects.instance.harmony.PatchAll();
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
        //*//*

        mls.LogInfo("this motherfucker is straight TRANSpiling which is poggies");
        for (int i = 0; i < codes.Count; i++) {
            mls.LogInfo(codes[i].ExtractLabels());

            if (!(codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("Spawn"))) continue;
            // just before component.NetworkObject.Spawn();

            mls.LogInfo("yo bitch fucker, i is "+i+" when shit gets insertamalated, we injecting UpdateGrabbableObjTrans() over here at "+i+" fr fr");
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
        mls.LogInfo("ayo this fucker just got injected lmao, also " + component.name + " says go fuck yourself");

        // todo: check if this name of obj has like, been moved already?
        if (PersistentShipObjects.instance.VivsTranses.ContainsKey(component.name)) { // if name in config, overwrite trans
            VivsTrans savedTrans = PersistentShipObjects.instance.VivsTranses[component.name];
            component.transform.position = savedTrans.position;
            component.transform.rotation = savedTrans.rotation;
        }
    }
}
}//*/