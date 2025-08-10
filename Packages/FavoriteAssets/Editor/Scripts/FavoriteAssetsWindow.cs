using System.Collections.Generic;
using System.IO;
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
        private Label _countLabel;
        
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
            
            RefreshAssetsList();
        }
        
        private void CreateToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            
            var leftSection = new VisualElement();
            leftSection.AddToClassList("toolbar-left");
            
            var title = new Label("Favorite Assets");
            _countLabel = new Label();
            leftSection.Add(title);
            leftSection.Add(_countLabel);
            
            var rightSection = new VisualElement();
            rightSection.AddToClassList("toolbar-right");
            
            var refreshButton = new Button(RefreshAssetsList) { text = "Refresh" };
            var clearButton = new Button(ClearAllFavorites) { text = "Clear All" };
            clearButton.AddToClassList("clear-button");
            
            rightSection.Add(refreshButton);
            rightSection.Add(clearButton);
            
            toolbar.Add(leftSection);
            toolbar.Add(rightSection);
            
            _rootElement.Add(toolbar);
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
            
            var favorites = FavoriteAssetsDataManager.GetFavorites();
            UpdateCountLabel(favorites.Count);
            
            if (favorites.Count == 0)
            {
                _assetsList.style.display = DisplayStyle.None;
                _emptyState.style.display = DisplayStyle.Flex;
                return;
            }
            
            _assetsList.style.display = DisplayStyle.Flex;
            _emptyState.style.display = DisplayStyle.None;
            
            foreach (var favorite in favorites)
            {
                CreateAssetItem(favorite);
            }
        }
        
        private void CreateAssetItem(FavoriteAssetData assetData)
        {
            var assetItem = new VisualElement();
            assetItem.AddToClassList("asset-item");
            
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
            
            assetItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    if (evt.clickCount == 2)
                    {
                        OpenAsset(assetData.AssetPath);
                    }
                    else
                    {
                        HighlightAssetInProject(assetData.AssetPath);
                    }
                    evt.StopPropagation();
                }
            });
            
            _assetsList.Add(assetItem);
        }
        
        private void UpdateCountLabel(int count)
        {
            _countLabel.text = $"({count})";
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