# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A 2D top-down action RPG demo built with **Godot 4.6** and **C# (.NET 8.0)**, inspired by the Legend of Mir franchise. Core gameplay: click-to-move combat, leveling/skill trees, loot drops, enemy AI.

## Build & Run

1. Open **Godot Engine 4.6 (.NET version)**
2. Import project: `File → Import → select project.godot`
3. Build C#: Bottom toolbar → **MSBuild → Build** (required after any `.cs` changes)
4. Run: **F5** (or Scene → Play)

There are no shell scripts, makefiles, or test commands — all development happens through the Godot editor.

## Architecture

### Global Singletons (Autoloads)
Defined in `project.godot`, accessible from any script as `GameManager.*` or `SkillManager.*`:

- **`GameManager`** — Item database (30+ items), inventory management, weighted loot drop table, exp multiplier
- **`SkillManager`** — Skill library (12 skills), cooldown tracking, active/passive skill effects

### Scene Hierarchy
- **`Main.tscn`** — Root scene; contains the map, Player instance, MonsterSpawner, and CheatPanel
- **`Player.tscn`** — CharacterBody2D + Camera2D + HUD (CanvasLayer with stats/inventory/skills/hotbar panels)
- **`Enemy.tscn`** — Monster template used by MonsterSpawner
- **`DamageText.tscn`** — Floating damage number effect

### Key Script Responsibilities

| Script | Role |
|--------|------|
| `Player.cs` | Input handling (click-to-move, skill casting), combat, leveling, UI toggles |
| `Enemy.cs` | AI state machine (Idle → Wander → Chase → Attack), monster separation forces |
| `MonsterSpawner.cs` | Spawns normal enemies every 1.5s (max 100) and boss every 60s |
| `CharacterAnimator.cs` | Procedural sprite animation (walk bounce, idle pulse, attack flash) — no sprite sheets used |
| `InventoryUI.cs` / `SkillUI.cs` / `SkillHotbar.cs` | HUD panels attached to Player's CanvasLayer |

### Data Models
`ItemData.cs` and `SkillData.cs` are Godot `Resource` subclasses used as data containers. Item and skill definitions live as inline objects inside `GameManager` and `SkillManager` respectively.

### Player Controls
- **Left-click**: Move to position or select/attack enemy target
- **1–5**: Cast skills from hotbar
- **B**: Toggle inventory
- **K**: Toggle skill panel
- **F12**: Toggle cheat panel (exp multiplier)

## C# / Godot Conventions

- Root namespace: `LegendOfMirDemo`
- Godot node references are assigned via `[Export]` attributes or `GetNode<T>()` in `_Ready()`
- Singletons accessed as: `GameManager gameManager = GetNode<GameManager>("/root/GameManager");`
- Use `GD.Print()` for debug logging (no logging framework)
- The `.godot/` directory is auto-generated — do not manually edit files there
