# Codex Handoff — Up Iz Up Mini

Written 2026-08-15 at the user's request so Codex (or any other agent) can
continue without re-deriving what has already been established. Read
`AGENTS.md`, `PROJECT-HANDOFF.md` and `TASKS.md` first; this file adds the
hard-won context that is easy to get wrong.

---

## 1. The animation problem, and why it is now solved

**Symptom the user reported:** characters walked/ran with "spaghetti legs"
in Mini, while the same animations look correct in the larger project at
`E:\Unity\Up iz up`.

**Root cause, measured (not guessed):**

| Model | Human bones mapped | Skeleton bones | Source |
|---|---|---|---|
| `Floreswa/Models/male0*.fbx` | **23** | 39 | authored **Generic**, force-converted to Humanoid in MINI-013 |
| `Mainchar.fbx` / `Strong.fbx` | **52** | 67–72 | authored **for Humanoid** (larger project) |

The Floreswa rigs mapped barely the minimum required Humanoid bones and
Unity auto-estimated their rest pose from a model never authored in a
T-pose. Retargeting the shared locomotion clips against that sparse,
mis-estimated avatar is what distorted the limbs. The bone mapping was
*complete and valid* — which is why an earlier "avatar is valid" check was
not sufficient evidence. Use `Mini016AvatarDiagnostic` to re-check any new
character: it prints bone counts, not just validity.

**Fix applied:** Franki uses `Mainchar.fbx`, Sacat uses `Strong.fbx`,
copied with the user's explicit approval into
`Assets/UpIzUpMini/Art/Characters/`.

**Related, found in MINI-017:** the Floreswa NPC models also imported at
**2.73-2.83m tall** against the protagonists' ~1.95m and a 2m
`CharacterController`. `Mini017NpcScaleFix` normalises them to 1.85m via
`ModelImporter.globalScale`. Use `Mini017ScaleProbe` to check any new
character - a model taller than its collision capsule cannot have its
feet meet the ground.

**If you add more characters:** prefer models authored for Humanoid.
Force-converting a Generic rig will reintroduce this bug.

## 2. Locomotion speeds are not arbitrary

`Assets/UpIzUpMini/Art/Animations/*` came from the larger project and are
**in-place clips with no root motion** (measured planar speed ≈ 0.00 m/s).
Their correct speeds therefore cannot be read from the clips — they come
from the larger project's `ThirdPersonController.cs`:

```
MoveSpeed   = 2.0     SprintSpeed = 5.335
JumpHeight  = 1.2     Gravity     = -15.0
```

`PlayerController`, `FollowController` and `PatrolNPC` all drive the
animator's `Speed` parameter in **real m/s**, and the blend tree
thresholds are `0 / 2.0 / 5.335`. **Do not change one without the other**
— mismatching them is what makes feet skate.

## 3. How the scene is built

`GrandBayProof.unity` is **generated**, not hand-authored. Everything is
built by `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.BuildScene`
(menu: *Up Iz Up Mini → MINI-011 → Phase B - Build Grand Bay Environment*).

**Editing the scene by hand will be overwritten the next time that runs.**
Change the builder instead. This follows D-004 (repeatable editor setup
over hand-edited scene YAML).

Useful editor entry points:

| Menu | Purpose |
|---|---|
| MINI-011 → Phase B - Build Grand Bay Environment | rebuild the whole scene |
| MINI-016 → Diagnose Character Avatars | bone-count check (see §1) |
| MINI-016 → Import Franki and Sacat | re-import the protagonist models |
| MINI-016 → Cap Character Texture Budget | re-apply mobile texture caps |
| MINI-014 → Prepare Locomotion Clips | re-import + measure animation clips |
| MINI-012 → Build Decimated Crop Meshes | regenerate crop meshes (see §4) |
| MINI-011/012/014/016 → Snapshot … | render a PNG to `Logs/Snapshots/` |

## 4. Crop meshes are decimated on purpose

The larger project's `Assets/Imported Plants/*.fbx` are **2,000,000-triangle
photogrammetry scans, ~183MB each**, single submesh, no growth stages —
about 13× a whole mobile scene budget for one plant. That is why the larger
project imported but never referenced them.

`Editor/MeshDecimator.cs` (vertex-clustering) reduces them to ~3,300 and
~5,600 triangles, preserving silhouette. Only the decimated `.asset`
meshes are committed (282KB); the source FBXs were deleted after
conversion. Re-run `Mini012CropAssetBuilder` if you need to change the
target resolution — you will need to re-copy the sources first.

Two gotchas already hit and fixed:
- The scans are centred on their origin, so a mesh must be offset by
  `-bounds.min.y * scale` or half the plant sits underground.
- They are authored ~2cm tall; normalize to real plant height.

## 5. Verification workflow that actually works here

Play Mode in batch mode **hangs** in this environment (a Unity Editor
Search-module bug — `SearchDatabase.GetDefaultSearchDatabase` throws during
`IndexationOnStartup`). Do not rely on it. What does work:

1. `Unity.exe -batchmode -nographics -quit -projectPath … -logFile …` → compile check.
2. `-executeMethod …Mini011PhaseBSetup.BuildScene` → rebuild scene.
3. `-executeMethod …Mini011AssetSnapshot.<Snapshot*>` (**without** `-nographics`) → renders a real camera frame to `Logs/Snapshots/*.png`. This is the only reliable way to *see* the game without a human.
4. Build the player, then run the built exe with `-batchmode -nographics -logFile` for ~12s and grep the log for `Error|Exception`. This exercises real `Awake/Start/Update` without the Editor bug.

**Animators do not evaluate outside Play mode.** To inspect a pose in a
snapshot you must call `clip.SampleAnimation(go, t)` explicitly, otherwise
you render the bind pose and learn nothing. `Mini011AssetSnapshot`
already does this.

**Screen Space - Overlay canvases do not render to a RenderTexture.** The
snapshot helpers temporarily switch canvases to `ScreenSpaceCamera` to
capture UI; the saved scene is unaffected.

## 6. Current state

Working: two switchable protagonists (Tab), mouse-look camera (yaw+pitch),
jump, swim, 8 farm plots + 3 land-gated plots, 4-stage crop growth with
green→red ripening, seeds and cloning (R), farm shop / apparel shop /
produce buyer, Boss K, patrolling police that react to heat, police
reinforcements, 5 missions with GTA-style arrow + objective card, HUD,
pause menu, H controls, area names, street signs, F5/F9 save-load,
windowed mode (F11).

## 7. Known outstanding work

Ordered roughly by the user's stated priority:

1. **Vehicles and driving** — explicitly wanted, explicitly deferred by the user until after the current pass. Also bikes, physics, road mechanics. `Vehicle Physics Pro - Community Edition` is already in the user's Asset Store account.
2. ~~Separate shopfronts~~ — **done in MINI-017.** Land now sells from a "Land and Surveys" office and vehicles/boats from a "Car Dealer", each with its own NPC, stall, sign and stock.
3. ~~Guadeloupe sea trade~~ — **done in MINI-017** (`GuadeloupeTrade`): $500 fee, the non-controlled character sails with the crop inventory and returns after ~90s with 3x value, and is locked out of switching while away. Trip length and multiplier are unbalanced first-pass values.
4. **Wearables** — cap/shades/chain/watch attach to bones and work; **shirts, shorts and shoes do not**, as they need material or mesh swaps rather than bone attachment.
5. ~~Building colliders too coarse~~ — **improved in MINI-017**: per-renderer boxes from local mesh bounds, trimmed 8% horizontally, instead of one box around the whole instance. Worth re-checking by hand.
6. **Harder police missions** — reinforcements spawn with heat, but no mission uses them yet and there is no active pursuit; `EscapeHeat` currently just means waiting for decay.
7. **Character texture weight** — `Art/Characters/` is ~380MB of 4K PNGs. Import settings are capped (normals 512, colour 1024) so runtime/mobile cost is fine, but the **repository** still carries the full-size sources. Consider re-encoding them down or moving to Git LFS.
8. Missions are generated in the scene builder rather than authored as assets; at 5+ missions this is worth moving to ScriptableObjects.

## 8. Ground rules that must not be broken

- `E:\Unity\Up iz up` and `E:\Assets` are **read-only reference**. Copy deliberately-chosen files only, and record them in `Docs/ASSET-REGISTER.md`.
- Claim a task in `PROJECT-HANDOFF.md` before editing, release it after.
- Never claim something works without verification evidence; distinguish clearly between "compiles and runs clean" and "actually playtested".
- Mobile/tablet is a first-class target (D-008), with WebGL a stated goal — hence PlayerPrefs saving, texture caps, and pooling.
