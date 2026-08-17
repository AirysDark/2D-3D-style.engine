# GE2D3D In-Editor Play Mode

The first Play Mode foundation is now present in `PlayModeController`.

## Intended workflow

1. Open a `.dat` level in the editor.
2. Press Play.
3. Build a runtime scene from the current `LevelInfo`.
4. Run `RuntimeEngine` against that runtime scene.
5. Stop Play and discard the runtime scene, leaving the editor level intact.

## Current status

This commit intentionally adds only the lifecycle foundation. It does **not** yet switch the viewport renderer or input system into gameplay mode. That is the next integration step.

Keeping the lifecycle separate first lets the editor and runtime systems be connected without duplicating the existing map/rendering code.
