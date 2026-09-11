# Build & Verification

## Current state

This is the meta-system every other system's ledger points back to: how to compile-check, batch-verify, and build this project without a human driving the Unity Editor GUI. It exists because this project has repeatedly proven that "compiles clean" and "a batch-mode check passes" are NOT the same thing as "works in real gameplay" - both gaps have real, documented history (see `Vehicles.md`/`Combat.md`/`Characters.md`'s own ledgers).

## Architecture

- **Compile check**: run Unity in batch mode with no `-executeMethod`, just import/compile the project and exit. Catches C# errors before anything else runs.
  ```
  Unity.exe -batchmode -nographics -quit -projectPath "<path>" -logFile "<log path>"
  ```
  Then grep the log for `error CS`.
- **One-off diagnostic/patch tools**: the established pattern (see the dozens of `Mini1##*.cs` files under `Assets/UpIzUpMini/Editor/`) is a small static class with a `[MenuItem(...)]` method, invoked via:
  ```
  Unity.exe -batchmode -nographics -quit -projectPath "<path>" -executeMethod Namespace.ClassName.MethodName -logFile "<log path>"
  ```
  Convention: these are NOT deleted after use (despite what some of their own header comments say) - they accumulate as a searchable record of exactly what was measured/fixed and how. Read an existing one before writing a new one that does something similar.
- **Rendering a visual check without Play Mode**: `AnimationClip.SampleAnimation(gameObject, time)` poses a character directly (bypasses the Animator/root-motion pipeline entirely - a real caveat, see `Combat.md`'s render-tool notes about root motion drift). Combined with a throwaway `Camera` rendering to a `RenderTexture` and `Texture2D.EncodeToPNG()`, this lets an agent SEE a pose/scene state without a human present. **Must run WITHOUT `-nographics`** (that flag disables the GPU context `Camera.Render()` needs - a real, reproduced failure mode this session, not a guess: `-nographics` + `Camera.Render()` = segfault).
- **Windows build**: `UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer`, output `Builds/GrandBayProof/UpIzUpMini.exe`. Check `ProjectSettings/` for stray diffs before building (a benign `DynamicsManager.asset` diff has shown up before and should usually be reverted, not shipped). After building, confirm freshness by checking that `Builds/GrandBayProof/UpIzUpMini_Data/level0` (and `Managed/Assembly-CSharp.dll` if code changed) has a timestamp AFTER the last relevant scene/code change - the launcher `.exe` stub itself can have unchanged bytes and a stale-looking timestamp even on a successful fresh build.
- **Standing convention (see `phase-testing-workflow` memory)**: build an exe after every meaningful phase and hand it to the user - a compile or even a passing batch check is never sufficient sign-off on its own for anything visual, animated, or feel-based.

## What worked / what didn't

- **(2026-08-29) Read the user's real `Player.log` directly from disk instead of asking them to paste it.** Standalone builds write to `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Player.log` (company/product name from `ProjectSettings/ProjectSettings.asset` - this project is `DefaultCompany`/`Up Iz Up Mini`). If the agent and the build run on the same machine, this file is directly readable and is the fastest, most reliable way to diagnose any "doesn't work in the real build" report - grep it for whatever diagnostic `Debug.Log` lines the suspect code already emits, or add one and ask for a re-test.
- **(2026-08-29) A Unity `AnimatorController` state added via script (`AnimatorStateMachine.AddState()`) can pass every Editor-mode check - `HasState` true, correct `Motion`, same asset instance ID the Player uses - yet still not work in an actual BUILT Player**, while an older state added the same way long ago (through many prior builds) works fine. Suspected stale build-time bake specific to states that are brand new as of the most recent build. Attempted fix: `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)` + `AssetDatabase.Refresh()` right after the patch, before building - `AssetDatabase.SaveAssets()` alone was not enough. See `Combat.md`'s matching ledger entry for whether this was confirmed to actually fix it.
- **(2026-08-28) `-nographics` + any real `Camera.Render()` call segfaults Unity outright** - not a graceful failure, a crash (exit code 139). Drop `-nographics` specifically for any tool that renders to a texture; keep it for pure compile/logic checks where it's harmless and faster.
- **(2026-09-08, MINI-158, re-confirmed) `-nographics` + `Camera.Render()` doesn't always crash - it can instead exit 0 and silently write flat solid-color PNGs** (this session: proved with a plain diagnostic cube through the render path, not just assumed). Re-running the exact same tool with only `-batchmode` (no `-nographics`) produced real, correct images immediately - same command, same log-clean exit either way, so a clean exit code is NOT enough to trust a render tool's output. Always eyeball at least one output image (or diff it against a known-blank flat color) before trusting a batch-mode render, and always try dropping `-nographics` first if a render tool comes back suspiciously blank/flat rather than assuming the tool or the asset is broken.
- **(2026-08-28) `AnimationClip.SampleAnimation` bypasses `Animator.applyRootMotion` entirely** - even when the real game always plays a clip through an Animator with `applyRootMotion=false` (so it never visually drifts in actual gameplay), directly sampling the SAME clip for a diagnostic render can still show root-motion drift, because that direct-sample path never goes through the Animator/root-motion system that suppresses it at runtime. Frame diagnostic-render cameras on the character's actual rendered bounds (recomputed after sampling), not a fixed offset from its static transform, so a drifted sample doesn't fall out of frame.
- **(2026-08-28) A scripted Editor batch-mode test cannot verify anything gated by `UnityEngine.Time.time` advancing across multiple calls** - Time.time does not tick outside Play Mode. A cooldown-gated action (e.g. a second `Attack()` call) will appear to silently do nothing in such a test, which is a tooling limitation, not proof the underlying code is broken. Structural/first-call verification plus a request for the user's real Play-mode test is the correct fallback, not further blind batch-mode workarounds.
- **(historical, repeated across this whole project) Reflection is often needed to force a MonoBehaviour lifecycle method that Editor batch mode never calls automatically** - `Awake()` on singletons (`CharacterSwitchManager.Instance` is only set in its own private `Awake()`), etc. The pattern: `typeof(T).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, null)`.
- **(historical) A duplicate/ghost component can silently exist alongside the real one** and cause exactly the kind of "should be simple, mysteriously isn't" bug that costs the most diagnosis time (see `Vehicles.md`'s ghost-Animator entry). When something that should obviously work doesn't, checking for an unexpected SECOND instance of a component (`GetComponents<T>()`, not `GetComponent<T>()`) is a cheap, high-value first move.

## Open items

- None specifically logged as of this system file's creation (2026-08-29) - this file itself IS the fix for the previously-implicit "how do you actually verify things on this project" knowledge that lived only in scattered commit messages.

## Key files

- `Tools/AIWorkflow/` - where compile/check/build logs land (gitignored - evidence, not shipped content).
- `Assets/UpIzUpMini/Editor/Mini001Build.cs` - the Windows build entry point.
- Any `Assets/UpIzUpMini/Editor/Mini1##*.cs` file - read a few from the system you're about to touch before writing a new diagnostic tool; the pattern is consistent across all of them.
