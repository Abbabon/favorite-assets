using System;
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
        
        public string AssetPath => _assetPath;
        public string AssetName => _assetName;
        public string AssetType => _assetType;
        public string AssetGuid => _assetGuid;
        public DateTime DateAdded => _dateAdded;
        
        public FavoriteAssetData(string assetPath, string assetName, string assetType, string assetGuid)
        {
            _assetPath = assetPath;
            _assetName = assetName;
            _assetType = assetType;
            _assetGuid = assetGuid;
            _dateAdded = DateTime.Now;
        }
        
        public bool IsValid()
        {
            return ! string.IsNullOrEmpty(_assetPath) && 
                   ! string.IsNullOrEmpty(_assetGuid);
        }
    }
}