# MINI-166 — Fit Codex Air Max models to both characters

User authorization: "put these on the main characters" (2026-09-13), repeated. Applies to the supplied Air Max90/97 Blender previews. Franki defaults to 90 and Sacat to 97; both retain selectable designs and independent saved selections. Preserve Claude's Mike270 and cap/body work.

Status: implemented, verified, rebuilt; final user appearance review remains. Owner released after checkpoint. Scope: a shoe-only integration, no broad wardrobe regeneration. Baseline dirty file: Packages/packages-lock.json, not owned by this task.

Plan: export reduced evaluated Blender meshes with palette materials and real Air cavities; bind each shoe to existing foot rest matrices; replace only matching OutfitPiece entries, preserve IDs and the other slots; visually inspect full characters and feet; exercise both models in idle/walk/run and UI/save regression; update build and process evidence.

Source: C:/Users/PCSS-PC/.codex/visualizations/2026/09/10/01a08aa2-5ac9-7b91-b264-63f520328c7d/airmax-modeling. Retain source guide, geometry and measurements there. No external credits.

Acceptance: both character feet fitted; 90/97 independent; 270 retained; finite skin/bounds and material channels; wardrobe save/load and previews; motion and current-scene evidence; record exact budgets and game-shading deviations.

## Implementation and exact workflow

The original modelling skill/source is now preserved under `Tools/CharacterPipeline/AirMax166Source/`, including both .blend sources, original generator, effective profile CSVs, measurement JSON, skill and failure memory. Those source documents describe the earlier standalone preview; this packet describes the subsequent game integration. Do not confuse historical preview limitations with current verification below.

### Export

`Tools/CharacterPipeline/mini166_airmax_export.py` reads the preserved .blend sources. It never changes them. It exports `Assets/UpIzUpMini/Art/Characters/Garments/AirMax166/AM90.json`, `AM97.json` and two 32x32 PNG palettes.

Blender objects are deselected before per-object conversion. This matters: source .blend selection state can otherwise make a conversion affect several selected objects. Curve bevel resolution is reduced; micro knit curves are omitted at game scale; high-tessellation surfaces receive bounded decimation. Air cavities, internal supports, tongue, collar and laces remain geometry. Degenerate exported triangles are filtered. Positions/normals/UVs and three index groups are flattened to JSON. Palette colours are converted from source linear colour to sRGB bytes and sampled at cell centres, with no mips/compression/filter bleed.

The three exported groups are tintable mesh/suede/silver, fixed foam/rubber/red/laces, and Air bladder. Unity adds a fourth fixed skin group for exposed ankles. The foam uses geometric triangle normals: exported custom/weighted normals after decimation produced visible striped shading in the first Unity check. Fixed opaque material specular/reflection keywords are disabled; bladder highlights remain.

No FBX armature import, extra avatar, runtime JSON parsing or new package is needed. JSON is editor input; generated Mesh assets are the actual runtime assets. Standard transparent shading approximates the Cycles bladder; no real refraction/parity claim is made. Procedural mesh bump from Blender is not baked into the palette.

### Fit and skinning

`Mini166AirMaxIntegration.cs` extends the existing partial Mini166Repair class to reuse its proven Surface/bind-pose and preview helpers without invoking the broad BuildCharacter routine.

The shoe renderer must be an identity child of the character root. Existing bind poses/bone palette are retained. For each foot, forward is the Foot-to-ToeBase vector projected onto XZ; right is cross(up,forward). Source Blender X becomes forward, Y becomes right, Z becomes up. This basis permutation has positive determinant, so triangle winding remains unchanged. Normals use inverse scale before normalization.

Length scale: 1.02. Height scale: clamp(foot-rest Y / .104,1,1.4). Root-space origin: (foot.x,.0038,foot.z) + forward*.085*1.02. Franki height scale approximately 1.15492; Sacat approximately 1.25351. Exact left/right rest positions, forward vectors, bounds and counts are in `Logs/Tasks/MINI-166/AirMaxIntegration/fit-budget.txt`.

Shoe geometry has one full Foot-bone influence per vertex, matching the existing wardrobe shoe binding convention. Low collars revealed missing ankle coverage hidden by the former tall shoes. A small 20-sided, 6-ring skin connector per ankle now spans foot.y-.042 to foot.y+.118, follows the shin rest line, and blends Foot to Leg with smoothstep. Its radii widen from .027/.031 to .035/.039 metres. It uses the existing character-specific skin material; original body, pants and their meshes are unchanged.

Only the existing shoes_mike90 and shoes_mike97 OutfitPiece mesh/material/tint fields are replaced. The generator compares serialized non-target pieces and non-shoe defaults before/after and fails if they differ. Mike270 stays intact. Fresh defaults are Franki90/white and Sacat97/white. Existing saved designs/colours still override defaults normally; the task does not rewrite the user's saved outfit.

The KeepInstalledAirMax hook in Mini166RepairClothes.cs restores these already-generated mesh/material references if the older full wardrobe generator is used later. It does not regenerate or refit them. Future intentional shoe revisions must use AirMaxInstall after updating the preserved/exported source.

### Commands and side effects

All commands use Mini, not `E:\Unity\Up iz up`. Unity was confirmed closed before each new batch process. Background Unity processes use Start-Process -WindowStyle Hidden; their PID is not evidence of completion. Read sentinels/errors and wait for exit. No -nographics is used for render checks.

```powershell
& .\Tools\AIWorkflow\Invoke-Preflight.ps1 -Agent Codex -TaskId MINI-166
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --python Tools/CharacterPipeline/mini166_airmax_export.py *> Logs/mini166-airmax-export.log
```

Unity executable: `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`.

Run with `-batchmode -projectPath "E:\Unity\Up Iz Up Mini"`, the indicated `-executeMethod`, and an absolute `-logFile`. Methods below exit Unity themselves; the compile-only invocation uses -quit instead.

| Method | Side effect / evidence |
|---|---|
| compile only, -quit | script compilation; Logs/mini166-airmax-compile.log |
| UpIzUpMini.EditorTools.Mini166Repair.AirMaxProofBefore | read-only scene, original full/feet pictures; Logs/mini166-airmax-before.log |
| UpIzUpMini.EditorTools.Mini166Repair.AirMaxInstall | writes only new shoe assets and scene wardrobe references/defaults; preserves before scene backup; Logs/mini166-airmax-install.log |
| UpIzUpMini.EditorTools.Mini166Repair.AirMaxProofAfter | read-only saved scene; both models on both characters, static and sampled motion; Logs/mini166-airmax-proof.log |
| UpIzUpMini.EditorTools.Mini166AirMaxValidation.Run | actual Play Mode wardrobe shoe UI, preview, Cancel/Apply, independent save/load; restores prior PlayerPrefs save; Logs/mini166-airmax-tests.log |
| UpIzUpMini.EditorTools.Mini166RepairValidation.Run | existing complete wardrobe/accessory/save regression; restores prior save; Logs/mini166-airmax-regression.log |
| UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer | rebuilds Builds/GrandBayProof/UpIzUpMini.exe; Logs/mini166-airmax-build.log |

Proof images are under `Logs/Tasks/MINI-166/Repair/AirMaxBefore` and `AirMaxAfter`. Camera.Render uses CPU-baked copies with copied indexed material blocks to avoid stale editor GPU skinning. Actual player proof, if completed below, is separate evidence.

### Failures found and corrected

1. Lower collars exposed empty space below the existing hems. Added anatomical ankle connectors instead of distorting the trousers or restoring thick shoes.
2. A test capped the combined two-foot bounds at 1.5m. That is inappropriate for a wide running stride. Revised check independently validates each foot/ankle mesh bounds (maximum .65m magnitude) and finite vertices; whole-character motion is also rendered. The threshold change is not a claim that animation itself was repaired.
3. Fixed sole shading after the first Unity renders showed conspicuous bands. Switched sole export normals to geometric triangle normals and removed fixed-material specular reflections. Air transparency remains separately shaded.
4. A source-copy helper initially read project Markdown with Windows cp1252 and failed on existing Unicode punctuation; rerun with Python -X utf8. No partial project claim edit occurred in the failed read.

### Scope limits

Motion evidence is sampled existing Unity animation clips, not proof of all interactive actions or toe-flex biomechanics. The rigid shoe binding follows Foot; ankle skin blends to Leg. No new locomotion system or avatar changes. Mobile profiling, retopologized low LODs and exact Blender-to-Unity material parity remain outside this integration. Do not certify those from a successful build.

Fresh defaults demonstrate individual shoes; an existing save retains its choices. Both new designs remain available to each character through the existing Shoes tab.


## Final verification, 2026-09-13

- Compile and shoe-only install passed. Backups: Logs/Tasks/MINI-166/AirMaxIntegration/GrandBayProof-before-airmax.unity.
- Focused actual Play Mode tests: MINI166_AIRMAX_TEST_PASS in Logs/mini166-airmax-tests.log. Both models on both characters through actual Shoes UI, portrait mesh match, Cancel/Apply, independent colours/save/load, Mike270 retained. Prior save restored.
- Full existing wardrobe regression: MINI166_REPAIR_TEST_PASS in Logs/mini166-airmax-regression.log. All designs, bones/bounds, tint masks, SaveLoadSystem, preview, accessories and character isolation passed.
- Sampled motion: MINI166_AIRMAX_PROOF_PASS After in Logs/mini166-airmax-proof.log. 12 poses per clip/model/character for walk and run, plus idle/static; 108 images including detail/full/side views. These are sampled animation checks, not interactive locomotion certification.
- Final wider static frames: MINI166_AIRMAX_PROOF_PASS FinalStatic in Logs/mini166-airmax-final-static.log. These only change inspection framing, not the scene. Final sheets use AirMaxFinalStatic images.
- Windows build: Logs/mini166-airmax-build.log, MINI-001 BUILD SUCCEEDED, 437302389 packaged bytes, 9.1316215 seconds. Builds/GrandBayProof/UpIzUpMini.exe.
- Actual built executable: Logs/mini166-airmax-player.log, WARDROBE_LIVE_PROOF_COMPLETE; no Exception/Error matches. Actual GPU-skinned players and wardrobe portrait renders inspected separately. Output files Built-Franki-player.png, Built-Sacat-player.png and corresponding portraits under the integration evidence directory.
- Narrow existing-code diff: one KeepInstalledAirMax call in Mini166RepairClothes.cs; all other new import/validation code lives in Mini166AirMax*.cs. No package changes are included.

Final pair budgets (includes both ankle connectors): AM90 43,178 vertices / 60,260 triangles; AM97 51,816 vertices / 69,920 triangles. Four submeshes/materials per pair (tint, fixed detail, transparent bladder, existing skin); two 32px palette textures shared between characters. These are reduced desktop meshes, still too detailed to assert mobile readiness without profiling/LODs. Preserve recognition before reducing further.

Review: Logs/Tasks/MINI-166/AirMaxIntegration/Main-Characters.jpg, Shoe-Fit.jpg, Motion-Check.jpg. Source measurement and exporter budget are preserved alongside their original files; generated fit report contains all per-foot values. The diagram/contact-sheet producer is Tools/CharacterPipeline/AirMax166Source/review_unity.py.

Built-player command:

```powershell
Start-Process -FilePath 'E:\Unity\Up Iz Up Mini\Builds\GrandBayProof\UpIzUpMini.exe' -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -wardrobe-proof "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-166\AirMaxIntegration\Built" -logFile "E:\Unity\Up Iz Up Mini\Logs\mini166-airmax-player.log"'
```

The proof mode exits automatically and restores the opening wardrobe without saving new choices. Normal play does not run this proof path. An existing save can show a previously selected shoe/colour; select either existing Mike90/Mike97 entry to use the replacement model.
