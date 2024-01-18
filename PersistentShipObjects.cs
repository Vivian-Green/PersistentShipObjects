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
using LethalCompanyTemplate;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using DunGen.Tags;

// todo: ask chatgpt for this lmao

// todo: launch game in post-build
//  add todo highlighting to vs bc gd
//  commit to github (continuous-) in post-build
//  copy manifest.json and README.md, fuck it, CHANGELOG.md too, from root

//  check if lethal company is running, & close it, in post-build (python or batch? whichever is easier-)

// fucking get the last edit date of the dll before copying, since apparently it doesn't fucking feel like it
// also fucking delete copied dlls after copying, and copying-to dlls before copying.




namespace PersistentShipObjects {
    [BepInPlugin(GUID, NAME, VERSION)]

    public class PersistentShipObjects : BaseUnityPlugin {
        private const string GUID = "VivianGreen.PersistentShipObjects";
        private const string NAME = "PersistentShipObjects";
        private const string VERSION = "0.2.0";

        public static ManualLogSource MLS { get; private set; }
        private readonly Harmony harmony = new Harmony(GUID);
        public static PersistentShipObjects instance;
       
        
        // Location for Saved Objects
        public static string ObjTransformsPath = Application.persistentDataPath + "/PersistentShipObjects/Objects.json";
        public static string ObjTransformsFolderPath = Application.persistentDataPath + "/PersistentShipObjects";

        public static ConfigEntry<bool> doDebugPrints;

        // Object Manager
        public static List<TransformObject> TransformObjectsManager; // no dict?? [megamind ascii but without fucking up export.py]

        const int A_CONCERNING_AMOUNT_OF_NESTING = 30; // arbitrary magic number for DebugPrintDescendants. a transform having a 
                                                       // great great great great great great great great great great great great great great great great great great great great great great great great great great great great
                                                       // grandchild is.. probably wrong, if not very dumb.                                                         - viv
        const String TAB = "|   ";


        public void Awake() {

            Debug.Log(ObjTransformsPath);

            doDebugPrints = Config.Bind(
                "Debug",
                "doDebugPrints",
                false,
                "should PersistentShipObjects paint the console yellow?"
            );

            if (!File.Exists(ObjTransformsFolderPath))
            {
                Directory.CreateDirectory(ObjTransformsFolderPath);
            }

            if (!File.Exists(ObjTransformsPath))
            {
                File.Create(ObjTransformsPath).Dispose();
            }

            TransformObjectsManager = new List<TransformObject>();
            ReadJSON();

            //Config.Save();
            
            Harmony.CreateAndPatchAll(typeof(ShipBuildModeManagerPatch));
        }

        // Write to file, should be called every time an update occurs
        // Updates occur on placement editing
        public static void SaveObjTransforms() {
            var str = JsonConvert.SerializeObject(TransformObjectsManager);
            Debug.Log("JSON: ");
            Debug.Log(str);
            File.WriteAllText(ObjTransformsPath, str);
        }
        
        // Finds object in manager via UnlockableID searching ObjectManager
        public static TransformObject FindObjectIfExists(int unlockableID)
        {
            foreach(TransformObject obj in TransformObjectsManager)
            {
                if (obj.unlockableID == unlockableID) 
                {
                    return obj;
                }
            }
            return null;
        }

        // Updates ObjectsManager with each update
        public static void UpdateObjectManager(TransformObject newObj)
        {
            if (newObj == null)
            {
                Debug.Log("NULL OBJECT??");
                return;
            }

            foreach(TransformObject obj in TransformObjectsManager)
            {
                if (!TransformObjectsManager.Any())
                {
                    TransformObjectsManager.Add(newObj);
                    break;
                }

                if (obj.unlockableID == newObj.unlockableID)
                {
                    TransformObjectsManager.Remove(obj);
                    break;
                }
            }

            TransformObjectsManager.Add(newObj);
            SaveObjTransforms();
        }

        // Inits ObjectManager from file
        public static void ReadJSON()
        {
            DebugLog("entering ReadJSON()", 'w');
            string json = File.ReadAllText(ObjTransformsPath);
            DebugLog("    json:\n"+json, 'w');

            List<TransformObject> objs = JsonConvert.DeserializeObject<List<TransformObject>>(json);

            DebugLog("    length of objs: " + objs.Count, 'w');
            foreach (TransformObject obj in objs)
            {
                DebugLog("        object found!", 'w');
                DebugLog("        named: " + obj.unlockableName, 'w');
                TransformObjectsManager.Add(obj);
            }
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



        public static void DebugPrintDescendantsWrapper(Transform parent) {
            if (doDebugPrints.Value == false) return;
            DebugPrintDescendants(parent, "");
        }


        // I wonder if there's a way to pass indentMinusOne as a ref to avoid making 500 copies of it                                                               -viv
        static void DebugPrintDescendants(Transform parent, string indentMinusOne) {
            return; // for why borked?? it's untouched since the last time it was working- wuh-
            /*String indentMinusOne = "";                 // leaving O(n^2) code here as a reminder to not concat strings like this.
            for (int i = 0; i < depth; i++) {           // Shouldn't matter without deep nesting, which is now Concerning(tm) anyway
                indentMinusOne += TAB;                  // but also this function is recursive as hell, soooooooo- try                                              -viv
            }//*/

            indentMinusOne += TAB;
            if (indentMinusOne.Length > A_CONCERNING_AMOUNT_OF_NESTING * TAB.Length) {
                Debug.LogWarning("DebugPrintDescendants: depth is " + (indentMinusOne.Length / 4).ToString() + ", which is probably very wrong. If this is a mod conflict, got dang they are nesting too hard. Have probably a comical amount of error:");
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                Debug.LogError(stackTrace);
                return;
            }

            DebugLog(parent.gameObject, 'w');
            DebugLog(parent.gameObject.GetType(), 'w');
            DebugLog(parent.gameObject.name, 'w');

            DebugLog(indentMinusOne + "P " + parent.gameObject.GetType() + " named " + parent.name + "-------------------- P of " + parent.childCount, 'w');

            foreach (Transform child in parent) {
                DebugLog(indentMinusOne + TAB + child.GetType() + " named " + child.name);

                if (child.childCount > 0) {
                    DebugPrintDescendants(child, indentMinusOne);
                }
            }
        }
    }
}