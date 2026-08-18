using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    /// <summary>
    /// Persisted, most-recent-first log of prefabs opened in Prefab Mode.
    /// Stored per project under UserSettings/ because prefab GUIDs are project-scoped.
    /// </summary>
    public static class PrefabHistoryManager
    {
        private const string _kDataFileName = "FavoriteAssetsPrefabHistory.json";

        private static List<PrefabHistoryEntry> _entries = new List<PrefabHistoryEntry>();
        private static readonly object _lock = new object();

        static PrefabHistoryManager()
        {
            LoadHistory();
        }

        /// <summary>
        /// Newest first.
        /// </summary>
        public static List<PrefabHistoryEntry> GetEntries()
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }

        public static int Count
        {
            get
            {
                lock (_lock)
                {
                    return _entries.Count;
                }
            }
        }

        /// <summary>
        /// Records a visit to the prefab at <paramref name="assetPath"/>.
        /// One entry per GUID: revisiting an existing prefab moves it back to the top.
        /// Returns true when the stored history changed.
        /// </summary>
        public static bool Record(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return false;

            lock (_lock)
            {
                var existingIndex = _entries.FindIndex(e => e.PrefabGuid == guid);
                if (existingIndex >= 0)
                {
                    var existing = _entries[existingIndex];
                    existing.Touch(assetPath);

                    // Already the most recent visit - refresh the timestamp but report no change so
                    // that reopening a prefab from the history tab does not churn the UI.
                    if (existingIndex == 0)
                    {
                        SaveHistory();
                        return false;
                    }

                    _entries.RemoveAt(existingIndex);
                    _entries.Insert(0, existing);
                    SaveHistory();
                    return true;
                }

                var name = Path.GetFileNameWithoutExtension(assetPath);
                _entries.Insert(0, new PrefabHistoryEntry(guid, assetPath, name));
                TrimToCapInternal();
                SaveHistory();
                return true;
            }
        }

        public static bool Remove(string prefabGuid)
        {
            if (string.IsNullOrEmpty(prefabGuid))
                return false;

            lock (_lock)
            {
                var removed = _entries.RemoveAll(e => e.PrefabGuid == prefabGuid) > 0;
                if (removed)
                {
                    SaveHistory();
                }
                return removed;
            }
        }

        public static void ClearAll()
        {
            lock (_lock)
            {
                _entries.Clear();
                SaveHistory();
            }
        }

        /// <summary>
        /// Drops the oldest entries beyond the configured cap. Called when the cap preference changes.
        /// </summary>
        public static void TrimToCap()
        {
            lock (_lock)
            {
                if (TrimToCapInternal())
                {
                    SaveHistory();
                }
            }
        }

        private static bool TrimToCapInternal()
        {
            var cap = FavoriteAssetsSettings.MaxPrefabHistoryEntries;
            if (_entries.Count <= cap)
                return false;

            _entries.RemoveRange(cap, _entries.Count - cap);
            return true;
        }

        private static void LoadHistory()
        {
            try
            {
                var dataPath = GetDataPath();
                if (!File.Exists(dataPath))
                {
                    _entries = new List<PrefabHistoryEntry>();
                    return;
                }

                var json = File.ReadAllText(dataPath);
                var wrapper = JsonUtility.FromJson<PrefabHistoryWrapper>(json);
                _entries = wrapper?.entries ?? new List<PrefabHistoryEntry>();

                // The cap may have been lowered while this file was larger.
                TrimToCapInternal();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load prefab history: {e.Message}");
                _entries = new List<PrefabHistoryEntry>();
            }
        }

        private static void SaveHistory()
        {
            try
            {
                var dataPath = GetDataPath();
                var directory = Path.GetDirectoryName(dataPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var wrapper = new PrefabHistoryWrapper { entries = _entries };
                File.WriteAllText(dataPath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save prefab history: {e.Message}");
            }
        }

        private static string GetDataPath()
        {
            // Application.dataPath is <project>/Assets, so its parent is the project root.
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, "UserSettings", _kDataFileName);
        }

        [Serializable]
        private class PrefabHistoryWrapper
        {
            public int version = 1;
            public List<PrefabHistoryEntry> entries = new List<PrefabHistoryEntry>();
        }
    }
}
