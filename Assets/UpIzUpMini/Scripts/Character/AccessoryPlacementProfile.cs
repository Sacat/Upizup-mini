using System;
using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>User-authored, rig-specific accessory transform. Once the
    /// user approves a profile, agents must not recalculate it.</summary>
    [CreateAssetMenu(menuName = "Up Iz Up Mini/Accessory Placement Profile")]
    public class AccessoryPlacementProfile : ScriptableObject
    {
        public bool useManualPlacement;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;

        [Tooltip("Exact child transforms from the user-approved fitted accessory. This preserves edits made below the accessory root during manual placement.")]
        public List<ChildTransformPose> fittedChildren = new List<ChildTransformPose>();

        [Serializable]
        public class ChildTransformPose
        {
            public string relativePath;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
        }
    }
}
