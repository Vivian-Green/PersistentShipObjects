using System;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace PersistentShipObjects {
    public static class JsonIO {
        private static DateTime lastSaveTime;
        private static bool saveIsCached = false;

        private static void SaveToJson<T>(string jsonPath, T saveableThing) {
            PersistentShipObjects.DebugLog("saving to json at path " + jsonPath, 'w');

            string thisJson = JsonConvert.SerializeObject(saveableThing);
            File.WriteAllText(jsonPath, thisJson);
            lastSaveTime = DateTime.Now;
        }

        private static bool IsTimeToSave(int maxSaveFrequency) {
            TimeSpan timeSinceLastSave = DateTime.Now - lastSaveTime;
            return timeSinceLastSave.TotalSeconds >= maxSaveFrequency;
        }

        public static async Task QueueSaveToJson<T>(string jsonPath, T saveableThing, int maxSaveFrequency) {
            if (saveIsCached) {
                String secondsToSaveStr = (
                    maxSaveFrequency - Math.Round((DateTime.Now - lastSaveTime).TotalSeconds, 2)
                ).ToString();

                Debug.Log("Went to save ship object transforms - but I was already going to in " + secondsToSaveStr + " seconds!");
                return;
            } else {
                Debug.Log("attempting to save");
            }
            
            if (!IsTimeToSave(maxSaveFrequency)) return;
            saveIsCached = true;

            PersistentShipObjects.DebugLog("It's been < " + maxSaveFrequency.ToString() + " seconds since the last save- queuing", 'w');

            await Task.Run(async () =>
            {
                double secondsToSave = (
                     maxSaveFrequency - Math.Round((DateTime.Now - lastSaveTime).TotalSeconds, 2)
                 );
                Debug.LogWarning("PersistentShipObjects time to queued save: " + secondsToSave);
                await Task.Delay(TimeSpan.FromSeconds(secondsToSave)); // aim to save exactly 30 seconds after the last save when one is queued
                SaveToJson(jsonPath, saveableThing);
                saveIsCached = false;
            });
        }

        public static T LoadFromJson<T>(string jsonPath) {
            PersistentShipObjects.DebugLog("loading from json at path " + jsonPath, 'w');

            try {
                // Read the original JSON file
                string json = File.ReadAllText(jsonPath);

                // Create a backup copy with .json.bak extension
                string backupPath = jsonPath + ".bak";
                File.Copy(jsonPath, backupPath, true);

                return JsonConvert.DeserializeObject<T>(json);
            } catch (Exception ex) {
                Debug.LogError("Error loading JSON file: " + ex);
                throw;
            }
        }
    }
}
