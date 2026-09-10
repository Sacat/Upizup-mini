# MINI-165 — Removable headphones

User requests that Sacat's existing headphones can be removed and worn as an accessory. Codex owns integration. Preserve the existing wave hair, face, garment repairs and headphone appearance; separate the fused headphone triangles into a skinned accessory without moving them.

Reserved: CharacterEquipment.cs; VisualWardrobePanel.cs; new Mini165Headphones.cs / Mini165HeadphoneValidation.cs; Garments/Headphones assets and metas; GrandBayProof.unity; task/current/system/handoff/changelog documentation.

Budget: local geometry separation, existing wardrobe integration, Unity compile/runtime tests and fixed visual proof; no paid assets. Acceptance: headphones toggle on/off in wardrobe, Cancel restores opening state, Apply persists through existing wardrobe save data, wave scalp remains visible, no missing skin, existing accessories work. Existing cap fit remains outside scope. Evidence and result to follow.

User clarification: headphones are a head accessory like a cap, not part of the character's permanent hair. Existing fitted geometry is preserved as a separately toggleable skinned object.

## Implementation and evidence

- Changed: CharacterEquipment.cs (serialized accessory reference, Wear/Remove state, existing wardrobe save integration); VisualWardrobePanel.cs (button, opening-state restoration on Cancel/Restore).
- New: Mini165Headphones.cs / Mini165HeadphoneValidation.cs and metas; Garments/Headphones folder and mesh assets (SacatWithHeadphones baseline, SacatWithoutHeadphones body, SacatHeadphones accessory); task packet.
- Scene: GrandBayProof.unity receives a separate HeadphonesAccessory skinned renderer at exactly the original renderer transform, and the equipment reference. Backup: Logs/Tasks/MINI-165/GrandBayProof-before-headphones.unity.
- No asset deletions, added textures, geometry generation, lighting or scale changes. Six existing components / 6760 triangles moved from body draw to one accessory draw using the same material. Body vertex stream retained; other submesh index buffers asserted identical. The accessory is removable independently of the cap; hair remains permanent.
- Budget: one additional skinned renderer/material draw per equipped Sacat, no added triangles/textures. Original vertices kept in baseline/body for safe repeatability; no mobile hardware profiling claimed.

Exact Unity executable: C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe
Common arguments: -batchmode -projectPath "E:\Unity\Up Iz Up Mini"
1. -executeMethod UpIzUpMini.EditorTools.Mini165Headphones.Preview -logFile "E:\Unity\Up Iz Up Mini\Logs\mini165-preview-2.log": compile and on/off previews PASS. First preview exposed Unity fake-null component semantics; corrected with explicit component null check.
2. -executeMethod UpIzUpMini.EditorTools.Mini165Headphones.Integrate -logFile "E:\Unity\Up Iz Up Mini\Logs\mini165-integrate.log": MINI165_INTEGRATED.
3. -executeMethod UpIzUpMini.EditorTools.Mini165HeadphoneValidation.Run -logFile "E:\Unity\Up Iz Up Mini\Logs\mini165-test-build.log": real Play Mode MINI165_HEADPHONES_PASS — repeated wear/remove, unchanged hair mesh/material, actual GameSave JSON roundtrip, null legacy defaults, wardrobe Cancel/Apply, character isolation. Then Windows build succeeded: 411132437 bytes.
4. -executeMethod UpIzUpMini.EditorTools.Mini165Headphones.Evidence -logFile "E:\Unity\Up Iz Up Mini\Logs\mini165-saved-evidence.log": MINI165_SAVED_EVIDENCE_PASS; source scene reloaded and on/off images captured. Fixed 800x800 front/side/crown/full views; neutral background, .55 ambient, 1.2 directional light; no lighting changes saved. Preview images inspected: ears, face and wave scalp remain intact with headphones off.
5. Built player ran its existing -wardrobe-proof harness, exited 0 with WARDROBE_LIVE_PROOF_COMPLETE and no Exception/Error matches. Both batch and non-batch hidden captures were black; they are explicitly rejected as UI visual proof. The button layout still requires hands-on review. Neither attempt altered the user's save file.

Evidence: Logs/Tasks/MINI-165/Sacat-Headphones-{On,Off}-{Front,Side,Crown,Full}.png. UI capture failures: Wardrobe.png and Wardrobe-Rendered.png; player logs retained alongside them.

Future maintenance: do not rerun the older MINI164 wave integrator on Sacat without preserving the MINI165 separated headphone body; its earlier baseline still has fused headphones. MINI165 stores its own baseline and repeatable split.

Checkpoint scope: only this task's previously clean runtime scripts, new editor tools, new headphone assets and new packet are eligible for staging. The scene and shared documentation contain earlier dirty work and are intentionally left unstaged. User visual acceptance is still pending; no new visual lock assumed.
