using UnityEngine;
namespace LethalCompanyTemplate
{
    public class TransformObject
    {
        public Vector3 pos;
        public Vector3 rot;
        public int unlockableID;
        public string unlockableName;

        public TransformObject(Vector3 newPosition, Vector3 newRotation, int newUnlockableID, string newUnlockableName)
        {
            pos = newPosition;
            rot = newRotation;
            unlockableID = newUnlockableID;
            unlockableName = newUnlockableName;
        }   
    }
}