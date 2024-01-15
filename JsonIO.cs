using System;
using System.Collections.Generic;
using System.Text;

namespace PersistentShipObjects {
    using UnityEngine;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public static class JsonIO {
        private static DateTime lastSaveTime;
        private static bool saveIsCached = false;

        public static void SaveToJson<T>(string jsonPath, T saveableThing, int maxSaveFrequency) {
            if (saveIsCached || !IsTimeToSave(maxSaveFrequency)) return;

            saveIsCached = true;

            PerformSave(jsonPath, saveableThing);
        }

        private static void PerformSave<T>(string jsonPath, T saveableThing) {
            PersistentShipObjects.DebugLog("saving to json at path " + jsonPath, 'w');

            string thisJson = JsonConvert.SerializeObject(saveableThing);
            File.WriteAllText(jsonPath, thisJson);
            lastSaveTime = DateTime.Now;
        }

        private static bool IsTimeToSave(int maxSaveFrequency) {
            TimeSpan timeSinceLastSave = DateTime.Now - lastSaveTime;
            return timeSinceLastSave.TotalSeconds >= maxSaveFrequency;
        }

        public static async Task QueueSave<T>(string jsonPath, T saveableThing, int maxSaveFrequency) {
            PersistentShipObjects.DebugLog("It's been < " + maxSaveFrequency.ToString() + " seconds since the last save- queuing", 'w');
            await Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(maxSaveFrequency));
                saveIsCached = false;
                SaveToJson(jsonPath, saveableThing, maxSaveFrequency);
            });
        }

        public static T LoadFromJson<T>(string jsonPath) {
            PersistentShipObjects.DebugLog("loading from json at path " + jsonPath, 'w');
            string json = File.ReadAllText(jsonPath);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

}
