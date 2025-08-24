# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2025-08-24

### Added
- **Collapsible Groups System**: Organize favorite assets into named, collapsible groups
- **Group Management**: Create, rename (double-click), and delete groups with confirmation
- **Asset Movement**: Right-click context menu to move assets between groups
- **Purple "Create Group" Button**: Dedicated toolbar button for easy group creation
- **Status Bar**: Bottom status line showing total favorite asset count
- **Double-Click Rename**: Click group names twice to rename them inline

### Changed
- **UI Layout**: Removed redundant "FavoriteAssets (N)" title from window header
- **Toolbar Organization**: Moved create group functionality to main toolbar
- **Visual Hierarchy**: Groups have distinctive blue headers with expand/collapse arrows
- **Compact Design**: Reduced padding and margins throughout interface
- **Asset Organization**: Ungrouped assets appear first, followed by grouped assets

### Fixed
- **DateTime Serialization**: Converted DateTime fields to serializable long ticks format
- **CSS Styling**: Removed problematic transform properties causing ArgumentOutOfRangeException
- **Group Data Persistence**: All group data now properly saves/loads with favorites

### Technical Details
- New `FavoriteGroup` class for group metadata management
- Extended `FavoriteAssetData` with `groupId` field for group membership
- Purple button (#9B59B6) for group creation to avoid color conflicts
- Context menu system for intuitive asset-to-group assignment
- Inline text field editing for group names with Enter/Escape shortcuts
- Thread-safe group operations with proper data validation

## [1.1.0] - 2025-08-10

### Added
- Advanced sorting system with 4 sorting options: Name, Type, Date Added, File Modified
- Color-coded cycling buttons replacing dropdowns for better UX
- Real-time file modification date tracking from disk
- Automatic cleanup of deleted assets from favorites list
- Smart asset validation using both Unity GUID system and file system checks

### Changed
- Replaced dropdown menus with intuitive cycling buttons
- Enhanced UI with colorful, easy-to-read button design
- Improved toolbar layout with better visual hierarchy
- Updated README with comprehensive feature documentation

### Fixed
- Assets deleted from project are now automatically removed from favorites
- File modification dates now reflect actual disk changes, not just access times

### Technical Details
- Blue button cycles through sort types (Name → Type → Added → Modified)
- Green button toggles sort order (ascending/descending with ↑/↓ arrows)
- Teal refresh button for manual list updates
- Red clear button for removing all favorites
- Automatic cleanup triggers on window focus, refresh, and data load
- Performance optimized with efficient validation and minimal disk I/O

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