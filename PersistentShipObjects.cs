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
        private const string VERSION = "0.1.2";

        public static ManualLogSource MLS { get; private set; }
        private readonly Harmony harmony = new Harmony(GUID);
        public static PersistentShipObjects instance;
       
        
        // Location for Saved Objects
        public static string ObjTransformsPath = Application.persistentDataPath + "/PersistentShipObjects/Objects.json";
        public static string ObjTransformsFolderPath = Application.persistentDataPath + "/PersistentShipObjects";

        // Object Manager
        public static List<TransformObjects> TransformObjectsManager;

        public void Awake() {

            Debug.Log(ObjTransformsPath);

            if (!File.Exists(ObjTransformsFolderPath))
            {
                Directory.CreateDirectory(ObjTransformsFolderPath);
            }

            if (!File.Exists(ObjTransformsPath))
            {
                File.Create(ObjTransformsPath).Dispose();
            }

            TransformObjectsManager = new List<TransformObjects>();
            ReadJSON();

            //Config.Save();
            
            Harmony.CreateAndPatchAll(typeof(ShipBuildModeManagerPatch));
        }

        // Write to file, should be called every time an update occurs
        // Updates occur on placement editing
        public static void SaveObjTransform() {
            var str = JsonConvert.SerializeObject(TransformObjectsManager);
            Debug.Log("JSON: ");
            Debug.Log(str);
            File.WriteAllText(ObjTransformsPath, str);
        }
        
        // Finds object in manager via UnlockableID searching ObjectManager
        public static TransformObjects GetObjects(int unlockableID)
        {
            foreach(TransformObjects obj in TransformObjectsManager)
            {
                if (obj.unlockableID == unlockableID) 
                {
                    return obj;
                }
            }
            return null;
        }

        // Updates ObjectsManager with each update
        public static void UpdateObjectManager(TransformObjects newObj)
        {
            if (newObj == null)
            {
                Debug.Log("NULL OBJECT??");
                return;
            }

            foreach(TransformObjects obj in TransformObjectsManager)
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
            SaveObjTransform();
        }

        // Inits ObjectManager from file
        public static void ReadJSON()
        {
            try
            {
                string json = File.ReadAllText(ObjTransformsPath);
                List<TransformObjects> objs = JsonConvert.DeserializeObject<List<TransformObjects>>(json);
                foreach (TransformObjects obj in objs)
                {
                    TransformObjectsManager.Add(obj);
                }
            } catch (Exception e)
            {
                //
            }
            
        }
    }
}
