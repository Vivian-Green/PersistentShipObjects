using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PersistentShipObjects.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;


// todo: ask chatgpt for this lmao

// todo: launch game in post-build
//  add todo highlighting to vs bc gd
//  commit to github (continuous-) in post-build
//  copy manifest.json and README.md, fuck it, CHANGELOG.md too, from root

//  check if lethal company is running, & close it, in post-build (python or batch? whichever is easier-)

// fucking get the last edit date of the dll before copying, since apparently it doesn't fucking feel like it
// also fucking delete copied dlls after copying, and copying-to dlls before copying.




namespace PersistentShipObjects {
    [BepInDependency("evaisa.lethallib", "0.12.1")]
    [BepInPlugin(GUID, NAME, VERSION)]

    public class PersistentShipObjects : BaseUnityPlugin {
        public static ManualLogSource MLS { get; private set; }

        private const string GUID = "VivianGreen.PersistentShipObjects";
        private const string NAME = "PersistentShipObjects";
        private const string VERSION = "0.1.0";

        private readonly Harmony harmony = new Harmony(GUID);
        public static PersistentShipObjects instance;

        public static ConfigEntry<bool> doDebugPrints;
        public static ConfigEntry<int> maxSaveFrequency;

        public static Dictionary<string, Transform> ObjTransforms;
        public static Dictionary<string, Transform> LastSavedObjTransforms;
        public static string ObjTransformsPath = Application.persistentDataPath + "/PersistentShipObjects/ObjTransforms.json";

        private static DateTime lastSaveTime;
        private static bool saveIsCached = false;



        public void Awake() {
            lastSaveTime = DateTime.MinValue;

            MLS = Logger;
            MLS.LogInfo("PersistentShipObjects instantiated!");

            if (instance == null) {
                instance = this;
            }
            ObjTransforms = JsonIO.LoadFromJson<Dictionary<string, Transform>>(ObjTransformsPath); //new Dictionary<string, Transform> { { "testName", PersistentShipObjects.PosAndRotAsTransform(Vector3.zero, Quaternion.identity) } };

            doDebugPrints = Config.Bind(
                "Debug",
                "doDebugPrints",
                false,
                "should PersistentShipObjects paint the console yellow?"
            );
            maxSaveFrequency = Config.Bind(
                "General",
                "maxSaveFrequency",
                15,
                "how many seconds have to pass between saving objects?"
            );

            //*/
            //Config.Save();

            Harmony.CreateAndPatchAll(typeof(ShipBuildModeManagerPatch));
        }


        public static bool SaveObjTransform(string name, Transform trans) {
            DebugLog("saving trans of obj named: " + name, 'i');

            DebugLog("about to check if is host-", 'i');
            if (!RoundManager.Instance.NetworkManager.IsHost) {
                MLS.LogInfo("something was moved, but this isn't my ship! So I'll just pretend I didn't see that.");
                return false;
            }
            if (trans == null) {
                MLS.LogWarning("SaveObjTransform: received transform is null!");
                return false;
            }

            // ContainsKey ? set : add
            if (ObjTransforms.ContainsKey(name)) {
                ObjTransforms[name] = PosAndRotAsTransform(trans.position, trans.rotation);
            } else {
                ObjTransforms.Add(name, PosAndRotAsTransform(trans.position, trans.rotation));
            }

            JsonIO.QueueSaveToJson(ObjTransformsPath, ObjTransforms, maxSaveFrequency.Value); // not awaited-

            DebugLog("saved object transform (to memory-)", 'i');
            return true; // it *did* save to ObjTransforms- but whether or not it *saves* saves-
        }


        public static Transform PosAndRotAsTransform(Vector3 position, Quaternion rotation) {
            GameObject go = new GameObject("transformHolder");
            Transform trans = go.transform;

            trans.position = position;
            trans.rotation = rotation;
            trans.localScale = Vector3.one;
            return trans;
        }


        public static void DebugLog(System.Object logMessage, char logType = 'i') {
            if (doDebugPrints.Value != true) return; // todo: is this truthy or boolean- how does C# work again lmao. does !doDebugPrints.Value give left or right:
                                                         // true: true                  true: true
                                                         // false: false                false: true
                                                         // null: false                 null: false
            String LogString = logMessage.ToString();

            switch (logType) {
                case 'w':
                    MLS.LogWarning("PersistentShipObjects: " + LogString);
                    break;
                case 'm':
                    MLS.LogMessage("PersistentShipObjects: " + LogString);
                    break;
                case 'e':
                    MLS.LogError("PersistentShipObjects: " + LogString);
                    break;
                case 'i':
                    MLS.LogInfo("PersistentShipObjects: " + LogString);
                    break;
                default:
                    MLS.LogWarning("DebugLogger received an invalid log type, but here's whatever this is:");
                    DebugLog("    " + LogString, 'w');
                    break;
            }
        }

    }
}








// todo: dumpster dive through this hot garbage and see if anything is worth keeping

/*
public class LoadShipGrabbableItemsPatch {
    internal ManualLogSource MLS;
    void Awake() {
        MLS.LogInfoError("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDdd");
        MLS = PersistentShipObjects.instance.MLS;
        MLS.LogInfoInfo("LoadShipGrabbableItemsPatch is waking up");
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

        DebugLog("this motherfucker is straight TRANSpiling which is poggies");
        for (int i = 0; i < codes.Count; i++) {
            DebugLog(codes[i].ExtractLabels());

            if (!(codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("Spawn"))) continue;
            // just before component.NetworkObject.Spawn();

            DebugLog("yo bitch fucker, i is "+i+" when shit gets insertamalated, we injecting UpdateGrabbableObjTrans() over here at "+i+" fr fr");
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
        DebugLog("ayo this fucker just got injected lmao, also " + component.name + " says go fuck yourself");

        // todo: check if this name of obj has like, been moved already?
        if (PersistentShipObjects.instance.VivsTranses.ContainsKey(component.name)) { // if name in config, overwrite trans
            VivsTrans savedTrans = PersistentShipObjects.instance.VivsTranses[component.name];
            component.transform.position = savedTrans.position;
            component.transform.rotation = savedTrans.rotation;
        }
    }
}
}//*/