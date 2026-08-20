using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-067 hand-tuning. The player's chain is attached at RUNTIME by
    /// CharacterEquipment (it is economy-gated), so there is normally nothing
    /// in the scene to grab in the Editor. PlaceForTuning drops a real one
    /// onto the player's chest bone so it can be moved by hand; ReadTuned
    /// then converts wherever it ended up back into the numbers the runtime
    /// actually uses.
    ///
    /// The offsets are read in the CHARACTER's frame (forward/up/side),
    /// never the bone's local axes - this rig's bone rest orientations are
    /// not world-aligned, so bone-local numbers would not survive being
    /// applied to a different character.
    /// </summary>
    public static class Mini067ChainTuning
    {
        private const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string ChainPrefab = "Assets/UpIzUpMini/Art/Accessories/GoldChain18k.prefab";
        public const string TuningName = "CHAIN_TUNE";

        [MenuItem("Up Iz Up Mini/MINI-067/1. Place Chain For Tuning")]
        public static void PlaceForTuning()
        {
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChainPrefab);
            if (prefab == null) { Debug.LogError("MINI-067: chain prefab missing."); return; }

            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-067: player 'Sacat' not found in the scene."); return; }

            var anim = player.GetComponentInChildren<Animator>();
            var chest = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Chest) : null;
            if (chest == null) { Debug.LogError("MINI-067: no humanoid Chest bone on the player."); return; }

            var existing = GameObject.Find(TuningName);
            if (existing != null) Object.DestroyImmediate(existing);

            var worn = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            worn.name = TuningName;
            worn.transform.SetParent(chest, worldPositionStays: true);
            worn.transform.rotation = player.transform.rotation
                * Quaternion.Euler(CharacterEquipment.ChainTilt, 0f, 0f);
            worn.transform.position = chest.position
                + player.transform.forward * CharacterEquipment.ChainForward
                + player.transform.up * CharacterEquipment.ChainUp;

            Selection.activeGameObject = worn;
            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveScene(player.scene);

            Debug.Log("MINI-067 TUNING READY: '" + TuningName + "' is on Sacat's chest bone and selected. " +
                      "Move/rotate/scale it in the Scene view, SAVE THE SCENE, then run '2. Read Tuned Chain'. " +
                      "Boss C's own chain is the 'BossChain_18k' object and can be adjusted the same way. " +
                      "Do NOT rebuild the Grand Bay scene before reading, or the tuning is overwritten.");
        }

        [MenuItem("Up Iz Up Mini/MINI-067/2. Read Tuned Chain")]
        public static void ReadTuned()
        {
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            // Each character is reported INDEPENDENTLY. An earlier version bailed
            // out entirely when the player's tuning object was absent, which meant
            // a scene rebuild (which removes it) also silently threw away the
            // ability to read Boss C - whose numbers were the ones that mattered.
            var player = GameObject.Find("Sacat");
            var tuned = GameObject.Find(TuningName);
            if (player != null && tuned != null)
            {
                var anim = player.GetComponentInChildren<Animator>();
                var chest = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Chest) : null;
                if (chest != null) Report("PLAYER (Sacat)", player.transform, chest, tuned.transform);
                else Debug.LogWarning("MINI-067: no humanoid Chest bone on the player.");
            }
            else
            {
                Debug.LogWarning("MINI-067: no '" + TuningName + "' in the scene - skipping the player. " +
                                 "Run '1. Place Chain For Tuning' if you want to retune him.");
            }

            // Search WITHIN Boss C, not the whole scene: Boss J wears one of these
            // too, GameObject.Find returns whichever was built first, and measuring
            // Boss J's chain against Boss C's chest reported a 4.2m forward offset.
            var boss = GameObject.Find("NPC_BossC");
            GameObject bossChain = null;
            if (boss != null)
            {
                foreach (var t in boss.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != "BossChain_18k") continue;
                    bossChain = t.gameObject;
                    break;
                }
            }
            if (boss != null && bossChain != null)
            {
                var bAnim = boss.GetComponentInChildren<Animator>();
                var bChest = bAnim != null && bAnim.isHuman ? bAnim.GetBoneTransform(HumanBodyBones.Chest) : null;
                if (bChest != null) Report("BOSS C", boss.transform, bChest, bossChain.transform);
            }
        }

        /// <summary>
        /// Converts a hand-placed chain back into character-frame numbers, and
        /// prints them ready to paste. Scale is reported as the width the
        /// prefab builder should target, so a hand-scaled chain is baked into
        /// the asset rather than left as a per-scene transform tweak.
        /// </summary>
        private static void Report(string label, Transform root, Transform chest, Transform chain)
        {
            Vector3 delta = chain.position - chest.position;
            float fwd = Vector3.Dot(delta, root.forward);
            float up = Vector3.Dot(delta, root.up);
            float side = Vector3.Dot(delta, root.right);

            Quaternion local = Quaternion.Inverse(root.rotation) * chain.rotation;
            Vector3 e = local.eulerAngles;
            float tilt = e.x > 180f ? e.x - 360f : e.x;
            float yaw = e.y > 180f ? e.y - 360f : e.y;
            float roll = e.z > 180f ? e.z - 360f : e.z;

            // Measured off the MESH's local bounds times world scale, not
            // Renderer.bounds. Renderer.bounds is world-axis-aligned, so for a
            // rotated chain its X extent is the chain's THICKNESS - which is how
            // Boss C's chain came out reported at 0.068m and got "corrected" to
            // four times the right size.
            float width = 0f;
            var mf = chain.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                width = mf.sharedMesh.bounds.size.x * mf.transform.lossyScale.x;
            }

            Debug.Log(
                "MINI-067 TUNED [" + label + "]\n" +
                "  ChainForward = " + fwd.ToString("F3") + "f;\n" +
                "  ChainUp      = " + up.ToString("F3") + "f;\n" +
                "  ChainSide    = " + side.ToString("F3") + "f;\n" +
                "  ChainTilt    = " + tilt.ToString("F1") + "f;   // yaw " + yaw.ToString("F1") +
                    ", roll " + roll.ToString("F1") + " (non-zero means you rotated off-axis)\n" +
                "  measured world width = " + width.ToString("F3") + "m  (TargetWidthM in Mini067GoldChainPrep)\n" +
                "  chain localScale = " + chain.localScale);
        }
    }
}
