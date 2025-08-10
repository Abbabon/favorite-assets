using System;
using System.IO;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    [Serializable]
    public class FavoriteAssetData
    {
        [SerializeField] private string _assetPath;
        [SerializeField] private string _assetName;
        [SerializeField] private string _assetType;
        [SerializeField] private string _assetGuid;
        [SerializeField] private DateTime _dateAdded;
        [SerializeField] private DateTime _dateUpdated;
        
        public string AssetPath => _assetPath;
        public string AssetName => _assetName;
        public string AssetType => _assetType;
        public string AssetGuid => _assetGuid;
        public DateTime DateAdded => _dateAdded;
        public DateTime DateUpdated => _dateUpdated == default ? _dateAdded : _dateUpdated;
        
        public DateTime FileModificationDate
        {
            get
            {
                try
                {
                    if (File.Exists(_assetPath))
                    {
                        return File.GetLastWriteTime(_assetPath);
                    }
                    else if (Directory.Exists(_assetPath))
                    {
                        return Directory.GetLastWriteTime(_assetPath);
                    }
                }
                catch
                {
                    // If we can't get file modification date, fall back to our tracked date
                }
                return DateUpdated;
            }
        }
        
        public FavoriteAssetData(string assetPath, string assetName, string assetType, string assetGuid)
        {
            _assetPath = assetPath;
            _assetName = assetName;
            _assetType = assetType;
            _assetGuid = assetGuid;
            _dateAdded = DateTime.Now;
            _dateUpdated = DateTime.Now;
        }
        
        public void UpdateAccessDate()
        {
            _dateUpdated = DateTime.Now;
        }
        
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_assetPath) || string.IsNullOrEmpty(_assetGuid))
                return false;
                
            // Check if the asset still exists in the project
            try
            {
                // Check if the GUID still points to a valid asset
                var pathFromGuid = UnityEditor.AssetDatabase.GUIDToAssetPath(_assetGuid);
                if (string.IsNullOrEmpty(pathFromGuid))
                    return false;
                
                // Check if the file/folder actually exists on disk
                return File.Exists(_assetPath) || Directory.Exists(_assetPath);
            }
            catch
            {
                return false;
            }
        }
    }
}