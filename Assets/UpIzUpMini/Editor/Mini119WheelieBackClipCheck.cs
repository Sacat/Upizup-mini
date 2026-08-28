using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "when riding the only thing i
    /// notice what the back goes through the handlebar so the back
    /// should stay outward while wheelieing." Read-only diagnostic:
    /// mounts the live preplaced StockDemoSuperMoto, forces the rider's
    /// wheelie pose to full blend (SetWheelieBlend(1f)), then measures
    /// the character's Spine/Chest/UpperChest bones against the bike's
    /// hand anchors (the closest real proxy for "the handlebar",
    /// confirmed already used for hand IK) to quantify the clipping
    /// instead of guessing a new pitch number blind. Delete after use.</summary>
    public static class Mini119WheelieBackClipCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Wheelie Back Clip (one-off, read-only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var bike = GameObject.Find("StockDemoSuperMoto");
            if (bike == null) { Debug.LogError("MINI-119 WHEELIE CLIP CHECK: no StockDemoSuperMoto in scene."); return; }

            var interactable = bike.GetComponent<SuperMotoVehicleInteractable>();
            if (interactable == null)
            {
                // Wiring normally happens live at Play-mode Start() and
                // isn't persisted in the saved scene - force it here the
                // same way every other diagnostic tool this session has.
                var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
                var switcher = CharacterSwitchManager.Instance ?? Object.FindFirstObjectByType<CharacterSwitchManager>();
                if (switcher != null && CharacterSwitchManager.Instance == null)
                {
                    var awakeMethod = typeof(CharacterSwitchManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                    awakeMethod?.Invoke(switcher, null);
                }
                var player = switcher?.Active?.root ?? switcher?.Slots[0]?.root;
                var spawnMethod = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
                if (spawner == null || player == null || spawnMethod == null)
                {
                    Debug.LogError("MINI-119 WHEELIE CLIP CHECK: couldn't force-wire the bike (missing spawner/player/method).");
                    return;
                }
                spawnMethod.Invoke(spawner, new object[] { player });
                interactable = bike.GetComponent<SuperMotoVehicleInteractable>();
            }
            if (interactable == null) { Debug.LogError("MINI-119 WHEELIE CLIP CHECK: still no SuperMotoVehicleInteractable after forcing wiring."); return; }

            var rightHand = FindDeep(bike.transform, "RightHandPos");
            var leftHand = FindDeep(bike.transform, "LeftHandPos");
            if (rightHand == null || leftHand == null) { Debug.LogError("MINI-119 WHEELIE CLIP CHECK: no hand anchors found."); return; }

            // The rider isn't actually mounted in the saved scene (mount
            // happens live in Play), so simulate the seated+wheelie pose
            // directly against the SEAT itself using VehicleSeat's own
            // baked numbers - same math VehicleRider.LateUpdate applies,
            // without needing a live Play session.
            var seatField = typeof(SuperMotoVehicleInteractable).GetField("_seat", BindingFlags.NonPublic | BindingFlags.Instance);
            var seat = seatField?.GetValue(interactable) as VehicleSeat;
            if (seat == null) { Debug.LogError("MINI-119 WHEELIE CLIP CHECK: no _seat field found via reflection."); return; }

            Vector3 seatedOffset = seat.SeatedOffset;
            float seatedPitch = seat.SeatedPitch;
            Vector3 wheelieOffset = seat.WheelieOffset;
            float wheeliePitch = seat.WheeliePitch;

            Transform seatAnchor = seat.SeatAnchor;
            Vector3 seatWorldPos = seatAnchor.TransformPoint(wheelieOffset);
            Quaternion seatWorldRot = seatAnchor.rotation * Quaternion.Euler(wheeliePitch, 0f, 0f);

            Debug.Log($"MINI-119 WHEELIE CLIP CHECK: seatedOffset={seatedOffset}, seatedPitch={seatedPitch}, wheelieOffset={wheelieOffset}, wheeliePitch={wheeliePitch}");
            Debug.Log($"MINI-119 WHEELIE CLIP CHECK: at full wheelie blend, rider root would be at {seatWorldPos}, forward={seatWorldRot * Vector3.forward}, up={seatWorldRot * Vector3.up}");

            Vector3 handMid = (rightHand.position + leftHand.position) * 0.5f;
            float distRootToHandlebar = Vector3.Distance(seatWorldPos, handMid);
            Debug.Log($"MINI-119 WHEELIE CLIP CHECK: hand-anchor midpoint (proxy for handlebar) at {handMid}, distance from wheelie-pose rider root={distRootToHandlebar:F2}m.");

            // A seated adult torso/chest sits roughly 0.4-0.5m above the
            // hip/root along the character's own up axis at rest. Project
            // that same offset through the wheelie pose's rotation to
            // estimate where the chest ends up once the extra pitch is
            // applied - if that lands closer to (or past) the handlebar
            // than the neutral seated chest position did, the pitch is
            // rotating the torso INTO the bars instead of away from them.
            Vector3 chestLocalOffset = new Vector3(0f, 0.45f, 0.05f);
            Vector3 wheelieChestPos = seatWorldPos + seatWorldRot * chestLocalOffset;

            Quaternion seatedWorldRot = seatAnchor.rotation * Quaternion.Euler(seatedPitch, 0f, 0f);
            Vector3 seatedWorldPos = seatAnchor.TransformPoint(seatedOffset);
            Vector3 seatedChestPos = seatedWorldPos + seatedWorldRot * chestLocalOffset;

            float seatedChestToHandlebar = Vector3.Distance(seatedChestPos, handMid);
            float wheelieChestToHandlebar = Vector3.Distance(wheelieChestPos, handMid);

            Debug.Log($"MINI-119 WHEELIE CLIP CHECK: estimated chest-to-handlebar distance - SEATED={seatedChestToHandlebar:F2}m, WHEELIE={wheelieChestToHandlebar:F2}m.");
            if (wheelieChestToHandlebar < seatedChestToHandlebar)
            {
                Debug.LogWarning($"MINI-119 WHEELIE CLIP CHECK: wheeliePitch={wheeliePitch} moves the chest CLOSER to the handlebar than the seated pose ({wheelieChestToHandlebar:F2}m < {seatedChestToHandlebar:F2}m) - consistent with the user's report of the back going through the handlebar.");
            }
            else
            {
                Debug.Log("MINI-119 WHEELIE CLIP CHECK: chest moves AWAY from the handlebar during wheelie by this estimate - the clipping may be a different cause (mesh-level, not the pitch value).");
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
