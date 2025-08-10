# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity Editor Tool package called "Favorite Assets" that allows users to mark Unity assets and folders as favorites for quick access. The tool provides a dedicated window to view and manage favorite assets with context menu integration.

## Key Architecture Components

### Core Classes
- **FavoriteAssetData**: Serializable data class containing asset metadata (path, name, type, GUID, date added)
- **FavoriteAssetsDataManager**: Static class handling all data persistence, loading/saving to JSON in `Application.persistentDataPath/Editor/FavoriteAssetsData.json`
- **FavoriteAssetsWindow**: EditorWindow providing the main UI using Unity's UIElements system
- **FavoriteAssetsContextMenu**: Handles right-click context menu integration in Project window

### Data Flow
1. Users right-click assets in Project window → Context menu appears
2. "Add to Favorites" calls `FavoriteAssetsDataManager.AddFavorite()`
3. Data is persisted to JSON file and window refreshes
4. Window displays favorites with icons, names, paths, and type labels
5. Single-click selects asset in Project, double-click opens it

### UI Architecture
- Uses Unity UIElements (not IMGUI)
- Styled with USS files located in `Editor/Resources/`
- Implements toolbar with refresh/clear buttons and asset count
- Scrollable list view with empty state when no favorites exist

## Unity Project Structure

- **Unity Version**: 2022.3.62f1
- **Package Location**: `Packages/FavoriteAssets/`
- **Assembly Definition**: Editor-only assembly with Unity Editor references
- **Dependencies**: Standard Unity modules and packages (no custom external dependencies)

## Development Commands

This Unity project uses standard Unity Editor workflows:
- Open project in Unity Editor (no build commands needed for editor tools)
- Code changes are automatically compiled by Unity
- Testing is done within Unity Editor by using the tool
- Access the window via `Window → Favorite Assets` menu

## Package Structure
```
Packages/FavoriteAssets/
├── Editor/
│   ├── FavoriteAssets.Editor.asmdef
│   ├── Resources/
│   │   └── FavoriteAssetsWindow.uss
│   └── Scripts/
│       ├── FavoriteAssetData.cs
│       ├── FavoriteAssetsContextMenu.cs
│       ├── FavoriteAssetsDataManager.cs
│       └── FavoriteAssetsWindow.cs
```

## Important Notes

- Data persistence uses JSON serialization to persistent data path (survives Unity restarts)
- Thread-safe data access using lock objects in DataManager
- Handles both files and folders as favorites
- GUID-based tracking ensures favorites persist even if assets move
- Asset validation on load filters out invalid/missing assets