using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PersistentShipObjects.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

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
       
        public static string ObjTransformsPath = Application.persistentDataPath + "/PersistentShipObjects/Objects.json";
        public static string ObjTransformsFolderPath = Application.persistentDataPath + "/PersistentShipObjects";

        public static ConfigEntry<bool> doDebugPrints;

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

            if (!File.Exists(ObjTransformsFolderPath)) Directory.CreateDirectory(ObjTransformsFolderPath);
            if (!File.Exists(ObjTransformsPath)) File.Create(ObjTransformsPath).Dispose();
            TransformObjectsManager = ReadJSON();

            //Config.Save();
            
            Harmony.CreateAndPatchAll(typeof(ShipBuildModeManagerPatch));
        }

        public static void SaveObjTransforms() {
            var str = JsonConvert.SerializeObject(TransformObjectsManager);
            //Debug.Log("JSON: ");
            //Debug.Log(str);
            File.WriteAllText(ObjTransformsPath, str);
        }
        
        public static TransformObject FindObjectIfExists(int unlockableID) {
            return TransformObjectsManager.FirstOrDefault(obj => obj.unlockableID == unlockableID);
        }

        public static void UpdateObjectManager(TransformObject newObj) {
            if (newObj == null) return;

            // remove TransformObject if exists, before adding
            TransformObject oldObjIfExists = FindObjectIfExists(newObj.unlockableID);
            if (oldObjIfExists != null) TransformObjectsManager.Remove(oldObjIfExists); 

            TransformObjectsManager.Add(newObj);
            SaveObjTransforms();
        }

        // Inits ObjectManager from file
        public static List<TransformObject> ReadJSON() {
            Console.WriteLine("entering ReadJSON()");

            try {
                string json = File.ReadAllText(ObjTransformsPath) ?? null;

                if (json == null) goto ReadJsonEarlyReturn;

                Console.WriteLine("    json:\n" + json);

                List<TransformObject> objs = JsonConvert.DeserializeObject<List<TransformObject>>(json);

                Console.WriteLine("    length of objs: " + objs.Count);
                foreach (TransformObject obj in objs) {
                    Console.WriteLine("        object found!");
                    Console.WriteLine("        named: " + obj.unlockableName);
                    //TransformObjectsManager.Add(thisObj);
                }
                return objs;      
            } catch (Exception ex) { 
                Debug.Log(ex);
            }
            ReadJsonEarlyReturn:    
            return new List<TransformObject> { };
        }


        public static void DebugLog(System.Object logMessage, char logType = 'i') {
            if (doDebugPrints?.Value == false) return; 
            if (logMessage == null) {
                Console.WriteLine("Error: logMessage is null");
                return;
            }

            String LogString = logMessage.ToString();
            if (LogString == null) {
                Console.WriteLine("Error: LogString is null");
                return;
            }

            Console.BackgroundColor = ConsoleColor.DarkGray;
            try {
                string fullMessage = "PersistentShipObjects: " + LogString;
                switch (logType) {
                    case 'w':
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(fullMessage);
                        break;
                    case 'm':
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(fullMessage);
                        break;
                    case 'e':
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(fullMessage);
                        break;
                    case 'i':
                        Console.WriteLine(fullMessage);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("DebugLogger received an invalid log type, but here's whatever this is:");
                        DebugLog("    " + LogString, 'w');
                        break;
                }
                Console.ResetColor();
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }


        public static void DebugPrintDescendantsWrapper(Transform parent) {
            if (doDebugPrints.Value == false) return;
            if (!parent.gameObject) return; //todo" err

            if (parent.parent) {
                DebugLog("My parent: " + parent.parent.gameObject.name + " at pos " + parent.parent.position, 'w');
            }
            DebugPrintDescendants(parent, "");
        }


        // I wonder if there's a way to pass indentMinusOne as a ref to avoid making 500 copies of it                                                               -viv
        static void DebugPrintDescendants(Transform parent, string indentMinusOne) {
            //return; // for why borked?? it's untouched since the last time it was working- wuh-

            /*String indentMinusOne = "";                 // leaving O(n^2) code here as a reminder to not concat strings like this.
            for (int i = 0; i < depth; i++) {           // Shouldn't matter without deep nesting, which is now Concerning(tm) anyway
                indentMinusOne += TAB;                  // but also this function is recursive as hell, soooooooo- try                                              -viv
            }//*/

            indentMinusOne += TAB;
            if (indentMinusOne.Length > A_CONCERNING_AMOUNT_OF_NESTING * TAB.Length) {
                Console.WriteLine("DebugPrintDescendants: depth is " + (indentMinusOne.Length / 4).ToString() + ", which is probably very wrong. If this is a mod conflict, got dang they are nesting too hard. Have probably a comical amount of error:");
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                Debug.LogError(stackTrace);
                return;
            }

            DebugLog(parent.gameObject, 'w');
            DebugLog(parent.gameObject.GetType(), 'w');
            DebugLog(parent.gameObject.name + " at pos " + parent.position, 'w');

            DebugLog(indentMinusOne + "P " + parent.gameObject.GetType() + " named " + parent.name + " at pos " + parent.position + "-------------------- P of " + parent.childCount, 'w');

            foreach (Transform child in parent) {
                DebugLog(indentMinusOne + TAB + child.GetType() + " named " + child.name);

                if (child.childCount > 0) {
                    DebugPrintDescendants(child, indentMinusOne);
                }
            }
        }
    }
}