using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PersistentShipObjects.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection.Emit;
using System.ComponentModel;
using System.Collections;


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
        private const string GUID = "VivianGreen.PersistentShipObjects";
        private const string NAME = "PersistentShipObjects";
        private const string VERSION = "0.1.2";

        public static ManualLogSource MLS { get; private set; }

        private readonly Harmony harmony = new Harmony(GUID);
        public static PersistentShipObjects instance;

        public static ConfigEntry<bool> doDebugPrints;
        public static ConfigEntry<int> maxSaveFrequency;

        public static Dictionary<string, Transform> ObjTransforms;
        public static Dictionary<string, Transform> LastSavedObjTransforms;
        public static string ObjTransformsPath = Application.persistentDataPath + "/PersistentShipObjects/ObjTransforms.json";

        GameObject transformHolder = new GameObject("transformHolder");

        public void Awake() {
            JsonIO.Awake();

            MLS = Logger;
            MLS.LogInfo("PersistentShipObjects instantiated!");

            if (instance == null) {
                instance = this;
            }

            MLS.LogInfo("about to load from json");
            MLS.LogInfo(ObjTransformsPath);
            ObjTransforms = JsonIO.LoadFromJson(ObjTransformsPath);

            if (ObjTransforms == null) {
                MLS.LogError("JsonIO.LoadFromJson(ObjTransformsPath) returned null!");
            } else {
                MLS.LogInfo("loaded from json");
            }

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
                "how many seconds have to pass between saving objects? (it will just queue them in between saves to not cause lag with a lot of shipObject movements)"
            );

            //Config.Save();

            Harmony.CreateAndPatchAll(typeof(ShipBuildModeManagerPatch));
        }


        public static bool SaveObjTransform(string name, Transform trans) {
            DebugLog("saving trans of obj named: " + name);

            DebugLog("about to check if is host-");
            if (!RoundManager.Instance.NetworkManager.IsHost) {
                MLS.LogInfo("something was moved, but this isn't my ship! So I'll just pretend I didn't see that.");
                return false;
            }
            if (trans == null) {
                MLS.LogWarning("SaveObjTransform: received transform is null!");
                return false;
            }
            DebugLog("I am host and transform isn't null!");
            DebugLog(trans.position);
            DebugLog(trans.rotation);

            DebugLog("before: ", 'e');
            foreach (KeyValuePair<string, Transform> kvp in ObjTransforms) {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }

            // ContainsKey ? set : add
            if (ObjTransforms.ContainsKey(name)) {
                DebugLog("key found in ObjTransforms!");
                ObjTransforms[name] = PosAndRotAsGOWithTransform(trans.position, trans.rotation).transform;
            } else {
                DebugLog("key NOT found in ObjTransforms!");
                ObjTransforms.Add(name, PosAndRotAsGOWithTransform(trans.position, trans.rotation).transform);
            }
            DebugLog("after: ", 'e');
            foreach (KeyValuePair<string, Transform> kvp in ObjTransforms) {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }

            Task.Run(async () => {
                await Task.Delay((int)(1000));
                await JsonIO.QueueSaveToJson(ObjTransformsPath, ObjTransforms, maxSaveFrequency.Value); // not awaited-
            });

            DebugLog("saved object transform (to memory-)");
            return true; // it *did* save to ObjTransforms- but whether or not it *saves* saves-
        }


        public static GameObject PosAndRotAsGOWithTransform(Vector3 position, Quaternion rotation) {
            Debug.Log("entering PosAndRotAsGOWithTransform");
            GameObject go = new GameObject("transformHolder");
            Transform trans = go.transform;

            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one;
            return go;
        }


        public static void DebugLog(System.Object logMessage, char logType = 'i') {
            if (doDebugPrints.Value != true) return; // todo: is this truthy or boolean- how does C# work again lmao. does !doDebugPrints.Value give left or right:
                                                     // true: true                  true: true
                                                     // false: false                false: true
                                                     // null: false                 null: false
            String LogString = logMessage.ToString();

            Console.BackgroundColor = ConsoleColor.DarkGray;
            switch (logType) {
                case 'w':
                    //Console.ForegroundColor = ConsoleColor.;
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
            //Console.WriteLine("PersistentShipObjects: " + LogString);
            Console.ResetColor();
        }


























        [HarmonyPatch(typeof(StartOfRound), "SpawnUnlockable")]
        [HarmonyPostfix]
        static void Postfix_SpawnUnlockable(int unlockableIndex, GameObject gameObject, UnlockablesList unlockablesList) {
            UnlockableItem unlockableItem = unlockablesList.unlockables[unlockableIndex];

            Debug.LogWarning("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
            Debug.Log(gameObject.GetType() + " named " + gameObject.name);
            Debug.Log(gameObject.transform.parent?.gameObject.GetType() + " named " + gameObject.transform.parent?.gameObject.name);

            switch (unlockableItem.unlockableType) {
                case 0:
                    // Handle case 0 if needed
                    break;
                case 1:
                    // Handle case 1
                    if (PersistentShipObjects.ObjTransforms.ContainsKey(gameObject.name)) { // if name in config, overwrite trans
                        Transform savedTrans = PersistentShipObjects.ObjTransforms[gameObject.name];
                        gameObject.transform.position = savedTrans.position; // probably needs to be parent
                        gameObject.transform.rotation = savedTrans.rotation;
                    }
                    break;
                    // Add more cases if necessary
            }
        }






        /*
        //LoadUnlockables
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
                //DebugLog(codes[i].ExtractLabels());

                if (!(codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("Spawn"))) continue;
                // just before component.NetworkObject.Spawn();

                DebugLog("yo bitch fucker, i is " + i + " when shit gets insertamalated, we injecting UpdateGrabbableObjTrans() over here at " + i + " fr fr");
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
                            typeof(PersistentShipObjects),
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
            if (PersistentShipObjects.ObjTransforms.ContainsKey(component.name)) { // if name in config, overwrite trans
                Transform savedTrans = PersistentShipObjects.ObjTransforms[component.name];
                component.transform.position = savedTrans.position; // probably needs to be parent of
                component.transform.rotation = savedTrans.rotation;
            }
        }*/
    }
}
