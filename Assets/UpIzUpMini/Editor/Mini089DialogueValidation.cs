using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    public static class Mini089DialogueValidation
    {
        private const string Expected = "Keep doing your ting. I'll maybe organize you when you build up ur self";

        [MenuItem("Up Iz Up Mini/MINI-089/Validate Locked Dialogue")]
        public static void Validate()
        {
            var go = new GameObject("MINI089_LockedBossCTest");
            try
            {
                var npc = go.AddComponent<TownNPCInteractable>();
                var so = new SerializedObject(npc);
                so.FindProperty("role").enumValueIndex = (int)NpcRole.StrainBoss;
                so.ApplyModifiedPropertiesWithoutUndo();

                var method = typeof(TownNPCInteractable).GetMethod("CanUseCurrentRole",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (method == null) throw new System.InvalidOperationException("CanUseCurrentRole was not found.");

                object[] args = { null };
                bool allowed = (bool)method.Invoke(npc, args);
                string feedback = args[0] as string;
                if (allowed) throw new System.InvalidOperationException("Boss C unexpectedly passed the locked-state test.");
                if (feedback != Expected)
                    throw new System.InvalidOperationException($"MINI-089 DIALOGUE VALIDATION FAIL: expected '{Expected}' but received '{feedback}'.");

                Debug.Log($"MINI-089 DIALOGUE VALIDATION PASS: locked Boss C returned exact text: {feedback}");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
