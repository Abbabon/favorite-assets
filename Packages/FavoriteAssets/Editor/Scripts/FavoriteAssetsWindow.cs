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
        private VisualElement _rootElement;
        private ScrollView _assetsList;
        private VisualElement _emptyState;
        private Label _statusLabel;
        private Button _sortTypeButton;
        private Button _sortOrderButton;
        
        private FavoriteSortType _currentSortType = FavoriteSortType.Name;
        private SortOrder _currentSortOrder = SortOrder.Ascending;
        
        private void OnFocus()
        {
            // Refresh the list when the window gains focus to update file modification dates
            // This will also automatically clean up any deleted assets
            if (_assetsList != null)
            {
                RefreshAssetsList();
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
                RefreshAssetsList();
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
            
            CreateToolbar();
            CreateAssetsList();
            CreateEmptyState();
            CreateStatusBar();
            
            RefreshAssetsList();
        }
        
        private void CreateToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            
            var leftSection = new VisualElement();
            leftSection.AddToClassList("toolbar-left");
            
            // Remove title and count from toolbar - will be moved to status bar
            
            var centerSection = new VisualElement();
            centerSection.AddToClassList("toolbar-center");
            
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
            
            var refreshButton = new Button(RefreshAssetsList) { text = "Refresh" };
            refreshButton.AddToClassList("refresh-button");
            
            var clearButton = new Button(ClearAllFavorites) { text = "Clear All" };
            clearButton.AddToClassList("clear-button");
            
            rightSection.Add(createGroupButton);
            rightSection.Add(refreshButton);
            rightSection.Add(clearButton);
            
            toolbar.Add(leftSection);
            toolbar.Add(centerSection);
            toolbar.Add(rightSection);
            
            _rootElement.Add(toolbar);
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
            
            // Handle completion of rename
            System.Action completeRename = () =>
            {
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
        
        
        private void CreateAssetsList()
        {
            _assetsList = new ScrollView();
            _assetsList.AddToClassList("assets-list");
            _rootElement.Add(_assetsList);
        }
        
        private void CreateEmptyState()
        {
            _emptyState = new VisualElement();
            _emptyState.AddToClassList("empty-state");
            
            var emptyText = new Label("No favorite assets yet.\n\nRight-click on assets in the Project window and select 'Add to Favorites' to get started.");
            emptyText.AddToClassList("empty-state-text");
            
            _emptyState.Add(emptyText);
            _rootElement.Add(_emptyState);
        }
        
        public void RefreshWindow()
        {
            if (_assetsList != null)
            {
                RefreshAssetsList();
            }
        }
        
        private void RefreshAssetsList()
        {
            if (_assetsList == null) return;
            
            _assetsList.Clear();
            
            var groups = FavoriteAssetsDataManager.GetGroups().OrderBy(g => g.SortOrder).ToList();
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
            
            var icon = new Image();
            icon.AddToClassList("asset-icon");
            var texture = AssetDatabase.GetCachedIcon(assetData.AssetPath);
            if (texture != null)
            {
                icon.image = texture;
            }
            
            var assetInfo = new VisualElement();
            assetInfo.AddToClassList("asset-info");
            
            var assetName = new Label(assetData.AssetName);
            assetName.AddToClassList("asset-name");
            
            var assetPath = new Label(assetData.AssetPath);
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
                        OpenAsset(assetData.AssetPath);
                    }
                    else if (evt.clickCount == 1)
                    {
                        HighlightAssetInProject(assetData.AssetPath);
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
            
            _assetsList.Add(assetItem);
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
            var groups = FavoriteAssetsDataManager.GetGroups().OrderBy(g => g.SortOrder).ToList();
            
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
            _statusLabel.text = count == 1 ? "1 favorite asset" : $"{count} favorite assets";
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
                    Selection.activeObject = folderAsset;
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
                    Selection.activeObject = asset;
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