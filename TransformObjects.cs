using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft;
using Newtonsoft.Json;
using JetBrains.Annotations;
using System.ComponentModel;
using System.IO;
namespace LethalCompanyTemplate
{
    public class TransformObjects
    {
        public AltVector3 pos;
        public AltVector3 rotation;
        public int unlockableID;
        public string unlockableName;

        public TransformObjects(Vector3 newPosition, Vector3 newRotation, int unlockableID, string unlockableName)
        {
            pos = new AltVector3(newPosition);
            rotation = new AltVector3(newRotation);
            this.unlockableID = unlockableID;
            this.unlockableName = unlockableName;
        }   

        public string ObjJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public Vector3 getPos()
        {
            return pos.GetVector3();
        }

        public Vector3 getRotation()
        {
            return rotation.GetVector3();
        }
    }

    public class AltVector3
    {
        public float x;
        public float y;
        public float z;
        public AltVector3(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 GetVector3 ()
        {
            return new Vector3(x, y, z);
        }
    }
}
