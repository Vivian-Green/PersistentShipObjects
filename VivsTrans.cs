using UnityEngine;

namespace PersistentShipObjects {
    public class VivsTrans { // I actually needed this lmao
        public Vector3 position = new Vector3(0, 0, 0);
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = new Vector3(1, 1, 1);

        public VivsTrans() { }

        public VivsTrans(Vector3 newPos, Quaternion newRot) {
            position = newPos;
            rotation = newRot;
        }

        public VivsTrans(Transform transform) {
            position = transform.position;
            rotation = transform.rotation;
        }

        public Transform AsTransform() {
            Transform trans = new GameObject("NewObject").transform;
            trans.position = position;
            trans.rotation = rotation;
            trans.localScale = scale;
            return trans;
        }
    }
}
