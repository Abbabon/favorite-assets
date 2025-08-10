# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-08-10

### Added
- Initial release of Favorite Assets Unity Editor tool
- Context menu integration for adding assets and folders to favorites
- Dedicated Favorite Assets window accessible via Window menu
- Persistent data storage using JSON serialization
- Thread-safe data management with proper locking
- Asset validation to filter out missing/invalid assets
- Single-click selection and double-click opening of favorite assets
- Visual asset type indicators and full path display
- GUID-based asset tracking that persists through asset moves
- Toolbar with refresh and clear all functionality
- Asset count display in window toolbar
- UIElements-based modern Editor UI
- Support for both files and folders as favorites
- Empty state display when no favorites exist

### Technical Details
- Built with Unity 2022.3 LTS compatibility
- Uses Unity's UIElements system for UI rendering
- Data stored in Application.persistentDataPath for persistence
- Assembly definition with Editor-only compilation
- USS styling for consistent visual appearance