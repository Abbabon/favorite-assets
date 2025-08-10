using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    public static class FavoriteAssetsDataManager
    {
        private const string _kPrefsKey = "FavoriteAssets_Data";
        private const string _kDataFileName = "FavoriteAssetsData.json";
        
        private static List<FavoriteAssetData> _favoriteAssets;
        private static readonly object _lock = new object();
        
        static FavoriteAssetsDataManager()
        {
            LoadFavorites();
        }
        
        public static List<FavoriteAssetData> GetFavorites()
        {
            lock (_lock)
            {
                return _favoriteAssets?.ToList() ?? new List<FavoriteAssetData>();
            }
        }
        
        public static bool AddFavorite(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid))
                return false;
                
            lock (_lock)
            {
                if (_favoriteAssets.Any(f => f.AssetGuid == assetGuid))
                    return false;
                    
                var assetName = Directory.Exists(assetPath) ? 
                    Path.GetFileName(assetPath.TrimEnd('/', '\\')) : 
                    Path.GetFileNameWithoutExtension(assetPath);
                    
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                var assetType = Directory.Exists(assetPath) ? "Folder" : 
                    (asset != null ? asset.GetType().Name : "Unknown");
                
                var favoriteData = new FavoriteAssetData(assetPath, assetName, assetType, assetGuid);
                _favoriteAssets.Add(favoriteData);
                
                SaveFavorites();
                return true;
            }
        }
        
        public static bool RemoveFavorite(string assetGuid)
        {
            lock (_lock)
            {
                var index = _favoriteAssets.FindIndex(f => f.AssetGuid == assetGuid);
                if (index >= 0)
                {
                    _favoriteAssets.RemoveAt(index);
                    SaveFavorites();
                    return true;
                }
                return false;
            }
        }
        
        public static bool IsFavorite(string assetPath)
        {
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return ! string.IsNullOrEmpty(assetGuid) && IsFavoriteByGuid(assetGuid);
        }
        
        public static bool IsFavoriteByGuid(string assetGuid)
        {
            lock (_lock)
            {
                return _favoriteAssets.Any(f => f.AssetGuid == assetGuid);
            }
        }
        
        public static void ClearAll()
        {
            lock (_lock)
            {
                _favoriteAssets.Clear();
                SaveFavorites();
            }
        }
        
        private static void LoadFavorites()
        {
            lock (_lock)
            {
                _favoriteAssets = new List<FavoriteAssetData>();
                
                var dataPath = GetDataPath();
                if (File.Exists(dataPath))
                {
                    try
                    {
                        var json = File.ReadAllText(dataPath);
                        var wrapper = JsonUtility.FromJson<FavoriteAssetsWrapper>(json);
                        if (wrapper != null && wrapper.favorites != null)
                        {
                            _favoriteAssets = wrapper.favorites.Where(f => f.IsValid()).ToList();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to load favorite assets data: {e.Message}");
                    }
                }
            }
        }
        
        private static void SaveFavorites()
        {
            lock (_lock)
            {
                try
                {
                    var dataPath = GetDataPath();
                    var directory = Path.GetDirectoryName(dataPath);
                    if (! Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    var wrapper = new FavoriteAssetsWrapper { favorites = _favoriteAssets };
                    var json = JsonUtility.ToJson(wrapper, true);
                    File.WriteAllText(dataPath, json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to save favorite assets data: {e.Message}");
                }
            }
        }
        
        private static string GetDataPath()
        {
            return Path.Combine(Application.persistentDataPath, "Editor", _kDataFileName);
        }
        
        [System.Serializable]
        private class FavoriteAssetsWrapper
        {
            public List<FavoriteAssetData> favorites = new List<FavoriteAssetData>();
        }
    }
}