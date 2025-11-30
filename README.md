
GE2D3D Map Editor

Unified Editor for the GE2D3D Engine

Project Overview • Architecture • UI Layout • Roadmap (2025 Edition)

The GE2D3D Map Editor is the official world, entity, and scene creation tool for the GE2D3D rendering engine.
This document summarizes everything: history, current architecture, UI design, and the future direction.


---

📌 1. Project History

The project was started to replace a collection of outdated, partial, or broken tools (legacy map editors, prototype renderers, and test scenes) with a single modern editor that:

Loads levels in the GE2D3D engine format

Provides real-time rendering (MonoGame host)

Offers 3D navigation, selection, manipulation

Lets level designers place entities, lights, props, and triggers

Supports skyboxes, camera paths, collision layers

Supports future GE2D3D game engines


Initial builds had:

MonoGame render host incomplete

No integrated Skybox UI

Entities/Selection/Camera Path tools hardwired into SceneView

Menu system minimal (only File, Edit)


The 2025 overhaul moved the project into a clean, modular, production-ready structure.


---

📌 2. Editor Architecture

✔ Prism / MVVM

The editor uses Prism + MVVM for:

Command bindings

DataContexts

ViewModel separation

Future module expansion


✔ Modules

The main editor logic lives in:

GE2D3D.MapEditor/
    Modules/
        SceneViewer/
            Views/
            ViewModels/
            Commands/
            Panels/

✔ RenderBootstrap

The real-time renderer is provided by a MonoGame-based host embedded into WPF.
Responsibilities:

Loading geometry

Rendering lights, props, skybox, collision meshes

Responding to camera input

Showing debug overlays, bounds, and selection outlines

Handling anti-aliasing (None, FXAA, MSAA, SSAA)


✔ SceneViewModel

This single ViewModel powers:

Prefab list

Entity list

Current selection

Camera path

Layer visibility

Skybox configuration

Snap settings

Anti-Aliasing mode

Undo/Redo system


Every tool window shares this same ViewModel instance.


---

📌 3. UI Design (Old vs New)

❌ Old UI Problems

Before the overhaul:

Too many panels were embedded inside SceneView

Editor was cluttered and cramped

Assets, Entities, Selection, Camera Path were stuck at the bottom

Anti-aliasing combo box sat inside the toolbar

No consistent windowing system

Harder for contributors to understand layout


This made the editor feel like a prototype instead of a professional tool.


---

✔ 4. New UI System (2025)

🎉 Introduced: Toolbox Menu

A fully modular main menu:

File | Toolbox

Toolbox contains:

🔷 Skybox

Skybox Inner

Skybox Outer


🔷 Tool Windows

Assets

Camera Path

Selection Inspector

Entities List


🔷 Layers

Geometry

Grid

Collision

Props

Lights

Triggers


🔷 Anti-Aliasing

None

FXAA

MSAA

SSAA


🔷 (Future: Environment, Materials, Terrain)


---

🎉 Introduced: Floating Tool Windows

Instead of forcing everything into SceneView, the following are now independent windows:

AssetsWindow — Prefab list

EntitiesWindow — All placed entities

SelectionWindow — Inspector for selected object

CameraPathWindow — Keyframes, smooth curves, tools


Properties:

✔ Can all be open at once
✔ Moveable / dockable by OS
✔ Share the same ViewModel
✔ Cleaner main editor view


---

🎉 SceneView Simplified

Before → cluttered.
After → clean viewport.

SceneView now contains only:

✔ Top Toolbar
✔ MonoGame Render Host

Nothing else is docked inside it.


---

📌 5. Top Toolbar (Retained)

The following items remain at the top of the editor:

Undo / Redo

Camera Presets (Top / Front / Side)

Focus Selection

Reload Level

Grid Snap

Rotation Snap


These are universal and should always be visible.


---

📌 6. Anti-Aliasing System

Moved from a toolbar combo box → Toolbox → Anti-Aliasing.

Modes:

None

FXAA

MSAA

SSAA


The MonoGame host updates instantly through:

RenderBootstrap.SetAntiAliasing(mode);


---

📌 7. Prefab / Assets Notes

PrefabListItem exposes:

ModelId (read-only)

Name (read-only)

Category (read-only)


Bindings must be Mode=OneWay, or WPF will throw.


---

📌 8. Goal of the New UI Layout

The new structure prepares the editor for full professional development, similar to:

Unity Inspector

Unreal Outliner

Godot docks

Blender tool panels


Benefits:

Clean viewport

Modular windows

Easy to add new tools

Consistent UX

Better for large maps and scenes

Future support for docking frameworks



---

📌 9. Future Roadmap

🔮 Rendering

Material editor

Shader previewer

Sky/Weather control

Light baking test mode


🔮 Tools

Terrain sculpting

Trigger editor upgrade

Animation timeline viewer

Prefab editor (create new prefabs inside editor)


🔮 UI

Dockable panels (AvalonDock or custom docking)

Theme support (dark/light)

Custom icons

Search bars across windows


🔮 Engine Integration

Live reload

Play-in-editor

Script watchers (C#, Lua, JSON)



---

📌 10. Contributor Guide

Folder Structure

GE2D3D.MapEditor/
    MainWindow.xaml       (menu + tool window creation)
    SceneView.xaml        (main viewport)
    Modules/
        SceneViewer/
            Views/
            ViewModels/
            Commands/

Adding a new tool window

1. Create XAML window in Views/


2. Bind to ViewModel


3. Add menu item under Toolbox


4. Add click handler in MainWindow


5. Create _myToolWindow field for reuse


6. Window will share DataContext automatically



Coding style

Use MVVM where possible

Use OneWay bindings for read-only VM properties

Keep tool logic OUT of SceneView

Never put heavy logic inside code-behind unless necessary (AA, window spawning)

Keep the MonoGame renderer isolated inside the GameHost controller

Document any new commands in SceneViewModel



---

📌 11. Summary

The GE2D3D Map Editor is now structured like a real modern editor:

✔ Clean and modular
✔ Easy to extend
✔ All tools in Toolbox
✔ All inspectors as windows
✔ SceneView focused on rendering only
✔ Stable bindings
✔ Clear folder structure

This README provides a full foundational understanding for anyone joining the project now or in the future.
