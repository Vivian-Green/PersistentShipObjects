using System;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PersistentShipObjects {
    // todo: all references to 30 here are Config.General.whateverINamedTheMaxSaveFrequencyThere and should stop being magic-

    public static class JsonIO {
        public static DateTime lastSaveTime;
        public static bool saveIsCached = false;

        private static float[] defaultObjTransformRawVals;// = new float[] { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f}; // Vector3 and Quaternion

        private static Dictionary<string, float[]> defaultObjTransformsRaw;// = new Dictionary<string, float[]> { { "testName", defaultObjTransformRawVals } };
        private static Dictionary<string, Transform> defaultObjTransforms;// = new Dictionary<string, Transform> { { "testName", PersistentShipObjects.PosAndRotAsGOWithTransform(Vector3.zero, Quaternion.identity).transform } };

        private static Dictionary<string, float[]> raws = defaultObjTransformsRaw;
        private static string serializedDefaultObjTransforms;

        public static void Awake() {
            Debug.Log("test");
            defaultObjTransforms = new Dictionary<string, Transform> { { "testName", PersistentShipObjects.PosAndRotAsGOWithTransform(Vector3.zero, Quaternion.identity).transform } };
            GameObject trnsHldr = new GameObject();
            trnsHldr.transform.position = Vector3.zero;
            trnsHldr.transform.rotation = Quaternion.identity;
            
            defaultObjTransformRawVals = objTransTo7(trnsHldr.transform);//new float[] { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f}; // Vector3 and Quaternion
            Console.WriteLine("[{0}]", string.Join(", ", defaultObjTransformRawVals)); // can't fucking Debug.Log this but Console.Writeline works fine- aight. Thx unity.
            //Debug.Log(defaultObjTransformRawVals.ToString());
            
            serializedDefaultObjTransforms = JsonConvert.SerializeObject(defaultObjTransforms);
            defaultObjTransformsRaw = new Dictionary<string, float[]> { { "testName", defaultObjTransformRawVals } };
            
            Debug.Log("test2");
        }

        private static void SaveToJson(string jsonPath, Dictionary<string, Transform> saveableThing) {
            // todo: fix: temp: seriously don't this
            saveableThing = PersistentShipObjects.ObjTransforms;
            PersistentShipObjects.DebugLog("SaveToJson: ", 'e');
            foreach (KeyValuePair<string, Transform> kvp in saveableThing) {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }

            PersistentShipObjects.DebugLog("PersistentShipObjects: saving to json at path " + jsonPath, 'w');
            
            if (!EnsureFile(jsonPath, serializedDefaultObjTransforms)) {
                PersistentShipObjects.MLS.LogWarning("EnsureFile returned false on save!");
                return;
            }

            PersistentShipObjects.DebugLog("test");

            string thisJson = serializeAndCompress();

            PersistentShipObjects.DebugLog("test2");

            File.WriteAllText(jsonPath, thisJson);
            lastSaveTime = DateTime.Now;
        }


        public static async Task QueueSaveToJson(string jsonPath, Dictionary<string, Transform> saveableThing, int maxSaveFrequency) {
            // temp - fix
            saveableThing = PersistentShipObjects.ObjTransforms;

            PersistentShipObjects.DebugLog("entering QueueSaveToJson!", 'w'); 
            
            PersistentShipObjects.DebugLog("QueueSaveToJson: ", 'e');
            foreach (KeyValuePair<string, Transform> kvp in saveableThing) {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
                
            double secondsToSave = (
                maxSaveFrequency - Math.Round((DateTime.Now - lastSaveTime).TotalSeconds, 2)
            );
            //PersistentShipObjects.MLS.LogInfo((DateTime.Now - lastSaveTime).TotalSeconds);

            PersistentShipObjects.DebugLog(saveIsCached);
            if (saveIsCached == true) {
                PersistentShipObjects.MLS.LogInfo("Went to save ship object transforms - but I was already going to in " + secondsToSave.ToString() + " seconds!");
                return;
            } else {
                PersistentShipObjects.MLS.LogInfo("attempting to save");
            }

            secondsToSave = Math.Clamp(secondsToSave, 0, 30);

            if (secondsToSave == 0) {
                Debug.Log("0 seconds!");
                lastSaveTime = DateTime.Now;
                SaveToJson(jsonPath, saveableThing);
                saveIsCached = false;
                return;
            }

            saveIsCached = true;
            lastSaveTime = DateTime.Now;

            Debug.Log("NOT 0 seconds!");
            Debug.Log(secondsToSave);

            PersistentShipObjects.MLS.LogInfo("It's been < " + maxSaveFrequency + " seconds since the last save (" + secondsToSave + " 'til free-) queuing");
            await Task.Run(async () =>
            {
                await Task.Delay((int)(secondsToSave * 1000));
                PersistentShipObjects.DebugLog("PersistentShipObjects: time to queued save: " + secondsToSave, 'w');
                
                SaveToJson(jsonPath, saveableThing);
                saveIsCached = false;
            });
        }


        public static bool EnsureFile(string filePath, string defaultContents) {
            // Ensure dir
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath)) {
                PersistentShipObjects.MLS.LogWarning("PersistentShipObjects: couldn't find directory '" + directoryPath + "'! trying to create it..");
                try {
                    Directory.CreateDirectory(directoryPath);
                    PersistentShipObjects.MLS.LogInfo("                       created directory!");
                } catch (Exception ex) {
                    PersistentShipObjects.MLS.LogError("PersistentShipObjects: failed to create directory");
                    PersistentShipObjects.MLS.LogError(ex);
                    return false;
                }
            }

            // Ensure file
            if (!File.Exists(filePath)) {
                PersistentShipObjects.MLS.LogWarning("PersistentShipObjects: couldn't find file '" + filePath + "'! trying to create it..");
                try {
                    File.WriteAllText(filePath, defaultContents);
                    PersistentShipObjects.MLS.LogInfo("                       created file!");
                } catch (Exception ex) {
                    PersistentShipObjects.MLS.LogError("PersistentShipObjects: failed to create file");
                    PersistentShipObjects.MLS.LogError(ex);
                    return false;
                }
            }
            return true;
        }

        public static Transform objTrans7ToTrans(float[] raw) {
            Vector3 extractedPos = new Vector3(raw[0], raw[1], raw[2]);
            Quaternion extractedRot = new Quaternion(raw[3], raw[4], raw[5], raw[6]);
            return PersistentShipObjects.PosAndRotAsGOWithTransform(extractedPos, extractedRot).transform;
        }

        public static float[] objTransTo7(Transform trans) {
            Debug.Log("objTransTo7");

            Debug.Log("objTransTo7 in transform:" + trans.position + ", " + trans.rotation);
            float[] retVal = new float[7] { trans.position.x, trans.position.y, trans.position.z, trans.rotation.w, trans.rotation.x, trans.rotation.y, trans.rotation.z };
            Debug.Log("objTransTo7 out 7: [" + string.Join(", ", retVal) + "]");

            return retVal;
        }


        public static Dictionary<string, Transform> deserializeAndExtract(string json) {

            Debug.Log(JsonConvert.DeserializeObject(json).GetType());
            raws = ((JObject)JsonConvert.DeserializeObject(json)).ToObject<Dictionary<string, float[]>>(); // This is looking C++y. You pronounced that.

            Dictionary<string, Transform> extractedObjTransforms = raws.ToDictionary(
                kvp => kvp.Key,
                kvp => objTrans7ToTrans(kvp.Value)
            );

            return extractedObjTransforms;
        }

        public static string serializeAndCompress() {
            PersistentShipObjects.DebugLog("serializeAndCompress entered");
            
            PersistentShipObjects.DebugLog("serializeAndCompress: ", 'e');

            Dictionary<string, float[]> compressedObjTrans7s = new Dictionary<string, float[]>();
            PersistentShipObjects.DebugLog("ee");


            foreach (KeyValuePair<string, Transform> pair in PersistentShipObjects.ObjTransforms) {
                Console.WriteLine("Key = {0}, Value = {1}", pair.Key, pair.Value);

                // ContainsKey ? set : add
                if (compressedObjTrans7s.ContainsKey(pair.Key)) {
                    compressedObjTrans7s[pair.Key] = objTransTo7(pair.Value);
                } else {
                    compressedObjTrans7s.Add(pair.Key, objTransTo7(pair.Value));
                }
                //compressedObjTrans7s[pair.Key] = objTransTo7(pair.Value);
            }


            PersistentShipObjects.DebugLog("rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr");
            PersistentShipObjects.DebugLog(compressedObjTrans7s, 'w');
            PersistentShipObjects.DebugLog("wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww");
            string serialized = JsonConvert.SerializeObject(compressedObjTrans7s);
            PersistentShipObjects.DebugLog("serializeAndCompress exited");
            return serialized;
        }

        public static Dictionary<string, Transform> LoadFromJson(string jsonPath) {
            lastSaveTime = DateTime.MinValue;
            if (!EnsureFile(jsonPath, serializedDefaultObjTransforms)) {
                PersistentShipObjects.MLS.LogWarning("EnsureFile returned false on load!");
                return null;
            }

            PersistentShipObjects.MLS.LogInfo("PersistentShipObjects: loading from json at path " + jsonPath);
            // Read the original JSON file
            try {
                string json = File.ReadAllText(jsonPath);

                try {
                    // Create a backup copy with .json.bak extension
                    string backupPath = jsonPath + ".bak";
                    File.Copy(jsonPath, backupPath, true);
                } catch (Exception ex) {
                    PersistentShipObjects.MLS.LogWarning("PersistentShipObjects: failed to copy json to backup!");
                    PersistentShipObjects.MLS.LogError(ex);
                }

                return (Dictionary<string, Transform>)deserializeAndExtract(json); ;

            } catch (Exception ex) {
                PersistentShipObjects.MLS.LogInfo("PersistentShipObjects: creating a new default json file. If this is in error, fuck-\n                   but also try reporting this, with your log, and pulling a backup from the above path ^\n                   Have the error:");
                PersistentShipObjects.MLS.LogWarning(ex);

                // try saving if loading errs? lmao- worth a shot- old code but still maybe useful ¯\_(ツ)_/¯
                //QueueSaveToJson(jsonPath, defaultObjTransforms, 30);

                return defaultObjTransforms;
            }
        }
    }
}
