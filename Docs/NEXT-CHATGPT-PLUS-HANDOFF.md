# Up Iz Up Mini — next ChatGPT Plus handoff

Copy the prompt below into the next ChatGPT Plus/Codex task. The project itself contains the authoritative details; this prompt deliberately stays compact to save tokens.

> Continue **Up Iz Up Mini** at `E:\Unity\Up Iz Up Mini`.
>
> Before changing anything, read these files completely and follow them in order:
> 1. `.agents/skills/upizup-mini-production/SKILL.md`
> 2. `AGENTS.md`
> 3. `Docs/CURRENT.md`
> 4. `PROJECT-HANDOFF.md` — read the `Current claim` block and search for `MINI-102`; do not load all old history unless needed
> 5. `Docs/AI-PRODUCTION-WORKFLOW.md`
> 6. `Docs/WORLD-EXPANSION-WORKFLOW.md` for any map work
>
> The latest bounded work is `Docs/WorkPackets/MINI-102.md`. It moves the user-drawn Backstreet to the **south/below Lalay**, removes the accidental north version, keeps shops and mission characters out of the paved travel lanes, places Boss J/Normy/police on short Lalay walking beats, gives Dog Life and Not Ah Word distinct Lalay blocks, adds Brakes in white at the church, keeps Paro on Lalay, and places Boat Man plus the boat at the jetty. It also adds a GTA-style transparent minimap with mission-aware blips and a red/blue police overlay from 50% heat.
>
> Do not rename save-facing compatibility IDs such as `MontineFarm` or `land_montine`; all visible text must say **Highland**. The generated scene source is `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs`; manual edits to `GrandBayProof.unity` will be overwritten. The map rollback is commit `aed8854`, and the district manifest is `Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/MANIFEST.json`.
>
> First ask for or inspect the user's MINI-102 EXE feedback. Do not begin new visual/world changes if that feedback is still pending. The essential test is: Backstreet joins at both ends and is drivable; Lalay lanes are clear; Highland connection is gentle; Boss J/Normy/police walk without collisions; gangs respawn correctly; Brakes, Paro, Boat Man and the boat are interactable; minimap follows Sacat/Franki and its 50% heat overlay is readable; companion and vehicles can travel between Lalay and Highland.
>
> Preserve user-locked manual character/accessory work, especially Sacat's chain. Never edit `Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat` unless the user explicitly unlocks it. Use `apply_patch`, reserve exact files, run preflight, rebuild only after compounded changes, generate fixed screenshots, show important screenshots to the user, update the work packet/handoff, and commit only task-owned files.

After the MINI-102 runtime feedback is resolved, the recommended next systems task is the existing low-budget production sequence in `Docs/CURRENT.md`: input facade/mobile contract, asset-size gate, deterministic runtime capture, then one approved Hitem3D modular-character proof. Do not spend Hitem3D credits before the user approves the reference card.
