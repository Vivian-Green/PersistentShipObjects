using UnityEngine;
namespace PersistentShipObjects {
    public class TransformObject // gdi gh is right
    {
        public AltVector3 pos;
        public AltVector3 rot;
        public int unlockableID;
        public string unlockableName;

        public TransformObject(Vector3 newPosition, Vector3 newRotation, int newUnlockableID, string newUnlockableName) {
            pos = new AltVector3(newPosition);
            rot = new AltVector3(newRotation);
            unlockableID = newUnlockableID;
            unlockableName = newUnlockableName;
        }

        public Vector3 getPos() {
            return pos.GetVector3();
        }

        public Vector3 getRotation() {
            return rot.GetVector3();
        }
     }

    public class AltVector3 {
        public float x;
        public float y;
        public float z;
        public AltVector3(Vector3 value) {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 GetVector3() {
            return new Vector3(x, y, z);
        }
    }
}