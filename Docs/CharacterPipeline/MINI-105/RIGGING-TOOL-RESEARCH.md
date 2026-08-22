# MINI-105 — rigging and hand-system tool decision

Date: 2026-08-21

## Decision

Use **AccuRIG 2 Free** for the first production skin bind and full five-finger skeleton, then use **Blender 5** for mesh cleanup, retopology, weight correction, modular garment fitting, UVs, and Unity export. Keep the already installed **Unity Animation Rigging 1.4.1** for wrist/arm contact targets. Use authored finger-pose clips or a lightweight bone-pose layer for finger closure; do not run individual finger IK every frame on mobile.

The failed Blender automatic-weight, nearest-surface transfer, and custom envelope experiments remain rejected. They are not suitable for gameplay integration.

## Why this stack

- AccuRIG 2 Free explicitly supports hand rigging, selectable finger count, manual finger-joint adjustment, high/low-density meshes, and FBX export to Unity and Blender.
- Blender Rigify is useful for authoring controls and finger poses but does not skin the mesh. It cannot solve the failed bind by itself.
- RetopoFlow is a useful GPL Blender retopology aid if the Hitem mesh needs manual animation loops around fingers, shoulders, elbows, knees, and crotch. It is optional; Blender's built-in QuadriFlow can provide a first remesh but does not guarantee production edge flow and discards mesh data layers.
- Unity Animation Rigging already provides the correct low-cost arm solution: a Two Bone IK target at each wrist plus elbow hints. Fingers should use reusable baked poses layered over that wrist placement.
- Unity's built-in Ragdoll Wizard is sufficient for the initial mobile ragdoll. Finger rigidbodies are deliberately excluded; fingers remain in a relaxed/open animation pose during ragdoll.

## Runtime hand contract

Each interactable defines left/right hand anchors and requests a named pose:

- `RelaxedOpen`
- `BikeGripLeft`
- `BikeGripRightThrottle`
- `BikeGripRightBrake`
- `SteeringWheelGrip`
- `CombatFist`
- `WeaponGrip`
- `EatHoldSmall`
- `CarryLarge`
- `RagdollOpen`

Arm IK places and rotates the wrist at the anchor. The hand-pose layer curls each thumb and finger independently, allowing trigger fingers or separated digits without changing the whole-body animation. Pose weights blend during mount, dismount, pickup, attack, eating, and ragdoll transitions.

## Mobile rules

- Maximum four bone influences per vertex.
- Full finger bones on the two heroes and important nearby NPCs.
- Distant/crowd NPC LODs may omit animated finger detail.
- No runtime cloth simulation for ordinary clothing.
- No per-finger physics or per-finger IK during normal gameplay.
- One canonical skeleton and bind pose for Sacat, Franki, and future same-size modular bodies.

## Tool status

- Blender 5.0.1: installed.
- Unity Animation Rigging 1.4.1: already installed in the Mini project.
- AccuRIG 2 Free: recommended, not currently installed.
- RetopoFlow: optional, not currently installed.
- Codex/ChatGPT plugins: no available plugin performs production mesh retopology or skin binding. Computer control can operate AccuRIG/Blender, but the actual 3D work remains in those applications.

## Next production gate

1. Inspect the original Hitem hands for true gaps and usable finger geometry.
2. Export an FBX/OBJ staging copy and rig it in AccuRIG 2 with all five fingers.
3. Calibrate open hand, fist, handlebar grip, and steering-wheel grip before export.
4. Import the AccuRIG FBX into Blender and inspect deformation close up.
5. Retopologize and correct weights only where the pose matrix reveals defects.
6. Show fixed-camera screenshots before any playable-character replacement.

## Primary references

- Reallusion AccuRIG: https://actorcore.reallusion.com/static-page/auto-rig/pre_page/accurig/accurig.html
- Reallusion hand-rig manual: https://manual.reallusion.com/ActorCore-AccuRIG-1/Content/ENU/1.1/07-Hand-Rig/Hand-Rig.htm
- Blender Rigify: https://docs.blender.org/manual/en/5.0/addons/rigging/rigify/introduction.html
- Blender Data Transfer: https://docs.blender.org/manual/en/4.3/modeling/modifiers/modify/data_transfer.html
- Blender retopology: https://docs.blender.org/manual/en/4.3/modeling/meshes/retopology.html
- RetopoFlow: https://github.com/CGCookie/retopoflow
- Unity Two Bone IK: https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.2/manual/constraints/TwoBoneIKConstraint.html
- Unity Humanoid configuration: https://docs.unity3d.com/Manual/ConfiguringtheAvatar.html
- Unity ragdoll physics: https://docs.unity3d.com/6000.0/Documentation/Manual/ragdoll-physics-section.html
