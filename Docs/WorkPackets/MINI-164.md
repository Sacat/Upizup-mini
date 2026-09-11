# MINI-164 — Wardrobe combination contact sheet + BLACK hair for both characters

```yaml
task_id: MINI-164
title: Show available wardrobe combinations; start Sacat hair; make both characters' hair black
request_owner: User
integrator: Claude (setup only) -> handed to Codex/ChatGPT 2026-09-10 at user request
status: partly_started_handed_off
approval_class: C
budget:
  claude_time: one session, iterate on renders until hair reads black and clean
  external_credits: 0
  stop_condition: do not integrate a visibly broken hair result into the live scene
reserved_files:
  - Tools/CharacterPipeline/mini164_franki_hair.py
  - Tools/CharacterPipeline/mini164_sacat_hair.py
  - Assets/UpIzUpMini/Editor/Mini164WardrobeCombos.cs
  - Assets/UpIzUpMini/Editor/Mini164IntegrateHair.cs
  - Assets/UpIzUpMini/Art/Characters/Garments/Franki_Hair.fbx
  - Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Hair.fbx
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Docs/WorkPackets/MINI-164.md
  - Docs/Systems/Characters.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - Logs/Tasks/MINI-164/
depends_on:
  - MINI-163 (Franki hair shipped but still shows a pale fade band + shattered crown; Sacat hair attempted and rejected)
  - MINI-159 (reshaped shirt/pants baked into the live Sacat/Franki)
```

## Part 1 — Wardrobe combination contact sheet

The wardrobe system today (`CharacterEquipment.cs`) is tint-only recolour of
the existing mesh slots plus three accessories. The real, currently-available
"combinations" are permutations of:

- cap (`cap_mike`) on / off
- shades (`shades_ray`) on / off
- watch (`watch_rollie`) on / off
- shoe colour: `shoes_mike` (white) vs `shoes_pumba` (red)
- baked reshaped shirt/pants from MINI-159 (navy top / charcoal pants) — fixed

Deliverable: `Mini164WardrobeCombos.cs` opens `GrandBayProof.unity`, equips a
spread of these combos on the live Sacat and Franki via the trial API, renders
each to a PNG, and writes a contact sheet under `Logs/Tasks/MINI-164/`.

### Acceptance
- [ ] One image per combination, both characters, full-body + head crop
- [ ] Contact sheet montage the user can scan at a glance
- [ ] No scene save — trial API only, scene restored

## Part 2 — Black hair, both characters

User: *"no one hair should be white. black hair and show me with screenshots"*.

Current state (from MINI-163 renders):
- **Franki**: shipped hair has a visible pale skin-tone fade band across the
  mid-scalp and a shattered/noisy wavy crown (wave displacement too strong).
- **Sacat**: no real hair mesh — a glossy dome baked into `Ch06`. The MINI-163
  transplant of Franki's hair shape onto his head was distorted and rejected.

### Approach
- **Franki** (`mini164_franki_hair.py`): drop the skin-tone `FADE_COLOR` band —
  make the whole hair one flat near-black material. Cut the wave displacement
  amplitude right down (or remove it) so the crown reads as a clean short cut,
  not noise. Re-export `Franki_Hair.fbx`.
- **Sacat** (`mini164_sacat_hair.py`): low-risk first step — retexture the baked
  `Ch06` dome to a **matte black** flat material (own material index, no shared
  atlas pixels, per MINI-163's headphone-corruption lesson). Kill the glossy
  plastic highlight (roughness up). This gives him black hair immediately with
  zero fitting risk. Do NOT ship the bounding-box hair-mesh transplant.
- Integrate via `Mini164IntegrateHair.cs` — bone-remap-by-name, only the hair /
  head-dome renderer touched, every other renderer confirmed unchanged by name.

### Acceptance
- [ ] Franki hair reads solid black, no pale band, clean crown — verified by render
- [ ] Sacat head-dome reads matte black (black hair), headphones/cap unaffected — verified by render
- [ ] Compile clean
- [ ] Screenshots delivered to the user
- [ ] Live scene integration only if both read clean; otherwise deliver renders and stop for approval

## Handoff state (2026-09-10 — user stopped the session to give this to Codex)

Done:
- This packet.
- `Tools/CharacterPipeline/mini164_franki_hair.py` — **run**. Produced
  `Assets/UpIzUpMini/Art/Characters/Garments/Franki_Hair.fbx`: whole `Ch28_Hair`
  mesh on ONE flat near-black material (0.020), no fade band, no wave
  displacement, 2 Laplacian smooth passes on the crown. Triangulated, exported
  with the armature.
- `Tools/CharacterPipeline/mini164_sacat_hair.py` — **run**. Produced
  `Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Hair.fbx`: the baked `Ch06`
  dome (5481 material-0, `mixamorig9:Head`-dominant faces above ear-top
  z=1.6661) reassigned to a new matte-black material `Ch06_Hair_Black`
  (roughness 0.78). **Retexture only — no geometry transplant** (MINI-163's
  transplant was rejected). The FBX contains the full `Ch06` mesh.
- `Tools/CharacterPipeline/mini164_render_hair.py` — standalone Blender eyeball
  render helper. **Not run yet.** Invoke:
  `blender -b -P Tools/CharacterPipeline/mini164_render_hair.py -- <fbx> <tag>`

Not done — next steps for Codex:
1. Eyeball both FBXs (mini164_render_hair.py, or straight in Unity). Confirm
   Franki reads solid black + clean crown, and Sacat's 5481-face dome selection
   didn't swallow the whole upper head / headphones.
2. Write `Assets/UpIzUpMini/Editor/Mini164IntegrateHair.cs` — copy
   `Mini163IntegrateFrankiHair.cs`. Franki: swap `Ch28_Hair` mesh/materials/bones
   by name. Sacat: swap the live `Ch06` renderer's `sharedMesh` +
   `sharedMaterials` + `bones` by name (the new mesh has an extra black-dome
   submesh). Confirm every other renderer unchanged by name. Preview render
   before saving.
3. Write `Assets/UpIzUpMini/Editor/Mini164WardrobeCombos.cs` — Part 1 contact
   sheet (see above; trial API only, no scene save).
4. Compile check, integrate only if clean, Windows build, deliver screenshots.

Both `Franki_Hair.fbx` and `Sacat_Hair.fbx` were overwritten from their MINI-163
versions. MINI-163's rejected Sacat transplant FBX is gone — intended.

## Evidence
(to be filled in as the pass runs — `Logs/Tasks/MINI-164/`)

## Codex continuation 2026-09-10
User explicitly requests short wave texture under removable caps on both characters. Reserve Mini164WaveHair.cs, Garments/Waves assets, the live scene and hair documentation. Preserve body/clothes by deriving Sacat scalp from the live mesh; do not swap in the full original-body FBX. Budget: local procedural texture and fixed Unity previews, no paid generation. Claim MINI-164 for hair only; wardrobe contact sheet remains a separate outstanding part.

## Codex hair result — 2026-09-10

Latest user scope: continue both characters' short black waves so removing a cap exposes wave texture. Prior task read: "Fix dull textures and wall lighting"; user's short waves / clean shape-up request preserved. This continuation implements the hair portion; the old wardrobe-combination contact sheet remains outstanding.

Implementation:
- New editor tools: Assets/UpIzUpMini/Editor/Mini164WaveHair.cs and Mini164WaveValidation.cs, with Unity-generated metas.
- New assets under Assets/UpIzUpMini/Art/Characters/Garments/Waves: BlackWaves.png, BlackWavesNormal.png, BlackWaves.mat, FrankiWaves.asset, SacatWaves.asset, SacatBeforeWaves.asset, and metas; Waves folder meta.
- GrandBayProof.unity: only the two targeted hair/body renderer mesh, material and bounds assignments are intentionally updated. Original scene backup: Logs/Tasks/MINI-164/GrandBayProof-before-waves.unity.
- Franki: replaces broken/gapped cards with a 3,072-triangle continuous shell, head-weighted, ray-fitted against his existing skull. Sacat: preserves all live original vertex positions/weights/body UVs, removes only head-dominant triangles from the fused-cap submesh, adds a 3,072-triangle head-weighted scalp shell. Baseline mesh allows repeatable integration without replacing repaired clothes.
- One shared material, two 512x512 mipmapped textures; no runtime generator, added lights, colliders, or network services. Procedural wave maps are locally authored; existing character source assets remain the basis.
- Uses the project's available Standard shader (URP Lit was not available through Shader.Find). No rendering-pipeline changes.

Verification (Unity 6000.3.10f1):
All commands use `-batchmode -projectPath "E:\Unity\Up Iz Up Mini"`, without `-nographics` for image captures.
1. `-executeMethod UpIzUpMini.EditorTools.Mini164WaveHair.Preview`: compile and iterative previews. Early rejects caught gapped cards, headphone selection, scalp overhang, stale GPU buffers. Final code fits Franki to skin and uses smooth Sacat shell; no geometry displacement for waves.
2. `-executeMethod UpIzUpMini.EditorTools.Mini164WaveHair.Integrate -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-164\waves-integrate.log"`: MINI164_WAVES_INTEGRATED.
3. `-executeMethod UpIzUpMini.EditorTools.Mini164WaveHair.Evidence -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-164\waves-saved-evidence.log"`: MINI164_SAVED_EVIDENCE_PASS. Saved meshes reloaded and rendered; images inspected. Fixed 800x800 front/side/crown/full captures, neutral grey environment, 1.2 intensity directional key and .55 ambient, generated only in unsaved proof scene.
4. `-executeMethod UpIzUpMini.EditorTools.Mini164WaveValidation.Run -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-164\waves-test-build.log"`: real Play Mode MINI164_CAP_REMOVE_PASS on both characters; cap appears, removes after a frame, mesh/material remain. Then Windows build succeeded (410,853,365 bytes). No broad scene regeneration used.
5. Standalone `Builds/GrandBayProof/UpIzUpMini.exe -batchmode -nographics -logFile .../waves-player-smoke.log`: 15-second smoke (result appended below).

Evidence filenames: Logs/Tasks/MINI-164/{Franki,Sacat}-Waves-{NoCap,Cap}-{Front,Side,Crown,Full}.png. Full view is a distance check, not device profiling or live motion acceptance. Cap trial proof records the pre-existing too-low cap; it is not an approved accessory placement. Hair art still requires user review, especially Sacat's forehead/temple edge. No screenshot is claimed as live motion proof.

Known limitations: existing cap prototype fit, headphones, eyelashes and garments are outside this hair change. The original wardrobe-combination contact sheet is still outstanding. Existing unrelated dirty changes were preserved. No blanket checkpoint commit was made because the live scene and documentation include extensive earlier uncommitted work; generated assets depend on that working state.


Final smoke: SMOKE_ALIVE_15_SECONDS; no Exception/NullReference/Error/Failed matches in waves-player-smoke.log. Ownership released to None. User review requested; no new appearance lock assumed.
