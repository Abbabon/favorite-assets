using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FavoriteAssets.Editor
{
    public class FavoriteAssetsWindow : EditorWindow
    {
        private enum WindowTab
        {
            Favorites,
            History
        }
        
        private VisualElement _rootElement;
        private VisualElement _favoritesView;
        private VisualElement _historyView;
        private ScrollView _assetsList;
        private ScrollView _historyList;
        private VisualElement _emptyState;
        private VisualElement _historyEmptyState;
        private Label _historyEmptyStateText;
        private Label _statusLabel;
        private Button _sortTypeButton;
        private Button _sortOrderButton;
        private Button _favoritesTabButton;
        private Button _historyTabButton;
        private VisualElement _sortSection;
        private VisualElement _favoritesActions;
        private VisualElement _historyActions;
        
        private FavoriteSortType _currentSortType = FavoriteSortType.Name;
        private SortOrder _currentSortOrder = SortOrder.Ascending;
        
        // Serialized so the active tab survives domain reloads and is stored in the saved window layout.
        [SerializeField] private WindowTab _activeTab = WindowTab.Favorites;

        private const string _kDragGenericDataKey = "FavoriteAssets.DraggedGuid";
        private const string _kDragOverClass = "drag-over";
        private const string _kTabActiveClass = "tab-button-active";
        
        private void OnFocus()
        {
            // Refresh when the window gains focus to update file modification dates.
            // This will also automatically clean up any deleted assets.
            if (_rootElement != null)
            {
                RefreshActiveTab();
            }
        }
        
        /// <summary>
        /// Refreshes every open Favorites window without creating one.
        /// Deliberately not GetWindow&lt;T&gt;(), which would pop the window open when a prefab is opened.
        /// </summary>
        public static void RefreshOpenWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<FavoriteAssetsWindow>())
            {
                window.RefreshWindow();
            }
        }
        
        [MenuItem("Window/Favorite Assets")]
        public static void ShowWindow()
        {
            var window = GetWindow<FavoriteAssetsWindow>();
            window.titleContent = new GUIContent("Favorite Assets", EditorGUIUtility.FindTexture("Favorite"));
            window.Show();
        }
        
        public void CreateGUI()
        {
            if (_rootElement != null && _rootElement.parent != null)
            {
                RefreshActiveTab();
                return;
            }
            
            _rootElement = rootVisualElement;
            _rootElement.Clear();
            _rootElement.AddToClassList("favorite-assets-window");
            
            var styleSheet = Resources.Load<StyleSheet>("FavoriteAssetsWindow");
            if (styleSheet != null)
            {
                _rootElement.styleSheets.Add(styleSheet);
            }
            
            CreateTabBar();
            CreateToolbar();
            CreateFavoritesView();
            CreateHistoryView();
            CreateStatusBar();

            RegisterWindowDropZone();

            // CreateGUI runs after deserialization, so _activeTab is already restored here.
            SetActiveTab(_activeTab);
        }
        
        private void SetActiveTab(WindowTab tab)
        {
            _activeTab = tab;
            var isFavorites = tab == WindowTab.Favorites;
            
            _favoritesView.style.display = isFavorites ? DisplayStyle.Flex : DisplayStyle.None;
            _historyView.style.display = isFavorites ? DisplayStyle.None : DisplayStyle.Flex;
            _sortSection.style.display = isFavorites ? DisplayStyle.Flex : DisplayStyle.None;
            _favoritesActions.style.display = isFavorites ? DisplayStyle.Flex : DisplayStyle.None;
            _historyActions.style.display = isFavorites ? DisplayStyle.None : DisplayStyle.Flex;
            
            _favoritesTabButton.EnableInClassList(_kTabActiveClass, isFavorites);
            _historyTabButton.EnableInClassList(_kTabActiveClass, !isFavorites);
            
            RefreshActiveTab();
        }
        
        private void RefreshActiveTab()
        {
            if (_rootElement == null) return;
            
            if (_activeTab == WindowTab.Favorites)
            {
                RefreshAssetsList();
            }
            else
            {
                RefreshHistoryList();
            }
        }

        private void RegisterWindowDropZone()
        {
            _rootElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (!CanAcceptCurrentDrag())
                    return;

                DragAndDrop.visualMode = IsInternalDrag()
                    ? DragAndDropVisualMode.Move
                    : DragAndDropVisualMode.Copy;
                _rootElement.AddToClassList(_kDragOverClass);
            });

            _rootElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                _rootElement.RemoveFromClassList(_kDragOverClass);
                if (!CanAcceptCurrentDrag())
                    return;

                DragAndDrop.AcceptDrag();
                HandleDrop(targetGroupId: null);
            });

            _rootElement.RegisterCallback<DragLeaveEvent>(evt => _rootElement.RemoveFromClassList(_kDragOverClass));
            _rootElement.RegisterCallback<DragExitedEvent>(evt => _rootElement.RemoveFromClassList(_kDragOverClass));
        }

        private void RegisterGroupDropZone(VisualElement groupHeader, string groupId)
        {
            groupHeader.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (!CanAcceptCurrentDrag())
                    return;

                DragAndDrop.visualMode = IsInternalDrag()
                    ? DragAndDropVisualMode.Move
                    : DragAndDropVisualMode.Copy;
                groupHeader.AddToClassList(_kDragOverClass);
                evt.StopPropagation();
            });

            groupHeader.RegisterCallback<DragPerformEvent>(evt =>
            {
                groupHeader.RemoveFromClassList(_kDragOverClass);
                if (!CanAcceptCurrentDrag())
                    return;

                DragAndDrop.AcceptDrag();
                HandleDrop(groupId);
                evt.StopPropagation();
            });

            groupHeader.RegisterCallback<DragLeaveEvent>(evt => groupHeader.RemoveFromClassList(_kDragOverClass));
            groupHeader.RegisterCallback<DragExitedEvent>(evt => groupHeader.RemoveFromClassList(_kDragOverClass));
        }

        private bool IsInternalDrag()
        {
            return DragAndDrop.GetGenericData(_kDragGenericDataKey) is string guid && !string.IsNullOrEmpty(guid);
        }

        private bool CanAcceptCurrentDrag()
        {
            // Dropping while the History tab is showing would add favorites the user cannot see.
            if (_activeTab != WindowTab.Favorites)
                return false;
            
            if (IsInternalDrag())
                return true;

            var paths = DragAndDrop.paths;
            return paths != null && paths.Any(p => !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p)));
        }

        private void HandleDrop(string targetGroupId)
        {
            var changed = false;

            if (IsInternalDrag())
            {
                var draggedGuid = (string)DragAndDrop.GetGenericData(_kDragGenericDataKey);
                DragAndDrop.SetGenericData(_kDragGenericDataKey, null);
                changed = FavoriteAssetsDataManager.MoveAssetToGroup(draggedGuid, targetGroupId);
            }
            else
            {
                foreach (var path in DragAndDrop.paths)
                {
                    if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                        continue;

                    if (FavoriteAssetsDataManager.AddFavorite(path, targetGroupId))
                    {
                        changed = true;
                    }
                    else if (targetGroupId != null && FavoriteAssetsDataManager.IsFavorite(path))
                    {
                        // Already a favorite - dropping it on a group moves it there
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        changed |= FavoriteAssetsDataManager.MoveAssetToGroup(guid, targetGroupId);
                    }
                }
            }

            if (changed)
            {
                RefreshAssetsList();
            }
        }

        private void MakeItemDraggable(VisualElement assetItem, FavoriteAssetData assetData)
        {
            var mouseDownPosition = Vector2.zero;
            var isMouseDown = false;

            assetItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    mouseDownPosition = evt.mousePosition;
                    isMouseDown = true;
                }
            });

            assetItem.RegisterCallback<MouseUpEvent>(evt => isMouseDown = false);

            assetItem.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!isMouseDown || (evt.pressedButtons & 1) == 0)
                    return;

                if ((evt.mousePosition - mouseDownPosition).sqrMagnitude < 25f)
                    return;

                isMouseDown = false;

                var asset = AssetDatabase.LoadMainAssetAtPath(assetData.AssetPath);
                if (asset == null)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { asset };
                DragAndDrop.paths = new[] { assetData.AssetPath };
                DragAndDrop.SetGenericData(_kDragGenericDataKey, assetData.AssetGuid);
                DragAndDrop.StartDrag(assetData.AssetName);
                evt.StopPropagation();
            });
        }
        
        private void CreateToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            
            var centerSection = new VisualElement();
            centerSection.AddToClassList("toolbar-center");
            _sortSection = centerSection;
            
            var sortLabel = new Label("Sort:");
            sortLabel.AddToClassList("sort-label");
            
            _sortTypeButton = new Button(CycleSortType);
            _sortTypeButton.AddToClassList("sort-type-button");
            _sortTypeButton.text = GetSortTypeDisplayName(_currentSortType);
            
            _sortOrderButton = new Button(CycleSortOrder);
            _sortOrderButton.AddToClassList("sort-order-button");
            _sortOrderButton.text = GetSortOrderDisplayName(_currentSortOrder);
            
            centerSection.Add(sortLabel);
            centerSection.Add(_sortTypeButton);
            centerSection.Add(_sortOrderButton);
            
            var rightSection = new VisualElement();
            rightSection.AddToClassList("toolbar-right");
            
            var createGroupButton = new Button(CreateNewGroup) { text = "+ Group" };
            createGroupButton.AddToClassList("create-group-button");
            
            var refreshButton = new Button(RefreshActiveTab) { text = "Refresh" };
            refreshButton.AddToClassList("refresh-button");
            
            var clearButton = new Button(ClearAllFavorites) { text = "Clear All" };
            clearButton.AddToClassList("clear-button");
            
            var clearHistoryButton = new Button(ClearPrefabHistory) { text = "Clear History" };
            clearHistoryButton.AddToClassList("clear-button");

            var settingsButton = new Button(OpenSettings) { text = "⚙" };
            settingsButton.AddToClassList("settings-button");
            settingsButton.tooltip = "Open Favorite Assets preferences";
            
            _favoritesActions = new VisualElement();
            _favoritesActions.AddToClassList("toolbar-actions");
            _favoritesActions.Add(createGroupButton);
            _favoritesActions.Add(clearButton);
            
            _historyActions = new VisualElement();
            _historyActions.AddToClassList("toolbar-actions");
            _historyActions.Add(clearHistoryButton);

            rightSection.Add(_favoritesActions);
            rightSection.Add(_historyActions);
            rightSection.Add(refreshButton);
            rightSection.Add(settingsButton);
            
            toolbar.Add(centerSection);
            toolbar.Add(rightSection);
            
            _rootElement.Add(toolbar);
        }
        
        private void CreateTabBar()
        {
            // The tabs get a row of their own. Sharing the toolbar row made them overlap the sort
            // controls as soon as the window was docked narrow.
            var tabBar = new VisualElement();
            tabBar.AddToClassList("tab-bar");
            
            var tabStrip = new VisualElement();
            tabStrip.AddToClassList("tab-strip");
            
            _favoritesTabButton = new Button(() => SetActiveTab(WindowTab.Favorites)) { text = "Favorites" };
            _favoritesTabButton.AddToClassList("tab-button");
            
            _historyTabButton = new Button(() => SetActiveTab(WindowTab.History)) { text = "Prefab History" };
            _historyTabButton.AddToClassList("tab-button");
            _historyTabButton.tooltip = "Prefabs you have opened in Prefab Mode, most recent first";
            
            tabStrip.Add(_favoritesTabButton);
            tabStrip.Add(_historyTabButton);
            tabBar.Add(tabStrip);
            _rootElement.Add(tabBar);
        }
        
        private void CreateGroupHeader(FavoriteGroup group)
        {
            var groupHeader = new VisualElement();
            groupHeader.AddToClassList("group-header");
            
            var collapseButton = new Button(() => ToggleGroupCollapse(group.Id));
            collapseButton.AddToClassList("group-collapse-button");
            collapseButton.text = group.IsCollapsed ? "▶" : "▼";
            
            var groupName = new Label(group.Name);
            groupName.AddToClassList("group-name");
            
            // Add double-click to rename functionality
            groupName.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0 && evt.clickCount == 2)
                {
                    StartGroupRename(groupHeader, group, groupName);
                    evt.StopPropagation();
                }
            });
            
            var assetCount = FavoriteAssetsDataManager.GetAssetsInGroup(group.Id).Count;
            var countLabel = new Label($"({assetCount})");
            countLabel.AddToClassList("group-count");
            
            var deleteGroupButton = new Button(() => DeleteGroup(group.Id)) { text = "×" };
            deleteGroupButton.AddToClassList("group-delete-button");
            deleteGroupButton.tooltip = "Delete group";
            
            groupHeader.Add(collapseButton);
            groupHeader.Add(groupName);
            groupHeader.Add(countLabel);
            groupHeader.Add(deleteGroupButton);

            RegisterGroupDropZone(groupHeader, group.Id);

            _assetsList.Add(groupHeader);
        }
        
        private void CreateSeparator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("separator");
            _assetsList.Add(separator);
        }
        
        
        private void ToggleGroupCollapse(string groupId)
        {
            var group = FavoriteAssetsDataManager.GetGroups().FirstOrDefault(g => g.Id == groupId);
            if (group != null)
            {
                FavoriteAssetsDataManager.SetGroupCollapsed(groupId, !group.IsCollapsed);
                RefreshAssetsList();
            }
        }
        
        private void DeleteGroup(string groupId)
        {
            var group = FavoriteAssetsDataManager.GetGroups().FirstOrDefault(g => g.Id == groupId);
            if (group != null && EditorUtility.DisplayDialog("Delete Group", 
                $"Are you sure you want to delete the group '{group.Name}'? Assets will be moved to ungrouped.", 
                "Delete", "Cancel"))
            {
                FavoriteAssetsDataManager.DeleteGroup(groupId);
                RefreshAssetsList();
            }
        }
        
        private void OpenSettings()
        {
            SettingsService.OpenUserPreferences(FavoriteAssetsSettings.PreferencesPath);
        }

        private void CreateNewGroup()
        {
            var groupName = $"Group {System.DateTime.Now:HH:mm:ss}";
            FavoriteAssetsDataManager.CreateGroup(groupName);
            RefreshAssetsList();
        }
        
        private void StartGroupRename(VisualElement groupHeader, FavoriteGroup group, Label groupNameLabel)
        {
            // Create text field for renaming
            var textField = new TextField();
            textField.AddToClassList("group-name-edit");
            textField.value = group.Name;
            
            // Replace the label with the text field
            var labelIndex = groupHeader.IndexOf(groupNameLabel);
            groupHeader.RemoveAt(labelIndex);
            groupHeader.Insert(labelIndex, textField);
            
            // Focus and select all text
            textField.Focus();
            textField.SelectAll();
            
            var renameHandled = false;

            // Handle completion of rename
            System.Action completeRename = () =>
            {
                if (renameHandled) return;
                renameHandled = true;
                var newName = textField.value.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != group.Name)
                {
                    FavoriteAssetsDataManager.RenameGroup(group.Id, newName);
                }
                RefreshAssetsList();
            };

            // Handle escape to cancel
            System.Action cancelRename = () =>
            {
                if (renameHandled) return;
                renameHandled = true;
                RefreshAssetsList();
            };
            
            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                completeRename();
            });
            
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    completeRename();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    cancelRename();
                    evt.StopPropagation();
                }
            });
        }
        
        
        private void CycleSortType()
        {
            _currentSortType = _currentSortType switch
            {
                FavoriteSortType.Name => FavoriteSortType.Type,
                FavoriteSortType.Type => FavoriteSortType.DateAdded,
                FavoriteSortType.DateAdded => FavoriteSortType.DateUpdated,
                FavoriteSortType.DateUpdated => FavoriteSortType.Name,
                _ => FavoriteSortType.Name
            };
            
            _sortTypeButton.text = GetSortTypeDisplayName(_currentSortType);
            RefreshAssetsList();
        }
        
        private void CycleSortOrder()
        {
            _currentSortOrder = _currentSortOrder == SortOrder.Ascending 
                ? SortOrder.Descending 
                : SortOrder.Ascending;
            
            _sortOrderButton.text = GetSortOrderDisplayName(_currentSortOrder);
            RefreshAssetsList();
        }
        
        private string GetSortTypeDisplayName(FavoriteSortType sortType)
        {
            return sortType switch
            {
                FavoriteSortType.Name => "Name",
                FavoriteSortType.Type => "Type",
                FavoriteSortType.DateAdded => "Added",
                FavoriteSortType.DateUpdated => "Modified",
                _ => "Name"
            };
        }
        
        private string GetSortOrderDisplayName(SortOrder sortOrder)
        {
            return sortOrder == SortOrder.Ascending ? "↑" : "↓";
        }
        
        
        private void CreateFavoritesView()
        {
            // The favorites list and its empty state share a wrapper so that the tab switcher toggles
            // one element while RefreshAssetsList keeps owning the list-vs-empty-state toggle.
            _favoritesView = new VisualElement();
            _favoritesView.AddToClassList("favorites-view");
            
            _assetsList = new ScrollView();
            _assetsList.AddToClassList("assets-list");
            
            _emptyState = new VisualElement();
            _emptyState.AddToClassList("empty-state");
            
            var emptyText = new Label("No favorite assets yet.\n\nDrag assets here from the Project window, or right-click them and select 'Add to Favorites' to get started.");
            emptyText.AddToClassList("empty-state-text");
            _emptyState.Add(emptyText);
            
            _favoritesView.Add(_assetsList);
            _favoritesView.Add(_emptyState);
            _rootElement.Add(_favoritesView);
        }
        
        private void CreateHistoryView()
        {
            _historyView = new VisualElement();
            _historyView.AddToClassList("history-view");
            
            _historyList = new ScrollView();
            _historyList.AddToClassList("history-list");
            
            _historyEmptyState = new VisualElement();
            _historyEmptyState.AddToClassList("empty-state");
            
            _historyEmptyStateText = new Label();
            _historyEmptyStateText.AddToClassList("empty-state-text");
            _historyEmptyState.Add(_historyEmptyStateText);
            
            _historyView.Add(_historyList);
            _historyView.Add(_historyEmptyState);
            _rootElement.Add(_historyView);
        }
        
        public void RefreshWindow()
        {
            // RefreshOpenWindows can reach a window whose CreateGUI has not run yet, which is a real
            // state right after a domain reload.
            if (_rootElement == null) return;
            
            RefreshActiveTab();
        }
        
        private void RefreshAssetsList()
        {
            if (_assetsList == null) return;

            FavoriteAssetsDataManager.CleanupInvalidAssetsManually();
            _assetsList.Clear();

            var groups = FavoriteAssetsDataManager.GetGroups();
            var ungroupedAssets = FavoriteAssetsDataManager.GetUngroupedAssets();
            var sortedUngrouped = SortFavorites(ungroupedAssets, _currentSortType, _currentSortOrder);
            
            var totalCount = ungroupedAssets.Count + groups.Sum(g => FavoriteAssetsDataManager.GetAssetsInGroup(g.Id).Count);
            UpdateStatusLabel(totalCount);
            
            if (totalCount == 0)
            {
                _assetsList.style.display = DisplayStyle.None;
                _emptyState.style.display = DisplayStyle.Flex;
                return;
            }
            
            _assetsList.style.display = DisplayStyle.Flex;
            _emptyState.style.display = DisplayStyle.None;
            
            // Add ungrouped assets first
            if (sortedUngrouped.Count > 0)
            {
                foreach (var favorite in sortedUngrouped)
                {
                    CreateAssetItem(favorite);
                }
                
                // Add separator if there are also groups
                if (groups.Count > 0)
                {
                    CreateSeparator();
                }
            }
            
            // Add groups and their assets
            foreach (var group in groups)
            {
                CreateGroupHeader(group);
                
                if (!group.IsCollapsed)
                {
                    var groupAssets = FavoriteAssetsDataManager.GetAssetsInGroup(group.Id);
                    var sortedGroupAssets = SortFavorites(groupAssets, _currentSortType, _currentSortOrder);
                    
                    foreach (var favorite in sortedGroupAssets)
                    {
                        CreateAssetItem(favorite, true);
                    }
                }
            }
        }
        
        private List<FavoriteAssetData> SortFavorites(List<FavoriteAssetData> favorites, FavoriteSortType sortType, SortOrder sortOrder)
        {
            switch (sortType)
            {
                case FavoriteSortType.Name:
                    return sortOrder == SortOrder.Ascending 
                        ? favorites.OrderBy(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList()
                        : favorites.OrderByDescending(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList();
                        
                case FavoriteSortType.Type:
                    return sortOrder == SortOrder.Ascending
                        ? favorites.OrderBy(f => f.AssetType).ThenBy(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList()
                        : favorites.OrderByDescending(f => f.AssetType).ThenBy(f => f.AssetName, StringComparer.OrdinalIgnoreCase).ToList();
                        
                case FavoriteSortType.DateAdded:
                    return sortOrder == SortOrder.Ascending
                        ? favorites.OrderBy(f => f.DateAdded).ToList()
                        : favorites.OrderByDescending(f => f.DateAdded).ToList();
                        
                case FavoriteSortType.DateUpdated:
                    return sortOrder == SortOrder.Ascending
                        ? favorites.OrderBy(f => f.FileModificationDate).ToList()
                        : favorites.OrderByDescending(f => f.FileModificationDate).ToList();
                        
                default:
                    return favorites;
            }
        }
        
        private void CreateAssetItem(FavoriteAssetData assetData, bool isInGroup = false)
        {
            var assetItem = new VisualElement();
            assetItem.AddToClassList("asset-item");
            if (isInGroup)
            {
                assetItem.AddToClassList("asset-item-grouped");
            }
            
            // Use the GUID-resolved path so a favorite that was moved or renamed still resolves.
            var currentPath = assetData.CurrentPath;
            
            var icon = new Image();
            icon.AddToClassList("asset-icon");
            var texture = AssetDatabase.GetCachedIcon(currentPath);
            if (texture != null)
            {
                icon.image = texture;
            }
            
            var assetInfo = new VisualElement();
            assetInfo.AddToClassList("asset-info");
            
            var assetName = new Label(assetData.AssetName);
            assetName.AddToClassList("asset-name");
            
            var assetPath = new Label(currentPath);
            assetPath.AddToClassList("asset-path");
            
            assetInfo.Add(assetName);
            assetInfo.Add(assetPath);
            
            var assetType = new Label($"[{assetData.AssetType}]");
            assetType.AddToClassList("asset-type");
            
            var removeButton = new Button(() => RemoveFavorite(assetData.AssetGuid)) { text = "×" };
            removeButton.AddToClassList("remove-button");
            removeButton.tooltip = "Remove from favorites";
            
            assetItem.Add(icon);
            assetItem.Add(assetInfo);
            assetItem.Add(assetType);
            assetItem.Add(removeButton);
            
            // Add left-click support
            assetItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    FavoriteAssetsDataManager.UpdateAssetAccessDate(assetData.AssetGuid);
                    
                    if (evt.clickCount == 2)
                    {
                        OpenAsset(currentPath);
                    }
                    else if (evt.clickCount == 1)
                    {
                        HighlightAssetInProject(currentPath);
                    }
                    evt.StopPropagation();
                }
            });
            
            // Add right-click context menu
            assetItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1) // Right click
                {
                    ShowAssetContextMenu(assetData);
                    evt.StopPropagation();
                }
            });

            MakeItemDraggable(assetItem, assetData);

            _assetsList.Add(assetItem);
        }
        
        private void RefreshHistoryList()
        {
            if (_historyList == null) return;
            
            _historyList.Clear();
            
            // History is a log, not a curated list: unlike favorites it is never pruned here.
            // Entries whose prefab no longer exists are shown greyed out instead.
            var entries = PrefabHistoryManager.GetEntries();
            var missingCount = entries.Count(e => !e.IsValid());
            
            SetStatusText(BuildHistoryStatusText(entries.Count, missingCount));
            
            if (entries.Count == 0)
            {
                _historyList.style.display = DisplayStyle.None;
                _historyEmptyState.style.display = DisplayStyle.Flex;
                _historyEmptyStateText.text = FavoriteAssetsSettings.RecordPrefabHistory
                    ? "No prefabs opened yet.\n\nOpen any prefab in Prefab Mode and it will show up here."
                    : "Prefab history recording is turned off.\n\nEnable it in Preferences \u2192 Favorite Assets.";
                return;
            }
            
            _historyList.style.display = DisplayStyle.Flex;
            _historyEmptyState.style.display = DisplayStyle.None;
            
            foreach (var entry in entries)
            {
                CreateHistoryItem(entry);
            }
        }
        
        private static string BuildHistoryStatusText(int total, int missing)
        {
            var label = total == 1 ? "1 prefab in history" : $"{total} prefabs in history";
            return missing > 0 ? $"{label} ({missing} missing)" : label;
        }
        
        private void CreateHistoryItem(PrefabHistoryEntry entry)
        {
            var path = entry.CurrentPath;
            var isValid = entry.IsValid();
            
            var historyItem = new VisualElement();
            historyItem.AddToClassList("asset-item");
            historyItem.AddToClassList("history-item");
            if (!isValid)
            {
                historyItem.AddToClassList("asset-item-missing");
                historyItem.tooltip = $"This prefab no longer exists at {path}";
            }
            
            var icon = new Image();
            icon.AddToClassList("asset-icon");
            var texture = AssetDatabase.GetCachedIcon(path);
            if (texture == null)
            {
                texture = EditorGUIUtility.IconContent("Prefab Icon").image;
            }
            if (texture != null)
            {
                icon.image = texture;
            }
            
            var assetInfo = new VisualElement();
            assetInfo.AddToClassList("asset-info");
            
            var assetName = new Label(entry.PrefabName);
            assetName.AddToClassList("asset-name");
            
            var assetPath = new Label(path);
            assetPath.AddToClassList("asset-path");
            
            assetInfo.Add(assetName);
            assetInfo.Add(assetPath);
            
            var timeLabel = new Label(FormatRelativeTime(entry.LastOpened));
            timeLabel.AddToClassList("history-time");
            timeLabel.tooltip = entry.LastOpened.ToString("f");
            
            var isFavorite = FavoriteAssetsDataManager.IsFavoriteByGuid(entry.PrefabGuid);
            var favoriteButton = new Button(() => ToggleFavoriteFromHistory(entry)) { text = isFavorite ? "\u2605" : "\u2606" };
            favoriteButton.AddToClassList("favorite-toggle-button");
            favoriteButton.EnableInClassList("favorite-toggle-button-on", isFavorite);
            favoriteButton.tooltip = isFavorite ? "Remove from favorites" : "Add to favorites";
            
            var removeButton = new Button(() => RemoveFromHistory(entry.PrefabGuid)) { text = "\u00d7" };
            removeButton.AddToClassList("remove-button");
            removeButton.tooltip = "Remove from history";
            
            historyItem.Add(icon);
            historyItem.Add(assetInfo);
            historyItem.Add(timeLabel);
            historyItem.Add(favoriteButton);
            historyItem.Add(removeButton);
            
            historyItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    if (evt.clickCount == 2)
                    {
                        OpenPrefabFromHistory(entry);
                    }
                    else if (evt.clickCount == 1)
                    {
                        HighlightAssetInProject(path);
                    }
                    evt.StopPropagation();
                }
            });
            
            historyItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    ShowHistoryContextMenu(entry);
                    evt.StopPropagation();
                }
            });
            
            _historyList.Add(historyItem);
        }
        
        private void ShowHistoryContextMenu(PrefabHistoryEntry entry)
        {
            var menu = new GenericMenu();
            var path = entry.CurrentPath;
            
            if (entry.IsValid())
            {
                menu.AddItem(new GUIContent("Open in Prefab Mode"), false, () => OpenPrefabFromHistory(entry));
                menu.AddItem(new GUIContent("Ping in Project"), false, () => HighlightAssetInProject(path));
                menu.AddSeparator("");
                
                if (FavoriteAssetsDataManager.IsFavoriteByGuid(entry.PrefabGuid))
                {
                    menu.AddItem(new GUIContent("Remove from Favorites"), false, () => ToggleFavoriteFromHistory(entry));
                }
                else
                {
                    menu.AddItem(new GUIContent("Add to Favorites"), false, () => ToggleFavoriteFromHistory(entry));
                }
                
                menu.AddSeparator("");
            }
            
            menu.AddItem(new GUIContent("Remove from History"), false, () => RemoveFromHistory(entry.PrefabGuid));
            menu.ShowAsContext();
        }
        
        private void ToggleFavoriteFromHistory(PrefabHistoryEntry entry)
        {
            if (FavoriteAssetsDataManager.IsFavoriteByGuid(entry.PrefabGuid))
            {
                FavoriteAssetsDataManager.RemoveFavorite(entry.PrefabGuid);
            }
            else
            {
                FavoriteAssetsDataManager.AddFavorite(entry.CurrentPath);
            }
            
            RefreshActiveTab();
        }
        
        private void RemoveFromHistory(string prefabGuid)
        {
            if (PrefabHistoryManager.Remove(prefabGuid))
            {
                RefreshHistoryList();
            }
        }
        
        private void OpenPrefabFromHistory(PrefabHistoryEntry entry)
        {
            var path = entry.CurrentPath;
            
            if (!entry.IsValid())
            {
                if (EditorUtility.DisplayDialog("Prefab Not Found",
                    $"'{entry.PrefabName}' no longer exists at {path}.",
                    "Remove from History", "Keep"))
                {
                    RemoveFromHistory(entry.PrefabGuid);
                }
                return;
            }
            
            UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(path);
        }
        
        private static string FormatRelativeTime(DateTime timestamp)
        {
            var elapsed = DateTime.Now - timestamp;
            
            if (elapsed.TotalSeconds < 60) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 2) return "yesterday";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
            
            return timestamp.ToString("MMM d");
        }
        
        private void CreateStatusBar()
        {
            var statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");
            
            _statusLabel = new Label();
            _statusLabel.AddToClassList("status-label");
            
            statusBar.Add(_statusLabel);
            _rootElement.Add(statusBar);
        }
        
        private void ShowAssetContextMenu(FavoriteAssetData assetData)
        {
            var menu = new GenericMenu();
            var groups = FavoriteAssetsDataManager.GetGroups();
            
            // Add "Remove from Group" option if asset is in a group
            if (!string.IsNullOrEmpty(assetData.GroupId))
            {
                menu.AddItem(new GUIContent("Remove from Group"), false, () =>
                {
                    FavoriteAssetsDataManager.MoveAssetToGroup(assetData.AssetGuid, null);
                    RefreshAssetsList();
                });
                menu.AddSeparator("");
            }
            
            // Add "Move to Group" options
            if (groups.Count > 0)
            {
                foreach (var group in groups)
                {
                    // Skip if asset is already in this group
                    if (assetData.GroupId == group.Id)
                        continue;
                        
                    var groupName = group.Name;
                    menu.AddItem(new GUIContent($"Move to Group/{groupName}"), false, () =>
                    {
                        FavoriteAssetsDataManager.MoveAssetToGroup(assetData.AssetGuid, group.Id);
                        RefreshAssetsList();
                    });
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move to Group/No Groups Available"));
            }
            
            menu.AddSeparator("");
            
            // Add "Remove from Favorites" option
            menu.AddItem(new GUIContent("Remove from Favorites"), false, () =>
            {
                RemoveFavorite(assetData.AssetGuid);
            });
            
            menu.ShowAsContext();
        }
        
        private void UpdateStatusLabel(int count)
        {
            SetStatusText(count == 1 ? "1 favorite asset" : $"{count} favorite assets");
        }
        
        private void SetStatusText(string text)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = text;
            }
        }
        
        private void RemoveFavorite(string assetGuid)
        {
            if (FavoriteAssetsDataManager.RemoveFavorite(assetGuid))
            {
                RefreshAssetsList();
            }
        }
        
        private void ClearAllFavorites()
        {
            if (EditorUtility.DisplayDialog("Clear All Favorites", 
                "Are you sure you want to remove all favorite assets?", 
                "Clear All", "Cancel"))
            {
                FavoriteAssetsDataManager.ClearAll();
                RefreshAssetsList();
            }
        }
        
        private void ClearPrefabHistory()
        {
            var count = PrefabHistoryManager.Count;
            if (count == 0)
                return;
            
            if (EditorUtility.DisplayDialog("Clear Prefab History",
                count == 1
                    ? "Are you sure you want to remove the single entry from the prefab history?"
                    : $"Are you sure you want to remove all {count} entries from the prefab history?",
                "Clear", "Cancel"))
            {
                PrefabHistoryManager.ClearAll();
                RefreshHistoryList();
            }
        }
        
        private void HighlightAssetInProject(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;
                
            // Check if it's a folder
            if (Directory.Exists(assetPath))
            {
                var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (folderAsset != null)
                {
                    EditorGUIUtility.PingObject(folderAsset);
                    if (FavoriteAssetsSettings.SelectOnClick)
                    {
                        Selection.activeObject = folderAsset;
                    }
                }
                return;
            }

            // Handle regular files
            if (File.Exists(assetPath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                    if (FavoriteAssetsSettings.SelectOnClick)
                    {
                        Selection.activeObject = asset;
                    }
                }
            }
        }
        
        private void OpenAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;
                
            // Handle folders - highlight and expand in Project view
            if (Directory.Exists(assetPath))
            {
                var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (folderAsset != null)
                {
                    EditorGUIUtility.PingObject(folderAsset);
                    Selection.activeObject = folderAsset;
                    // Try to expand the folder in the Project view
                    var projectWindow = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                    if (projectWindow != null)
                    {
                        var window = EditorWindow.GetWindow(projectWindow);
                        if (window != null)
                        {
                            window.Repaint();
                        }
                    }
                }
                return;
            }
            
            // Handle regular files
            if (File.Exists(assetPath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }
            }
        }
    }
}