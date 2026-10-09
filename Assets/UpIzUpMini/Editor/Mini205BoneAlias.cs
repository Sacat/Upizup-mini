using System;
using System.Linq;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-205: maps the Mixamo-style bone names the wardrobe generators use onto the Character Creator (CC_Base_*) rig of the bare bodies.</summary>
    public static partial class Mini166Repair
    {
        /// <summary>Mixamo suffix (e.g. LeftForeArm) to CC_Base bone name, or null when there is no direct equivalent.</summary>
        static string CcAlias(string n)
        {
            switch (n)
            {
                case "Hips": return "CC_Base_Hip"; case "Spine": return "CC_Base_Waist"; case "Spine1": return "CC_Base_Spine01"; case "Spine2": return "CC_Base_Spine02";
                case "Neck": return "CC_Base_NeckTwist01"; case "Head": case "HeadTop_End": return "CC_Base_Head";
            }
            string side = n.StartsWith("Left") ? "L" : n.StartsWith("Right") ? "R" : null; if (side == null) return null;
            string r = n.Substring(side == "L" ? 4 : 5);
            switch (r)
            {
                case "Shoulder": return "CC_Base_" + side + "_Clavicle"; case "Arm": return "CC_Base_" + side + "_Upperarm"; case "ForeArm": return "CC_Base_" + side + "_Forearm";
                case "Hand": return "CC_Base_" + side + "_Hand"; case "UpLeg": return "CC_Base_" + side + "_Thigh"; case "Leg": return "CC_Base_" + side + "_Calf";
                case "Foot": return "CC_Base_" + side + "_Foot"; case "ToeBase": case "Toe_End": return "CC_Base_" + side + "_ToeBase";
            }
            if (r.StartsWith("Hand") && r.Length == 9 + 0 || r.StartsWith("Hand"))
            {
                string f = r.Substring(4); int digit = f.Last() - '0'; if (digit < 1) return null; digit = Mathf.Min(digit, 3); string name = f.Substring(0, f.Length - 1);
                string cc = name == "Thumb" ? "Thumb" : name == "Index" ? "Index" : name == "Middle" ? "Mid" : name == "Ring" ? "Ring" : name == "Pinky" ? "Pinky" : null;
                return cc == null ? null : "CC_Base_" + side + "_" + cc + digit;
            }
            return null;
        }

        /// <summary>Index of the bone in target that corresponds to an old-rig bone: same name, else the CC alias, else the nearest aliased ancestor.</summary>
        static int FindTargetBone(Surface target, Transform b)
        {
            for (var t = b; t != null; t = t.parent)
            {
                string n = t.name.Split(':').Last();
                int i = Array.FindIndex(target.bones, x => x && x.name.Split(':').Last() == n); if (i >= 0) return i;
                string cc = CcAlias(n); if (cc != null) { i = Array.FindIndex(target.bones, x => x && x.name == cc); if (i >= 0) return i; }
            }
            return -1;
        }
    }
}
