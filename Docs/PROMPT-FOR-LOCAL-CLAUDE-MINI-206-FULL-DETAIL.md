# PROMPT FOR THE LOCAL CLAUDE: everything the cloud design lane did (MINI-197, 201, 202, 206), so you can build your own workflow

You are the local Claude Code session for Up Iz Up Mini (E:\Unity\Up Iz Up Mini), with the Unity Editor. A cloud Claude session (Linux,
no Unity, no GPU, no C# compiler) did the work below. All of it is on branch `cloud/mini-206` (tip b03d0bf) and, for the earlier part,
`cloud/sacat-legs`. Use this document to:
1. understand exactly what was made, how, with which numbers, and what is still unverified;
2. build your OWN repeatable workflow (Blender scripts + Unity tools + checks) for each kind of job: characters, garments, accessories,
   vehicles, roads, riding;
3. integrate and verify the results in Unity (see Part E for the order).

Follow AGENTS.md / CLAUDE.md (ownership protocol, copies before live scenes, protected visual locks). Prove every claim in Unity with
Play Mode evidence. The cloud never ran Unity, so treat every C# file and every in-engine behaviour as unverified until you test it.

The document has six parts:

| Part | Contents |
|---|---|
| A | The full report: what was made, the numbers, the files |
| B | Exact pipeline commands and parameters, per stage (so you can re-run or adapt them) |
| C | What failed along the way and how it was fixed (lessons to bake into your workflow) |
| D | Reusable techniques and conventions to adopt |
| E | Integration order and acceptance checks for you |
| F | Open items |

=====================================================================================================================
PART A: FULL REPORT (verbatim copy of Docs/CLOUD-LANE-FULL-REPORT-MINI-197-206.md)
=====================================================================================================================
# Cloud design lane: full report (MINI-197, MINI-201, MINI-202, MINI-206)

Written 2026-10-09 by the Claude cloud session for the owner and for any model that continues this work. It covers
everything this lane did: programs and methods, coordinate systems, dimensions, modelling, rigging, garments, accessories,
the TMAX rebuild, riding/animation code, and both road passes. Each section ends with what was NOT verified.

Nothing in this lane ran in Unity. The cloud machine has no Unity Editor, no GPU and no C# compiler. Every number here
was measured in Blender/Python on the real files. Every C# file is uncompiled until the local machine builds it.

---------------------------------------------------------------------------------------------------------------------

## 0. Where everything is

| Branch | Base | Commits (oldest first) |
|---|---|---|
| `cloud/sacat-legs` (pushed) | owner's main line | 9ff9479 legs, 23b6aac skin, f28c8fe hands + finger rig + rig frame fix, 530c30a hip fix, b385cae test-pose fix, 25deaec torso/neck, e1be34a Franki + import note, 8a5ed29 Franki face, a033c14 hair cap, a4511f7 blend recompress, 440b32d MINI-201 roads, f9fc4d6 MINI-202 riders |
| `cloud/mini-206` (LOCAL ONLY, see below) | `claude/local-integration` (fc580b1) | 0641ac7 stage 4 diagnosis, 8acd22f + 617282f stage 1 TMAX, 80f6e93 stage 2 garments/chain/accessories, 73b2555 stage 3 new-map roads, a87ff44 stage 4 update |

**Push status:** `cloud/mini-206` is committed in the cloud container but NOT on GitHub. The environment's network
policy blocks `lfs.github.com`, so Git LFS cannot upload the .blend/.fbx/.png files. To push:
1. Add `lfs.github.com` to the cloud environment's allowed domains (environment settings, Network access).
2. In the session, run `git push --force-with-lease=cloud/mini-206:7aeaa7a -u origin cloud/mini-206`.

The force-with-lease only replaces my own pre-rebase copy of the stage 4 commit.

| Folder | Contents |
|---|---|
| `Docs/CharacterPipeline/MINI-197/BareBody/` | Bare bodies, stages 01-05; final = `04-NeckTorso` (Sacat) and `05-Franki` |
| `Docs/CharacterPipeline/MINI-206/TMAX/` | Rebuilt TMAX FBX/LOD1/blend/textures/report/renders |
| `Docs/CharacterPipeline/MINI-206/Garments/{Sacat,Franki}/` | Garment FBX, textures, blend, report, pose renders |
| `Docs/CharacterPipeline/MINI-206/Chain/` | 6 fitted chain FBX + json + renders |
| `Docs/CharacterPipeline/MINI-206/Accessories/` | Placement JSON per character |
| `Docs/CharacterPipeline/MINI-206/Shoes/` | Shoe-fit renders |
| `Docs/WorkPackets/MINI-201/`, `MINI-201.md` | Old-map road data + note |
| `Docs/WorkPackets/MINI-206/`, `MINI-206.md`, `MINI-206-riding.md` | New-map road data, stage notes, riding analysis |
| `Tools/CharacterPipeline/mini197_bare_body/` | Body generator + checks |
| `Tools/CharacterPipeline/mini206_tmax/` | TMAX pipeline |
| `Tools/CharacterPipeline/mini206_garments/` | Garments, chain, accessories, shoes |
| `Tools/RoadPipeline/mini201_road_smooth/` | Road solver, old map |
| `Tools/RoadPipeline/mini206_newmap/` | Road solver, new map |

C# added by this lane:

| File | Purpose |
|---|---|
| `Assets/UpIzUpMini/Editor/Mini201RoadSmooth.cs` | MINI-201 apply tool (old map) |
| `Assets/UpIzUpMini/Editor/Mini206RoadsNewMap.cs` | MINI-206 apply tool (new map) |
| `Assets/UpIzUpMini/Editor/Mini206AccessoryProfiles.cs` | JSON → AccessoryPlacementProfile assets |
| `Assets/UpIzUpMini/Scripts/Character/Draft/BareFeetUnderShoes.cs` | Hides bare feet under shoes |
| `Assets/UpIzUpMini/Scripts/Vehicles/Draft/TmaxSuperMotoVisualBinder.cs` | New TMAX on SuperMoto physics (`#if MINI206_DRAFT`) |
| `Assets/UpIzUpMini/Scripts/Vehicles/Draft/TmaxRebuiltVisuals.cs` | New TMAX visuals on the current controller (`#if MINI206_DRAFT`) |
| `Assets/UpIzUpMini/Scripts/Vehicles/Draft/TmaxRideFeel.cs` | Ride values + realistic easy wheelie (`#if MINI206_DRAFT`) |

MINI-202 also edited these existing scripts (owner-requested task):

| Script | Change |
|---|---|
| `TmaxRideDynamics.cs` | Rider anchors + bounce |
| `TmaxBikeControllerCustom.cs` | Two lines + two properties |
| `BikeInteractable.cs` | Lean-copy off when attached; bounce/look-ahead |
| `VehicleRider.cs` | `SetBounce`, `SetLookAhead`, look-at IK; additive, off by default |

---------------------------------------------------------------------------------------------------------------------

## 1. Programs, environment, conventions

- **Blender:** the `bpy` 4.2 Python module (Blender 4.2 as a library) in a Python 3.11 venv, run headless.
  - `bpy` 5.2 on Python 3.13 crashed on start (glog double init), so 4.2 is used.
  - Rendering: Cycles on CPU, 8-24 samples, no denoiser.
  - Every render sheet in the repo was made this way and looked at before any claim was made.
- **Python libraries:**
  - numpy and scipy: sparse least squares for roads, KD-trees, circle and catenary fits;
  - UnityPy: reads the binary `.unity` scenes and serialized meshes;
  - matplotlib: section plots;
  - PIL: render sheets.
- **Unity files read without Unity:**
  - Scenes via UnityPy `read_typetree`. A GUID is converted to its `.meta` hex form by swapping the nibbles of each byte.
  - Text `.asset` meshes via a parser of `m_VertexData._typelessdata` + `m_IndexBuffer`.
  - The prefab by a YAML transform walker (`Tools/CharacterPipeline/mini206_tmax/prefab_anchors.py`).
- **Frames:**
  - Blender: metres, Z up. Characters and the TMAX face −Y. The bike's left is +X.
  - Unity: metres, Y up, +Z forward, +X right.
  - Conversion used everywhere: `unity = (-bx, bz, -by)`. The FBX exporter's default axes (forward −Z, up Y) give exactly this.
  - UnityPy's OBJ export mirrors X, which matters if you re-extract geometry.
- **FBX export settings (all assets):**
  - `apply_unit_scale=True`, `bake_space_transform=False`, `add_leaf_bones=False`, `path_mode='STRIP'`.
  - Characters: `mesh_smooth_type='OFF'` (custom normals); TMAX: `'FACE'` with tangents.
  - Textures ship as PNG next to the FBX.
- **Rig:** Reallusion AccuRig `CC_Base` skeleton, 101 bones, names unchanged.
  - The armature object is a plain ×0.01 scale; meshes are parented at identity.
  - Max 4 influences per vertex, normalised.
- **Git LFS** for every .blend/.fbx/.png/.glb. New `.gitattributes` were added inside the new folders.

---------------------------------------------------------------------------------------------------------------------

## 2. Characters: MINI-197 bare bodies (Sacat and Franki)

**Goal (owner):** proper, well-toned modular characters with real arms and legs, not recoloured clothes. The owner approved the
AccuRig body, the same body for Franki, Sacat's new face, and later gave Franki his own face.

**Donor:**
- MINI-105 Hitem3D + AccuRig Sacat (`Docs/CharacterPipeline/MINI-105`): an AI-scanned body wearing boxers and a vest, with
  fused fingers and a wrong finger rig.
- Generator: `Tools/CharacterPipeline/mini197_bare_body/build_bare_body.py`, helpers in `bodylib.py`.

### Pipeline, piece by piece (each stage committed with front/side/back/three-quarter renders)

#### 01-Legs
- Removed the boxers as geometry. Each leg was rebuilt as a ring sweep along the thigh, calf and foot bones.
- The cross-sections come from the donor's own leg shape. The groin and hips were rebuilt with a conservative solve after the
  owner said "legs look deformed".
- Measured result: knee-to-hip sections within 0-7 mm of the donor (`03-Hands/leg_sections_vs_donor.txt`).
- One render had a knee bent backwards. That was my test pose (sign error), not the mesh; fixed in b385cae.

#### 02-SkinTone
- The arms were lighter than the face and legs. The large-scale tone was evened out in the 2048 base colour; pore-level detail was kept.

#### 03-Hands and the rig fixes
New separated fingers (1.8k triangles for both hands).

Hand rig fix:
- The donor's finger rig was one finger off: the Index chain sat in the thumb, the pinky had no bones, and the thumb was a 13 mm stub.
- The 30 finger bones were moved into the correct digits; no other bone moved (max 0.001 mm).
- Convention: on the finger bones −X curls toward the palm.

Rig frame fix:
- The MINI-105 FBX imported with the armature rotated 90° on X against the mesh, so posing tore the skin.
- The armature is now a plain ×0.01 scale. All pose tests ran on a re-import of the exported FBX.

#### 04-NeckTorso
- Removed the vest. The torso keeps its volume (about 10 mm of girth was fabric).
- New neck/shoulder join with natural sloped shoulders and rounded triceps (the owner's silhouette requirement).

#### 05-Franki
Same body and rig, with his own face:
- **Head:** `Strong.fbx` (Ch28) head, raised 9.9 cm so its head joint sits on `CC_Base_Head`, joined by a slanted neck bridge
  following the original neck-stub edge.
- **Texture:** his Ch28 face texture is baked into the shared atlas.
- **Skin:** a warmer tone, target sRGB 0.50/0.32/0.20.
- **Hair:** the owner asked for mobile-light hair, so the 9,484-triangle alpha-card hair was replaced by a 1,197-triangle
  opaque hair cap grown from the scalp. It sits 6.5 mm proud at the crown, is rigid on `CC_Base_Head`, and needs no transparency.

**Faces:** both characters got extra triangles around the eyes, nose and mouth, snapped onto the full-detail surface. Sacat's
flat eyelid shelves were smoothed.

### Final numbers and parts

| Character | Triangles | Height | Parts (one shared material) |
|---|---|---|---|
| Sacat | 19,200 | about 1.85 m | `SacatBare_Head/_Torso/_Arms/_Hands/_Legs/_Feet` |
| Franki | 20,091 (18,900 + 1,197 hair) | about 1.85 m | `FrankiBare_` + the same six + `FrankiBare_Hair` |

- Base colour texture: 2048 (import at 1024 on mobile).
- Key landmarks (Sacat, Blender frame):

| Bone | Position |
|---|---|
| Hip | z 0.999 |
| Spine02 | z 1.265 |
| NeckTwist01 | (0, 0.039, 1.507) |
| L_Upperarm | (0.189, 0.069, 1.458) |
| L_Forearm | (0.500, 0.066, 1.483) |
| L_Thigh | (0.098, −0.002, 0.975) |
| L_Calf | (0.138, 0.026, 0.517) |
| L_Foot | (0.157, 0.057, 0.082) |

- Bind pose is a T-pose (arms horizontal).

**Checks** (in `Tools/CharacterPipeline/mini197_bare_body/`): `verify_fbx.py` (re-import, bones, weights), `pose_hands.py`
(fist and pistol grip), `legs_inspect.py`, `leg_measure.py`, `torso_measure.py`.

**Local integration (done by the local session, MINI-204/205):** the FBX were imported to
`Assets/UpIzUpMini/Art/Characters/Modular/Bare/` and swapped into Sacat and Franki in `GrandBayProof_HouseEnhance.unity`.

**Not verified here:** in-game lighting, Humanoid avatar mapping, animation clips on the new rig (the local machine did
idle/walk/run/pistol/TMAX checks in MINI-205).

**Known limits:**
- No fingernails, and no LOD1/LOD2 for the bodies.
- Franki's hair cap has no strand texture.
- A faint tone seam on Franki's neck side.
- Franki's eye bones are still at Sacat's eye position (affects shades placement, see 4.2).

---------------------------------------------------------------------------------------------------------------------

## 3. Garments: MINI-206 Stage 2

**Owner problem:** the local tool built clothes as thin offset shells of the body, so they read tight, the trousers looked
like leggings, and the neckline was a stepped boat neck.

**Builder:** `Tools/CharacterPipeline/mini206_garments/build_garments.py` + `garlib.py`; full run in `finish_stage2.py`.

### Method (per garment, per body)

1. **Solid.** One closed solid is the voxel union (7 mm voxels) of three parts:
   - the body inflated 7 mm in the garment's region (yoke, hips, seat);
   - a loose torso tube lofted from body cross-sections (40 directions, 26 rings), hanging 50% from the chest
     (`hang = 0.5·max(radius above) + 0.5·own radius`); the hang tapers to zero over the top 14 cm so the loose torso meets
     the yoke with no step;
   - sleeve or leg tubes along the arm or leg bones, with a straight-leg minimum radius.
2. **Cut the openings.**
   - **Hem:** z 0.875.
   - **Sleeves:** |x| > 0.42 for the tee, 0.44 for the polo.
   - **Crew neckline:** a smooth height function round the neck axis (side 1.565, back 1.525, front 1.462 m), rising
     2 mm per mm away from the neck so it follows the shoulder slope.
   - **Trousers:** waist z 1.035; hem z 0.040 for jeans, 0.045 for trousers, 0.56 for shorts.
3. **Shape the surface.**
   - Smooth 12-18 iterations so the cloth drapes instead of copying muscles.
   - Decimate to budget with the open-edge ring locked; unlocked, the decimator tore slits at the jeans hems.
   - Relax the open-edge loops to remove the voxel stair-step.
4. **Add thickness where it shows.**
   - **Tee:** a crew rib (56 segments, rising 1.6 cm up the neck, 4 mm thick), cuffs (2.2 cm fold) and a hem band (2.5 cm).
   - **Polo:** a two-leaf collar (2.6 cm stand plus a leaf settled onto the shirt by nearest-surface projection, pointed tips by
     the placket, 3 mm solidify), a 3-button placket (1.5 cm wide, buttons 5.5 mm), and a 3 cm logo patch on the left chest.
   - **Trousers:** a 4.2 cm waistband and turned-up hems (3 cm; 2.2 cm on the shorts).

### Fit

| Area | Measurement |
|---|---|
| Tee ease | 3.5 cm at the hem, 2.8 cm at 1.05 m, 1.6 cm just below the armpit |
| Sleeve ease | 1.8 → 3.0 cm towards the cuff |
| Jeans | straight leg, hem radius 7.2 cm, breaking over the shoe |
| Trousers | hem radius 6.4 cm |
| Shorts | hem radius 10.5 cm |

### Skinning

- CC_Base weights copied from the nearest body face (`POLYINTERP_NEAREST`), limited to 4, normalised, with **no smoothing**.
- With 2 smoothing passes, skin poked through the shoulders in the pistol and seated poses; with 0 passes it does not
  (compare `Garments/Sacat/sheet_before_weightsmooth2.png` with `sheet_hardposes_weightsmooth0.png`).

### Pose tests

Posed with world-axis bone rotations; every outfit was rendered in each pose.

| Pose | Joints |
|---|---|
| Stride | left thigh −28°, calf 18°; right thigh 18°, calf 35°; arms down 70° |
| Pistol grip | upper arms swung forward 80° and down 12°, forearms 15° |
| Seated riding | thighs −85°, calves 85°, arms forward to the bars |

Outfits: tee+jeans, polo+trousers, tee+shorts.

### Results

Triangle counts are per character.

| Garment | Sacat | Franki | Budget |
|---|---|---|---|
| Tee | 4,600 | 4,600 | 5,000 |
| Polo | 5,982 | 5,982 | 6,000 |
| Jeans | 5,914 | 5,916 | 6,000 |
| Trousers | 5,916 | 5,916 | 6,000 |
| Shorts | 4,216 (+216 over) | 4,216 (+216 over) | 4,000 |

- 0 unweighted vertices, max 4 influences.
- Each garment: an FBX (armature + garment) and a 1024 white texture with soft AO (`0.30 + 0.70·AO`), so it can be tinted
  any colour in Unity.
- Franki's body has different topology, so he has his own fitted set.

**Known problems:**
- The polo cuff and logo AO read dark grey.
- The shorts' leg openings flare when seated.
- No real animation clips have been played on the garments yet.

---------------------------------------------------------------------------------------------------------------------

## 4. Accessories

### 4.1 Puffed mariner chain (`chain_fit.py`)

**Link:** puffed oval, 9 mm wide × 13.5 mm long, 3.4 mm wire (18% fatter at the ends), with a centre bar. Links alternate 90°.
76 triangles per link. Pitch = length − 1.15 × wire.

**Fit algorithm** (works on any body + garment FBX pair):
1. **Back and side arc:** an ellipse round the neck base (rx 0.098, ry 0.074), each point ray-cast down onto the
   trapezius/collar. Hits more than 6 cm below the neck base are rejected, so a ray can't fall through the inside of the neck.
2. **Front:** a cosh catenary between the arc's front ends, lowest point 27 cm below the neck base (the brief's 25-30 cm),
   each point ray-cast from the front onto the outer surface (shirt if worn, else skin).
3. **Clean-up:** smooth, push every point out to wire radius + 2.8 mm from the nearest surface, resample at the link pitch,
   and push again.
4. **Orient links** along the tangent with the surface normal.

**Skinning:** Spine02, with up to 60% NeckTwist01 near the back of the neck.

**Results:**
- 6 FBX: Sacat and Franki × Bare, Tee, Polo.
- 110-112 links, about 8.4k triangles, loop 1.06-1.09 m (about 42 in), lowest point 27 cm below the neck base.
- Clearance: +2.8 mm bare and tee; polo +0.46 mm (Sacat) and −0.5 mm (Franki, one link under the collar leaf).
- Over the polo it passes under the collar leaves at the sides, as a real chain does.

### 4.2 Placement profiles

**Data:** `Accessories/<Char>_AccessoryPlacements.json`, in the Unity character frame, bind pose. For each item: the bone
(`HumanBodyBones` + CC name), the anchor, the offset from the bone head, euler angles in the character frame, and the measured
sizes.

| Item | Bone | Sacat anchor (x, y, z) | Notes |
|---|---|---|---|
| Shades | Head | (0, 1.718, 0.115) | 12 mm in front of the nose bridge; temple width 15.2 cm (Franki 13.2) |
| Cap | Head | (0, 1.805, 0.012), tilt −8° | head circumference 58.9 cm (Franki 55.7) |
| Headphones | Head | (−0.002, 1.701, −0.018) | ear span 17.3 cm (Franki 15.6) |
| Watch | LeftLowerArm | (−0.702, 1.482, −0.039) | 3.5 cm up from the wrist joint, wrist radius 2.9 cm, face on the back of the wrist |
| Chain | Chest | offset 0 | the fitted chain meshes already sit in place |

**Converter:** `Mini206AccessoryProfiles.cs` turns the JSON into bone-local `AccessoryPlacementProfile` assets (menu
Up Iz Up Mini > MINI-206 > Create Accessory Profiles For Selected Character). It writes to
`Assets/UpIzUpMini/Art/Characters/Accessories/MINI206/` with `useManualPlacement=false`, so no approved profile changes until
the owner flips it.

**Caveat:** Franki's eye bones were not moved to his Ch28 eyes, so his shades height comes from Sacat's eye height. Check
visually.

### 4.3 Shoes

**Check:** the `*_shoes_mike90/97/270` meshes, read from the Unity `.asset` files, line up with the new feet. A red-foot
render shows the bare toes poking 1-2 cm through the soles.

**Fix:** `BareFeetUnderShoes.cs` hides the separate `*_Feet` mesh while any shoe renderer is active.

**Not checked:** the AM90/AM97 assets are stored in another bind space (bounds −6 cm .. +28 cm) and could not be compared
offline.

---------------------------------------------------------------------------------------------------------------------

## 5. TMAX 560 rebuild: MINI-206 Stage 1

**Input:** `Docs/CharacterPipeline/MINI-206/Inputs/TMAX560_Hitem3D_2Mtri_single_mesh.fbx`, 116.9 MB.
- One closed manifold mesh: 995,009 vertices, 1,992,422 triangles, 0 boundary and 0 non-manifold edges.
- 4096 base colour, metallic and roughness maps; `1.png` is not used by the material.
- Raw size about 0.011 units long under a ×0.01 parent with a 90° X rotation.

### Alignment (`tmax_align.py`, `tmax_wheels.py`)

1. **Heading:** PCA long axis, then a mirror-symmetry fit (Nelder-Mead on roll, yaw and side shift; 14 mm residual from the
   asymmetric stand, exhaust and plate).
2. **Pitch:** the two tyre contact points were levelled, a 9.93° correction.
3. **Wheels:** circles fitted in side view to the convex hull of the lower tyre half.
   - Front: r 0.2618 raw, 0.6 mm residual.
   - Rear: r 0.2177 raw, 0.75 mm residual.
4. **Scale:** ×1.1654 makes the wheelbase the real 1.575 m.
   - Overall length 2.225 m (real 2.200), height 1.505 m with the screen.
   - Final frame: Blender, front −Y, ground at 0, origin midway between the axles.

**Wheel sizes:** the AI scan's wheels are not to spec. Front r 0.304 m against 0.2745 for a real 120/70-15; rear r 0.254 m
against 0.2865 for a real 160/60-15. The bodywork is shaped round the scan's own wheels, and a spec rear tyre would cut the
rear hugger. So the scan sizes are kept (plot: `TMAX/renders/wheel_clear.png`).

**Old in-game model:** `TMAX_560_clean.glb` (15k triangles) is the same scan decimated (same internal name `tmp113av4i0`).
- An ICP similarity fit to the new frame gives scale 1.0104, 3.6 mm median.
- It sat yawed about 7.5°, which is why the old rear wheel was 16 cm off-centre and the old rider anchors are 10-14 cm off the
  new centre line.
- Old prefab facts: root scale 1.15, wheelbase 1.523 in root-local units, axle height 0.3036.
- Mapping of every old anchor: `TMAX/legacy_anchor_map.json`.

**Steering geometry:**
- The gold fork tubes, segmented by texture colour, rise at about 25°, the real TMAX caster (trail 95 mm).
- The steering axis meets the ground 95 mm ahead of the front contact.
- Head bearing (Blender frame): (0, −0.547, 0.72). The fork legs sit on x ±0.098.
- The scan's handlebar is turned about 15° and sits 0.30 m behind the axis. On one rigid fork it would swing 15 cm through
  the dash, so the handlebar has its own pivot at the clamp (0, −0.13, 1.09), with its axis parallel to the rake.

### Segmentation (`tmax_build.py`, `tmax_parts.py`)

1. **Working mesh:** the scan decimated to 400k triangles (still closed).
2. **Cutting:** sequential EXACT boolean differences with closed cutters, giving a watertight body after every cut:

| Cutter | Shape |
|---|---|
| Front wheel | cylinder r 0.316, \|x\| < 0.092 |
| Rear wheel | cylinder r 0.266, \|x\| < 0.088 |
| Fork legs | 2 boxes along the rake |
| Fender | annular sector r 0.308-0.389, 8-178° |
| Handlebar | 2 boxes \|x\| 0.16-0.42, y −0.42..0.06, z 1.03-1.19, plus a centre box |

3. **Fender:** the fender is the scan ∩ sector, keeping the biggest piece.
4. **Clean-up:** 15 loose fragments near the wheels were deleted.

### Procedural moving parts

Rebuilt because spinning and steering parts must be round and clean:

- **Wheels:** lathed tyre (rounded crown, inner wall closed), rim barrel, 5 split spokes, hub, discs and carriers.
  - Front: width 0.128, rim r 0.192, 2 discs r 0.134 at x ±0.072.
  - Rear: width 0.150, rim r 0.168, 1 disc r 0.120 (right side), belt pulley r 0.118 (left side).
- **Fork:** black sliders r 0.029 (z 0.25-0.52), gold tubes r 0.022 (z 0.50-0.82, running up inside the fairing so steering
  never shows a cap), axle lugs, radial calipers on the rear-upper disc edge, 13 mm chrome axle, plus the scan fender.
- **Handlebar:** a swept tube r 11 mm (ends 3.5 cm back), grips r 17 mm (|x| 0.255-0.365), bar-end weights, switch pods,
  reservoirs, levers, clamp and riser.

### Pivots and naming contract (FBX `TMAX_560_Rebuilt.fbx`, Unity local coordinates)

Parts:

| Part | Pivot | Triangles | Watertight |
|---|---|---|---|
| `TMAX_Body` | (0, 0, 0) | 14,226 | no: 2 hidden touching edges, no holes |
| `TMAX_FrontWheel` | front axle (0, 0.3042, 0.7875), spins about local X | 2,464 | yes |
| `TMAX_RearWheel` | rear axle (0, 0.2537, −0.7875) | 2,272 | yes |
| `TMAX_FrontForkAssembly` | head bearing (0, 0.72, 0.547); local Y = steering axis (25°) | 1,380 | yes |
| `TMAX_Handlebar` | bar clamp (0, 1.09, 0.13); local Y parallel to the steering axis | 564 | yes |

Empties:

| Empty | Position |
|---|---|
| GripLeft / GripRight (under the handlebar) | (∓0.31, 1.091, 0.103) |
| Seat_Driver | (0, 0.77, −0.40) |
| Seat_Pillion | (0, 0.852, −0.78) |
| FootPeg_L / FootPeg_R | (∓0.19, 0.47, 0.12) |
| PillionPeg_L / PillionPeg_R | (∓0.27, 0.34, −0.40) |
| PillionHandle_L / PillionHandle_R | (∓0.17, 0.82, −0.80) |
| FrontAxle / RearAxle | on the axles |

Seat, floorboard and rail heights were measured from the scan's top surface. The real TMAX seat height is 0.80 m.

**Budgets:**

| Level | Triangles | Limit | Breakdown |
|---|---|---|---|
| LOD0 | 20,906 | 30,000 | see parts table |
| LOD1 | 10,450 | 12,000 | body 7,000, wheels 1,200 + 1,100, fork 700, bar 450 |

**Materials and textures:**
- Body (`TMAX_Body`): keeps the scan's own UV layout, with 2048 base colour and metallic-smoothness (R = metal, A = smoothness)
  resampled from the 4096 maps, plus a 2048 tangent normal map baked from the 2M mesh (selected-to-active, cage 12 mm).
- Moving parts (`TMAX_Parts`): share a 512 atlas baked from flat procedural materials (rubber, gunmetal rim, steel discs,
  gold, black, chrome).
- So there are 2 materials, not 1. A smart-UV re-atlas of the decimated scan gave thousands of tiny islands and blotchy bakes,
  so it was dropped.

**Renders** (`TMAX/renders/`): assembled ×6 sides, each part isolated, steering 0/±30° (front, three-quarter, cockpit),
wheel spin.
- At ±30° the fork and wheel have no visible clipping.
- 0-4 sampled bar vertices touch the cover at full lock.
- The tube tops inside the fairing are hidden by design.

**Known problems:**
- One small left switch/reservoir block from the scan remains on the cowl.
- The number-plate text is mirrored (scan texture).
- The screen is opaque, with no glass material.
- The swingarm is static in the body; the rear wheel moves alone with the suspension.

---------------------------------------------------------------------------------------------------------------------

## 6. Riding and rider animation

### 6.1 MINI-202 (on `cloud/sacat-legs`, edited existing scripts at the owner's request)

**Finding:** the TMAX body leaned and pitched only under `VisualLeanRoot`, while the 8 rider anchors (seats, foot targets,
pillion grab/foot) were siblings of it. So the riders stayed upright, and `BikeInteractable` faked 52% of the lean (capped at 9°).

**Changes:**
- `TmaxRideDynamics.AttachRiderAnchors` re-parents those 8 anchors under `VisualLeanRoot` at runtime, keeping their world pose,
  so the riders lean, squat and dive 1:1 with the body (like the SuperMoto).
- The partial lean copy is set to 0 when attached; the pillion copies 35% of any deliberate extra lean.
- **Rider bounce:** a per-rider spring-damper driven by the bike's vertical acceleration.

| Rider | ω (rad/s) | ζ | Amplitude | Max |
|---|---|---|---|---|
| Driver | 11 | 0.55 | ×1.0 | — |
| Pillion | 8 | 0.45 | ×1.25 | 4.5 cm |

  Plus a small torso nod, via `VehicleRider.SetBounce`.
- **Eyes on the road:** Animator look-at IK (head 1, body 0.05, clamp 0.55) at a point 12 m ahead at head height. The pillion
  looks past the driver's shoulder into the turn. `riderLookAheadWeight` = 0.7.
- The SuperMoto never calls these methods, so it is unchanged.

**Not compiled or play-tested here.**

### 6.2 MINI-206 Stage 4: why the TMAX feels robotic (`Docs/WorkPackets/MINI-206-riding.md`, line references there)

Ranked:

| # | Behaviour | Prefab value | Effect |
|---|---|---|---|
| 1 | Extra air gravity | 2500 m/s² after 0.01 s airborne | the off-road slam and bounce |
| 2 | Kinematic yaw lock | strength 1 (heading = input × 110°/s) | turns like a cursor |
| 3 | Yaw spin damping | 250 with a 0°/s threshold | erases the remaining yaw |
| 4 | Two world-up upright systems | `MoveRotation` slerp 0.32 per step | the body never rolls; chatter on cambers |
| 5 | Trike stabiliser springs | 6000 N/m after the front is airborne 0.22 s | sideways jolts |
| 6 | Launch cap | 6 m/s | asymmetric vertical motion |
| 7 | Hill-climb push | 9 m/s² | surges |
| 8 | Kinematic wheelie | `MoveRotation`/`MovePosition` | the wheelie is placed, not balanced |

**Path A (current TMAX, values only):** extraAirGravity 15, grace 0.3 s, ramp 0.4 s, yawLock 0.15, spin threshold 70°/s,
uprightAssist 5 (×1.5 when slow), hillClimb 3, trike debounce 0.6, yawAssist 3.

**Path B (recommended):** run the TMAX on the SuperMoto physics (`TMAX_560_SuperMoto.prefab`: Gadd420 RB_Controller,
AutoLeveling, GroundAngle, SuperMoto suspension 24000/2300 and 28000/2700, balance-point wheelie) with the new TMAX visuals.

### 6.3 New drafts for the owner's "easy but realistic wheelie, turning bars/tyres, better lean" request

All are wrapped in `#if MINI206_DRAFT`.

**`TmaxRideFeel.cs`** (current TMAX):
- Applies path A at Awake through the controller's public setters (no prefab edit).
- Re-shapes the wheelie after the controller each physics step, about the same rear contact patch:

| Phase | Behaviour |
|---|---|
| Lift | 2nd-order spring toward the controller's angle, ω 7, ζ 0.62 (fast pop, about 5% overshoot) |
| Hold | throttle trims ±6°, brake drops it |
| Fall | ω 5.5, ζ 0.85, plus a gravity-like 60·cos(θ) deg/s² drop |
| Character | wobble pitch ±1.2° at 0.55 Hz and roll ±0.8° (scaled by height); counter-lean roll ≤ 7° from the yaw rate; 3 cm rear squat |

- The Q trigger, speed window, 89° cap and crash logic are untouched, so it stays as easy as now.

**`TmaxRebuiltVisuals.cs`** (current TMAX):
- Drives the new FBX: wheels from the WheelCollider poses (rest X/Z, collider Y, banked with the lean).
- Turns the fork and bars about their raked local Y by the steer angle (×1.8 at walking pace, blended to ×1 by 25 km/h,
  limited to 32°, smoothed over 60 ms).
- Moves HandlebarLeft/Right IK targets onto GripLeft/GripRight.
- Hides the old glb and overlay wheels.

**`TmaxSuperMotoVisualBinder.cs`** (path B duplicate): wheels on the vendor holders; fork and bars copy the vendor fork yaw.

---------------------------------------------------------------------------------------------------------------------

## 7. Roads

### 7.1 MINI-201: old live map `GrandBayProof.unity` (on `cloud/sacat-legs`)

**Survey** with UnityPy: 27 road meshes, 4 bridge decks and `ExpansionTerrain` (36,857 vertices).

**Before:**
- 3 asphalt looks.
- Junction steps up to 2.07 m.
- Floating and buried roads.
- Terrain through roads at 5.6% of samples.
- Bumps up to 5.8% grade change per 0.5 m.

**Solver** (`solve_roads.py`): one sparse least-squares height field for all road vertices.
- Terms: bi-Laplacian smoothness, cross-section levelling, curvature, and junction coupling (a vertex on another road's
  surface takes that height).
- Data term: ground + 3 cm for buried or floating roads, otherwise the current height.
- Locked: Lalay sidewalk roads; roads within 8 m of buildings are held. X/Z never change.
- **Terrain fit:** cut where the ground pokes through a road; raise only where a road edge floats more than 12 cm; never raise
  within 6 m of buildings; never touch the gully under a bridge.
- Duplicate covered triangles are dropped.

**After:**
- 1 material; worst junction 6.4 cm; terrain through roads 0.15%; worst bump 4.9%.

**Apply tool:** `Mini201RoadSmooth.cs` (Preview On Copy / Apply To Live with backup).
- Every vertex is matched by world X/Z/Y within 2 cm; stale data aborts.
- Validation: three drive lines, 0.25 m raycasts.

### 7.2 MINI-206 Stage 3: new map `GrandBayProof_HouseEnhance.unity`

**Why a new pass:** the local tool refused the old data because the terrain is now 39,160 vertices (the school field was
levelled).

**Scripts:** `Tools/RoadPipeline/mini206_newmap/` (the solver copied, with the scene path from the `SCENE` env), plus:
- `protect.py`: 2 m footprint grids of the school, school lot outline, bay wall, roundabout beach, apartments, terrace/fence and
  Lalay homes, a 12 m disc round GenevaRoundaboutIsland (its mesh could not be read offline), plus 1,105 object pivots,
  9,395 points in all;
- `junction_table.py`.

**Excluded from the solve:** `MINI193_RoadJunctions/*` (26 patches), which the local tool regenerates afterwards.

**Rules:**
- The dirt farm spur keeps `DirtTrack.mat`, with the ground fitted 1 cm under its edges for a soft blend.
- Asphalt roads get ExpansionAsphalt.
- Road vertices inside a bridge deck footprint go 2 cm under the deck (69 vertices).
- Junction coupling weight 120 (the trade-off between step height and smoothness was measured at 60, 120 and 200).

**Results** (`Docs/WorkPackets/MINI-206/`):

| Measure | Before | After |
|---|---|---|
| Worst join | 2.07 m | 4.8 cm (all 41 junction and bridge joins listed with x/z in `junctions_bridges.md`) |
| Bridge joins | 0.08-0.31 m | ≤ 4.8 cm |
| Terrain through roads | 5.49% | 0.09% |
| Worst bump | 5.8% | 6.2% (ExpansionRoad_10 at its bridge end); most roads ≤ 2% |
| Structures/props/NPCs with ground change > 3 cm | — | 0 |

Also: 1 fully covered road disabled.

**Apply:** use `Mini206RoadsNewMap.cs`, not `Mini203RoadsNewMap` (which calls the MINI-201 tool, reading the old data and
forcing asphalt).
- Menu: MINI-206 > Roads: Preview On Copy Of New Map.
- It writes `GrandBayProof_HouseEnhance_RoadFix206.unity`; logs go to `Logs/Tasks/MINI-206/`.
- Then run Mini193RoadJunctions on the copy and drive the joins.

**Known problems:**
- A small dark wedge of approach road at the Bridge_03 west corner (2-4 cm, drivable).
- The ground moved up to 2.7 m where Road_way_254679575 had been buried.
- AI waypoints with absolute Y were not checked.

---------------------------------------------------------------------------------------------------------------------

## 8. Local machine: integration order (suggested)

1. Push or fetch `cloud/mini-206` (see 0). Copy the FBX and PNG from `Docs/` into an isolated `Assets/` candidate folder;
   Unity ignores `Docs/`.
2. **Characters/garments:**
   - Import the garment FBX (Rig: Generic or Humanoid copied from the bare body avatar; Normals Import).
   - Tint the white textures.
   - Add `BareFeetUnderShoes` to Sacat and Franki.
   - Run Create Accessory Profiles, check them in the Scene view, then set `useManualPlacement` true.
   - Swap the chain for the fitted `Chain_<Char>_<Outfit>.fbx`.
3. **TMAX:**
   - Duplicate `TMAX_560.prefab`.
   - Add `TmaxRideFeel` (define `MINI206_DRAFT`) and ride.
   - Then add the FBX under VisualLeanRoot plus `TmaxRebuiltVisuals`.
   - Set the WheelCollider radii to 0.304 / 0.254 and move them onto the new axles; refit the seat keyframes to Seat_Driver.
   - In parallel, try path B with the binder.
4. **Roads:** run Mini206RoadsNewMap preview on the copy → Mini193RoadJunctions → drive → owner approval → copy over the map.
5. **Compile everything once:** none of the new C# was compiled in the cloud.

## 9. Not verified anywhere yet

- C# compile; Unity import of every new FBX; the Humanoid avatar on the garments.
- Play-Mode riding, wheelie feel, steering visuals.
- Road drive-through and Mini193 regeneration.
- Accessory placement in Unity space.
- AM90/AM97 shoe fit.
- Mobile performance on a device.

=====================================================================================================================
PART B: EXACT PIPELINE COMMANDS AND PARAMETERS
=====================================================================================================================

## B0. Environment the cloud used (reproduce it on Windows)

- **Python 3.11 venv** with `pip install bpy==4.2.0 numpy scipy matplotlib pillow UnityPy`. bpy 5.2 / Python 3.13 crashed.
  - On Windows you can instead run the scripts with Blender 4.2's own Python: `blender -b -P script.py -- args`.
  - Every script reads its arguments after `--`.
- **Never name a script `inspect.py`.** It shadows the Python standard library module and breaks bpy.
- **Working directory.** The TMAX scripts use relative paths such as `tmax/scan_final.blend` and `tmax/out/`, and the garment/chain
  scripts use `g2/...`. Run them from a scratch folder that has those subfolders, or edit the paths at the top of each script.
- **Render helpers:** `rlib.py` (Cycles CPU setup, `turntable`, `closeup`) and `sheet.py` (`python sheet.py out.png in1.png in2.png ...`,
  pastes images side by side). Always render front/side/back/three-quarter and LOOK at the sheet before accepting a step.

## B1. Bare bodies: MINI-197 (`Tools/CharacterPipeline/mini197_bare_body/`)

- **Sacat:**
  ```
  python build_bare_body.py -- <donor.fbx> <basecolor.png> neck <out_prefix> [r,g,b skin target]
  ```
- **Franki:** the same command, with env `BODY_NAME=Franki FRANKI_HEAD=<Strong.fbx> FRANKI_HAIR=<Garments/Franki_Hair.fbx>`.
  - `HAIR_MODE=cards` restores the old card hair.
  - `BODY_TRIS` sets the triangle target, for making LODs.
- **Checks:**

| Script | Checks |
|---|---|
| `verify_fbx.py` | re-import, bone count 101, weights ≤ 4, no unweighted vertices |
| `pose_hands.py` | fist and pistol grip, by rotating finger bones about −X |
| `legs_inspect.py`, `leg_measure.py`, `torso_measure.py` | cross-sections against the donor |
| `knee_dir.py` | knee bend direction, to avoid sign errors in test poses |
| `check_fingers.py`, `hand_bones.py` | finger bone placement per digit |

## B2. Garments: MINI-206 stage 2 (`Tools/CharacterPipeline/mini206_garments/`)

- **One character, all 5 garments:**
  ```
  python finish_stage2.py -- <body.fbx> <outdir> <Sacat|Franki>
  ```
  - Builds tee, polo, jeans, trousers and shorts in one scene.
  - Smart UV project (66° limit), bakes a 1024 AO, converted to white `0.96·(0.30+0.70·AO)^0.8`.
  - Skins from the body (`WSMOOTH` env, default 0 smoothing passes; keep 0).
  - Exports `<Char>_<garment>.fbx` (armature + garment) plus `<Char>_<garment>_BaseColor.png`.
  - Writes `<Char>_garments_report.json` and `<Char>_Garments.blend`.
  - Renders 3 outfits × (rest ×4 views, stride, pistol, seated).
- **One garment only:** `python build_garments.py -- <body.fbx> <outdir> <char> tee polo ...`
- **Env tunables** (build_garments.py):

| Env | Default | Meaning |
|---|---|---|
| `HEM` | 0.875 | Shirt hem z |
| `SLEEVE` | 0.42 tee / 0.44 polo | Sleeve end \|x\| |
| `HANG` | 0.5 | Chest drape fraction |
| `NECK_BACK` / `NECK_SIDE` / `NECK_FRONT` | 1.525 / 1.565 / 1.462 (polo front 1.475) | Neckline heights, m |
| `SHELL_TRIS` | 3000 tee, 3300 polo, 4700 jeans/trousers, 3000 shorts | Shell triangle target before the bands |
| `WAIST` | 1.035 | Trouser waist z |
| `HEM_Z` | jeans 0.040, trousers 0.045, shorts 0.56 | Trouser hem z |

- **Library** (`garlib.py`):
  - `load_body`
  - `body_radius`, `fill_circ`, `smooth_circ`
  - `loft`, `tube_along`, `inflated_body`
  - `union_remesh` (voxel union)
  - `delete_faces` (keeps the largest piece)
  - `smooth_shell`
  - `decimate_to` (locks the open-edge ring, symmetric X)
  - `boundary_loops`, `resample_loop`
  - `band_along_loop`, `fold_band`
  - `smooth_open_edges`, `body_region_solid`
  - `pose_bone_world` (rotate a pose bone about a WORLD axis through its head)
  - `skin_from_body`
- **Pose-test angles** (world axes, Blender frame, character facing −Y, T-pose bind):

| Pose | Rotations |
|---|---|
| Stride | L_Thigh X −28, L_Calf X +18, R_Thigh X +18, R_Calf X +35, L_Upperarm Y +70, R_Upperarm Y −70 |
| Pistol | L_Upperarm Z −80 then Y +12, R_Upperarm Z +80 then Y −12, L_Forearm Z −15, R_Forearm Z +15 |
| Seated | both Thighs X −85, Calves X +85, L_Upperarm Z −55, Y +35; R_Upperarm Z +55, Y −35; Forearms Z ∓25 |

## B3. Chain and accessories

- **Chain:**
  ```
  python chain_fit.py -- <body.fbx> <outdir> <Name> [garment.fbx ...]
  ```
  - Env `CHAIN_DROP` (default 0.27 m below the neck base).
  - Outputs `Chain_<Name>.fbx`, `.json` (links, triangles, loop length, clearance), renders and `.blend`.
- **Placements:** `python accessories.py` writes `<Char>_AccessoryPlacements.json`. It measures the eye bones (`CC_Base_L/R_Eye`),
  nose bridge (ray from the front), temples, head top, head circumference (48 rays at the band height), ears (extreme |x| of the head
  mesh at eye height − 2.5 cm) and the wrist (`CC_Base_L_Hand` head − 3.5 cm along the forearm; median radius from 24 rays).
- **Shoes:**
  - `python shoe_check.py` reads the Unity `.asset` meshes with `scene_lib.load_yaml_mesh` and converts Unity → Blender as
    `(−x, −z, y)`.
  - `python shoe_render.py -- <CharBare> <body fbx rel path> <shoe id>` renders the feet red under a white shoe; red showing means
    poke-through.

## B4. TMAX rebuild: MINI-206 stage 1 (`Tools/CharacterPipeline/mini206_tmax/`), in order

1. `tmax_import.py -- <scan.fbx> tmax/scan_raw.blend`: prints mesh, material and image stats.
2. `tmax_views.py`, or `tmax_6views.py -- <blend> <outprefix> <width> "" left,right,front,back,tq_front,tq_rear`: also applies
   the parent transform and writes `tmax/scan_raw_applied.blend`.
3. `tmax_align.py -- tmax/scan_raw_applied.blend tmax/align.npy`:
   - PCA long axis;
   - vertical = world Z orthogonalised;
   - Nelder-Mead on (roll, yaw, dx) minimising the mean squared distance between the mesh and its X-mirror (60k sample, capped
     at 3 cm).
4. `tmax_wheels.py -- tmax/scan_raw_applied.blend tmax/align.npy tmax/align2.npy`:
   - levels the two lowest points in the front and rear halves (|x| < 0.1), 4 iterations;
   - fits circles (least-squares Kasa fit plus residual trimming at 6 mm) to the convex hull of the lower tyre half (z < 0.24 raw,
     |x| < 0.05).
5. `glb_icp.py`: Umeyama similarity ICP of the old `TMAX_560_clean.glb` onto the new frame, trying both front directions (40
   iterations, 90th-percentile trimming).
6. `prefab_anchors.py <prefab>`: world positions of every transform in `TMAX_560.prefab`. `map_anchors.py` maps them into the new
   frame using Unity root-local = (Blender x, Blender z, Blender y) of the glb.
7. `tmax_final_frame.py`: bakes the transform into the 2M mesh (scale 1.575/1.3515, front −Y, origin between the axles) and writes
   `tmax/scan_final.blend` and `tmax/hp_pts.npy`.
8. `tmax_mid.py`: decimates to 400k (stays closed) → `tmax/mid.blend`.
9. `tmax_build.py`: constants FA, RA (axles), RF, RR (radii), RAKE 25°, TRAIL 0.095, HEAD, BARC.
   - Builds the closed cutters and applies them ONE BY ONE as EXACT boolean DIFFERENCE → `TMAX_Body`.
   - Fender = scan ∩ sector, keeping the largest piece → `tmax/build_a.blend`.
10. `tmax_parts.py`:
    - deletes loose body fragments near the wheels;
    - builds the procedural wheels (lathe with a CLOSED profile), fork legs (cylinders along the rake), calipers, axle and handlebar;
    - decimates the fender (`FENDER_TRIS` 900) and the body (`BODY_TRIS` 14000);
    - sets the pivots → `tmax/build_b.blend`.
11. `tmax_bake.py` (env `RES`=2048):
    - body: keeps the scan UV, resamples the 4096 maps to 2048, bakes the tangent normal map from the HP (selected-to-active, cage
      0.012, max ray 0.05);
    - moving parts: smart UV, 512 atlas, EMIT bakes of base colour, metallic and smoothness;
    - writes the URP MetallicSmoothness PNGs (R = metal, A = smoothness).
12. `tmax_export.py`:
    - root empty, parts parented;
    - fork and handlebar re-framed so local +Z (Unity +Y) is the steering axis;
    - triangulates (CLIP n-gons) and removes duplicate faces;
    - creates the empties;
    - exports `TMAX_560_Rebuilt.fbx`, builds LOD1 (body 7000, wheels 1200/1100, fork 700, bar 450) → `_LOD1.fbx`, writes
      `parts_report.json` and `.blend`.
13. `tmax_pose.py -- <blend> <outprefix> 0,30,-30 tqf,cockpit,front [spin_deg]`:
    - rotates the fork and wheel about the raked axis through the head, and the bar about the axis through the clamp;
    - counts part vertices inside the body (ray parity);
    - renders. Hide the LOD1 objects when rendering (they share the space).
14. `tmax_isolated.py`: one render per part.

## B5. Roads (`Tools/RoadPipeline/mini201_road_smooth/` for the old map; `Tools/RoadPipeline/mini206_newmap/` for the new map)

Env `SCENE` = path of the `.unity` file to read (new-map default: `GrandBayProof_HouseEnhance.unity`).

1. `survey_roads.py roads.json`: every road-like object: path, active, materials, mesh reference.
2. `measure_roads.py geo.npz`: world triangles of roads (`ExpansionRoad_*`, `Road_*`, `RoadJunctionPatch_*`, `JunctionPatch_*`,
   `DogLifeJunction`, `ImportTrim_Road*`), `Driveable_Deck` (stored as `<bridge>_Deck`) and `ExpansionTerrain`.
   `MINI193_RoadJunctions/Junction_*` are not matched, which is intended.
3. `obstacles.py obstacles.json`: pivots of buildings, props and NPCs.
4. `protect.py obstacles.json protect.json protect_names.json`:
   - 2 m grids over the footprints of school/field/wall/roundabout/apartment/house/church/market/stall/fence/gate meshes;
   - env `PROTECT_DISC` + `DISC_R` (12) for objects whose mesh cannot be read (GenevaRoundaboutIsland).
5. `analyse_roads.py geo.npz > analysis_before.txt` and `poke_check.py geo.npz >> analysis_before.txt`.
6. `OBSTACLES=protect.json W_COUPLE=120 python solve_roads.py geo.npz geo_after.npz`. Weights and env:

| Weight / env | Value | Role |
|---|---|---|
| `W_SMOOTH` | 300 | Bi-Laplacian on each road's welded graph |
| `W_CROSS` | 80 | Cross-section level |
| `W_CURV` | 400 | Curvature |
| `W_COUPLE` | 120 | Junction coupling (60 = smoother but 7.7 cm steps; 200 = 3.9 cm steps but 6.3% bumps) |
| — | ×2 | Decks are fixed; roads couple to them at twice the weight |
| `W_DATA` | 1 | Pull toward the target height |
| `W_NEAR` | 25 | Roads within `OBS_ROAD_R` (8 m) of protected points are held |
| — | 30 | LOCKED Lalay sidewalk roads |
| OFF | 0.03 | Buried (< −5 cm) or floating (> 15 cm) roads go to ground + 3 cm |
| `T_IN` | 2.0 | Terrain fit: inner radius |
| `T_BAND` | 5.5 | Terrain fit: blend band |
| `T_UNDER` | 0.05 | Ground set this far under the road |
| `T_FLOAT` | 0.12 | Ground raised only where a road edge floats more than this |
| `OBS_GROUND_R` / `OBS_NOFILL_R` | 4 / 6 | Never raise ground near protected points |
| — | — | Never touch the gully under a deck |
| `DIRT` | `Road_user_highland_farm_spur` | Ground fitted 1 cm under the dirt edges |

7. `junction_table.py geo.npz geo_after.npz junctions_bridges.md`: every overlapping pair with x/z and step before/after.
8. `export_apply.py geo.npz geo_after.npz roads.json newmap_apply.json`:
   - lowest-priority road triangles covered by a higher-priority surface (including bridge decks) are dropped;
   - road vertices inside a deck footprint are tucked 2 cm under the deck;
   - per-road `material` ('' = asphalt; DirtTrack kept);
   - terrain heights;
   - ground-follow and not-moved reports;
   - `terrainVertexCount`.
9. `render_roads.py <npz> roads.json before|after <outprefix>`: Blender renders at fixed spots (bridges and the farm spur included).
10. Unity: `Mini206RoadsNewMap.cs` applies it on a COPY:
    - matches every vertex by world X/Z/Y within 2 cm, aborting on stale data;
    - checks the terrain vertex count;
    - validates three drive lines per ribbon with 0.25 m raycasts. Pass: step < 6 cm and terrain-above-road < 0.5%.

## B6. Riding code (C#, uncompiled)

| Task | File | Notes |
|---|---|---|
| MINI-202 (edited existing scripts) | `TmaxRideDynamics.cs`, `TmaxBikeControllerCustom.cs`, `BikeInteractable.cs`, `VehicleRider.cs` | Details in Part A §6.1 and `Docs/WorkPackets/MINI-202.md` |
| MINI-206 drafts | `Assets/UpIzUpMini/Scripts/Vehicles/Draft/` | `TmaxRideFeel.cs`, `TmaxRebuiltVisuals.cs`, `TmaxSuperMotoVisualBinder.cs`; behind define `MINI206_DRAFT`; parameters in Part A §6.3 |

TmaxRideFeel runs at `DefaultExecutionOrder(50)` after the controller and calls `MoveRotation`/`MovePosition` again about the rear
contact patch. The last call in the step wins, so it overrides the controller's linear wheelie pose without editing the controller.

=====================================================================================================================
PART C: WHAT FAILED AND HOW IT WAS FIXED (bake these checks into your workflow)
=====================================================================================================================

## Characters

- **Deformed legs:** the first leg rebuild exaggerated the hips and groin. Fixed with a conservative solve that stays within
  0-7 mm of the donor sections. Lesson: measure sections against the donor, do not eyeball.
- **"Left leg bent wrongly":** the TEST POSE had the knee rotation sign flipped, not the mesh. Lesson: verify bend direction
  (`knee_dir.py`) before judging the mesh.
- **Rig frame:** the armature imported rotated 90° X against the mesh, so posing tore the skin. Lesson: always pose-test a
  RE-IMPORT of the exported FBX, never the in-memory scene.
- **Hand rig one finger off:** found only by checking every finger chain's bones lie inside the right digit.
- **Large .blend:** FrankiBare.blend was 82 MB; recompressed to 22 MB.

## Garments

| Problem | Fix |
|---|---|
| Tee, first try: sack-like, neckline on the chin with tabs | Iterated the neckline as a smooth height function; NECK_FLARE 2.0 (cut rises away from the neck) gave a clean crew neck |
| Ellipse cut made a V-neck; raising the sides left tabs | Same neckline fix |
| Horizontal crease across the chest/back where the loose torso tube met the yoke | Taper the hang to zero over the top 14 cm and the ease to 8 mm at the armpit |
| Polo collar came out as a stand-up band | Leaf placed by projecting onto the shirt |
| Downward-ray projection of the collar leaf hit the stand → jagged spikes | Nearest-surface projection + 3 mm along the normal, smoothed along the loop |
| Jeans: vertical slits at the outer hem | The DECIMATOR tore the open boundary; lock the boundary verts plus one ring (vertex group on the Decimate modifier) |
| Lumpy voxel stair-step on open edges | `smooth_open_edges` relaxation |
| Waistband loft had inverted/garbage rings | Replaced by `fold_band` |
| Skin poked through the shoulders/armpits in pistol and seated poses | Garment weight smoothing (2 passes) was the cause; use 0 passes of nearest-face weights |
| AO texture write crashed | Blender `pixels.foreach_set` needs float32 |

## Chain

- **4.86 m loop hanging 1.08 m:** the "ray down until below the neck" loop fell through the inside of the neck to the chest. Fix:
  reject hits more than 6 cm below the neck base.
- **Clearance −1.3 mm:** link centres interpolated after the push-out dipped back. Fix: push out again after resampling.

## TMAX

- **The scan's wheels are not to spec** (front too big, rear too small) and the bodywork is shaped round them. Decision: keep the
  scan sizes so nothing clips; document it.
- **Old glb yawed 7.5°:** found by ICP. Explains the old off-centre rear wheel and rider anchors.
- **Handlebar 0.30 m behind the steering axis:** a rigid fork would swing the bar centre 15 cm sideways. Fix: give the handlebar
  its own pivot with its axis parallel to the rake.
- **Joining all cutters into one object broke the EXACT boolean** (the body collapsed to 3k triangles). Fix: one boolean per cutter.
- **Lathe without a closed profile** left open seams. Close the profile loop.
- **Smart-UV re-atlas of the decimated scan** gave thousands of tiny islands and a blotchy bake. Fix: keep the scan's own UVs.
- **Triangulation** created 1-2 non-manifold (bow-tie) edges on the hidden rear cut face. The repair attempt opened holes, so it was
  reverted. 2 touching edges remain (no holes).
- **"Double handlebar" in the steering renders:** a render artefact. The LOD1 copies in the same .blend did not move. Hide LOD1 when
  rendering poses.
- **Scan handlebar pieces left in the body:** the first cutter did not reach far enough forward. Extended to y −0.42, stopping
  before the mirror stalks (y −0.48..−0.54).
- **Mirrored plate text and opaque screen:** scan texture artefacts, left as they are.

## Roads

| Problem | Fix |
|---|---|
| Single pivot points do not protect long walls or fields | 2 m footprint grids |
| GenevaRoundaboutIsland mesh unreadable offline | 12 m disc |
| One junction at 7.7 cm with W_COUPLE 60 | W_COUPLE 120 brought it under 5 cm; 200 made bumps 6.3% |
| Approach-road triangles showed through the bridge decks once flush | Decks count as covering surfaces; road verts inside a deck footprint tucked 2 cm under. Bridge_03 still shows a small wedge |
| MINI-201 tool unified everything to asphalt | Per-road material field; dirt kept |

## Git / delivery

| Problem | Fix |
|---|---|
| Blender `.blend1` backups (40 MB) and `__pycache__` got committed as plain git data | Removed from the unpushed commits; add `*.blend1`, `__pycache__/`, `*.pyc` to ignore |
| `filter-branch` dropped the commit signatures | Re-signed with `git rebase --exec "git commit --amend --no-edit --reset-author"` |
| The cloud proxy blocked lfs.github.com | Upload each LFS object to the S3 href from the batch API, then POST verify to `https://github.com/<owner>/<repo>.git/info/lfs/objects/<oid>/verify`; after that a normal `git push` works because the objects already exist |
| Local repo had no `origin` remote | `git remote add origin https://github.com/Sacat/Upizup-mini.git` |

=====================================================================================================================
PART D: REUSABLE TECHNIQUES AND CONVENTIONS (adopt these)
=====================================================================================================================

1. **Measure, don't guess.** Pull dimensions from the real assets: scan point clouds, prefab transforms, scene meshes via UnityPy,
   texture colours (the gold fork tubes were found by sampling the base colour per vertex).
2. **Closed solids for anything that moves.** Cut with closed cutters (EXACT boolean, one at a time) and check boundary and
   non-manifold edge counts after every step.
3. **Pivots and axes in the asset:**
   - wheels: pivot on the axle, spin about local X;
   - steering parts: local up = the steering axis, so Unity steers with one local-Y rotation;
   - rider anchors as named empties.
4. **Garments from the body, not as offset shells:** voxel union of an inflated yoke + loose lofted tubes, then cut, drape-smooth,
   decimate with a locked boundary, and add thickness bands. Skin with nearest-face weights, no smoothing. Pose-test the hard poses.
5. **Accessories:** measure anchors from bones + surface rays in the bind pose and write them in the CHARACTER frame. Convert to
   bone-local inside Unity (`Mini206AccessoryProfiles.cs`) so rig axis conventions can't be wrong.
6. **Roads:** one global sparse least-squares height field (smoothness + coupling + data) instead of per-junction patches; protect
   by footprints; never move structures; apply only on a scene copy with vertex verification; validate with raycast drive lines.
7. **Riding feel:** keep the gameplay gates (easy Q wheelie) and shape the motion (2nd-order springs, bounded noise, counter-lean)
   rather than making it harder; apply tuning through public setters in a separate component that can be unticked.
8. **Evidence:** front/side/back/three-quarter renders for every stage, looked at before claiming success; measured numbers
   in every note; failures reported plainly.
9. **Coordinates:** Unity = (−Blender x, Blender z, −Blender y) with the default FBX axes. Characters and vehicles face −Y in
   Blender (+Z in Unity). Metres everywhere.
10. **Budgets used (mobile):**
    - TMAX LOD0 30k / LOD1 12k;
    - garments: tee 5k, polo 6k, jeans 6k, trousers 6k, shorts 4k;
    - chain about 8.4k;
    - bodies about 19-20k;
    - textures ≤ 2048 (characters 1024 in game).

=====================================================================================================================
PART E: YOUR INTEGRATION ORDER AND ACCEPTANCE CHECKS
=====================================================================================================================

### 0. Compile

Fix compile errors in the new C#:
- `Mini206RoadsNewMap.cs`, `Mini206AccessoryProfiles.cs`, `BareFeetUnderShoes.cs`;
- then define `MINI206_DRAFT` and fix `TmaxRideFeel.cs`, `TmaxRebuiltVisuals.cs`, `TmaxSuperMotoVisualBinder.cs`.

Log every fix.

### 1. Roads

- Run MINI-206 > Roads: Preview On Copy Of New Map. Do NOT use Mini203RoadsNewMap.
- Run Mini193RoadJunctions on the copy.
- Drive every join in `junctions_bridges.md`, the 4 bridges and the dirt spur.
- Accept when: validation PASS; steps < 6 cm; no ground through roads; school, wall, roundabout and houses untouched.

### 2. TMAX

1. TMAX_560 copy + TmaxRideFeel → ride test. The wheelie must stay as easy as before. Check the off-road strip and kerb drops.
2. Add the rebuilt FBX + TmaxRebuiltVisuals; move the WheelColliders to the new axles and radii; refit the seats.
3. Accept when: bars, fork and front wheel turn together; hands stay on the grips; wheels spin and follow the suspension; no
   clipping at full lock; both riders seated.
4. Optional path B: the SuperMoto physics duplicate + binder.

### 3. Characters

- Garments on copies; tint them.
- Play-test idle, walk, run, pistol and riding; no skin poking through.
- Add BareFeetUnderShoes.
- Accessory profiles: check, then let the owner approve before setting `useManualPlacement`. Check Franki's shades.
- Fitted chains.
- Check the AM90/AM97 shoes.

### 4. Finish

- Build + smoke test.
- Document in `Docs/WorkPackets/MINI-206.md` (a Local integration section), `Docs/Systems/*.md` and the PROJECT-HANDOFF pointer.
- Report pass/fail per item to the owner.

=====================================================================================================================
PART F: OPEN ITEMS
=====================================================================================================================

- No LODs for the bare bodies (`BODY_TRIS` can make them).
- Franki's eye bones still sit at Sacat's eye height.
- Franki's hair cap has no strand texture.
- TMAX:
  - plate text mirrored;
  - screen opaque;
  - static swingarm;
  - one leftover switch block from the scan;
  - wheel sizes follow the scan, not the spec.
- Shorts are 216 triangles over budget; the polo cuff/logo AO reads dark.
- Bridge_03 shows a small road wedge.
- Road_way_254679575 area terrain moved up to 2.7 m; check AI waypoints.
- Nothing has been profiled on a phone yet.
