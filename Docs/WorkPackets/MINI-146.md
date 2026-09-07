# MINI-146 - Approved watch purchase and home wardrobe foundation

## Completion evidence

Implemented and owner released 2026-09-07. Final Logs/MINI-146-wardrobe-final.log: validation PASS, Windows build SUCCEEDED (398087331 bytes); no edit-mode Destroy errors after instance cleanup guard. Logs/Tasks/MINI-146/validation.txt details isolation/ownership/home gating/JSON compatibility. player-smoke.log: survived 12 seconds, zero error/exception matches. Scene backup GrandBayProof-Before-WatchIntegration.unity retained there. Protected transforms and profile hashes unchanged. Sacat/Franki-Purchased-Watch.png show actual purchase path; no live IMGUI or full PlayerPrefs save/reload test. User test: buy independently, home E -> 5 -> 1/2, save, reload, switch protagonists, inspect during movement. Full garments, colour UI and body replacement remain out of scope.

User accepted exact MINI-145 whole-wrist placement ('yes perfect') and approved next: individual purchase and home wardrobe.
Owner/integrator Codex. Status implemented, awaiting user playtest. External credits0. Scope: existing watch_rollie purchase uses approved mobile prefab/profile per protagonist; owned-safehouse wardrobe wear/remove; selections persist per character in existing save. Old saves retain ownership and default existing auto-equipped behavior.

Protect all chains, approved watch mesh/material/scale/pose profiles, body models, controls outside home menu, missions, map transforms and shop price/unlock. Patch only two equipment references on each live protagonist, never regenerate scene. Make backup first.

Non-goals: full clothing catalogue, new base bodies/rigs, colour selection UI, shirt/shoe generation, Android certification. This is the first reusable equipment-selection save layer with watch-only UI.

Validation: per-character purchase, remove/wear, lack-of-ownership refusal, resale, serializable wardrobe restore incl. missing old fields, safehouse ownership/proximity, protected chain/profile/scene-transform checks, compile/build/smoke and screenshot of real equipment path. No user save overwritten by tests. Stop for playtest after one build.
