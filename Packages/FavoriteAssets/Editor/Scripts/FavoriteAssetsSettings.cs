using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    public static class FavoriteAssetsSettings
    {
        public const string PreferencesPath = "Preferences/Favorite Assets";

        private const string _kSelectOnClickKey = "FavoriteAssets.SelectOnClick";
        private const string _kRecordPrefabHistoryKey = "FavoriteAssets.RecordPrefabHistory";
        private const string _kMaxPrefabHistoryEntriesKey = "FavoriteAssets.MaxPrefabHistoryEntries";

        private const int _kDefaultMaxPrefabHistoryEntries = 20;
        private const int _kMinPrefabHistoryEntries = 5;
        private const int _kMaxPrefabHistoryEntries = 200;

        /// <summary>
        /// When false (default), clicking a favorite only highlights (pings) the asset in the Project window.
        /// When true, clicking also selects the asset so it shows in the Inspector.
        /// </summary>
        public static bool SelectOnClick
        {
            get => EditorPrefs.GetBool(_kSelectOnClickKey, false);
            set => EditorPrefs.SetBool(_kSelectOnClickKey, value);
        }

        /// <summary>
        /// When true (default), opening a prefab in Prefab Mode records it in the Prefab History tab.
        /// </summary>
        public static bool RecordPrefabHistory
        {
            get => EditorPrefs.GetBool(_kRecordPrefabHistoryKey, true);
            set => EditorPrefs.SetBool(_kRecordPrefabHistoryKey, value);
        }

        /// <summary>
        /// How many prefabs the history keeps before dropping the oldest.
        /// </summary>
        public static int MaxPrefabHistoryEntries
        {
            get => Mathf.Clamp(
                EditorPrefs.GetInt(_kMaxPrefabHistoryEntriesKey, _kDefaultMaxPrefabHistoryEntries),
                _kMinPrefabHistoryEntries,
                _kMaxPrefabHistoryEntries);
            set => EditorPrefs.SetInt(
                _kMaxPrefabHistoryEntriesKey,
                Mathf.Clamp(value, _kMinPrefabHistoryEntries, _kMaxPrefabHistoryEntries));
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(PreferencesPath, SettingsScope.User)
            {
                label = "Favorite Assets",
                keywords = new HashSet<string>
                {
                    "favorite", "assets", "select", "click", "ping", "highlight",
                    "prefab", "history", "recent"
                },
                guiHandler = _ =>
                {
                    var previousLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 220f;

                    EditorGUILayout.Space();

                    SelectOnClick = EditorGUILayout.Toggle(
                        new GUIContent(
                            "Select Asset on Click",
                            "When enabled, clicking a favorite also selects the asset so it shows in the Inspector. " +
                            "When disabled, clicking only highlights it in the Project window."),
                        SelectOnClick);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Prefab History", EditorStyles.boldLabel);

                    RecordPrefabHistory = EditorGUILayout.Toggle(
                        new GUIContent(
                            "Record Prefab History",
                            "When enabled, every prefab you open in Prefab Mode is recorded in the Prefab History tab."),
                        RecordPrefabHistory);

                    EditorGUI.BeginChangeCheck();
                    var maxEntries = EditorGUILayout.IntSlider(
                        new GUIContent(
                            "Max History Entries",
                            "How many prefabs the history keeps before dropping the oldest."),
                        MaxPrefabHistoryEntries,
                        _kMinPrefabHistoryEntries,
                        _kMaxPrefabHistoryEntries);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MaxPrefabHistoryEntries = maxEntries;
                        PrefabHistoryManager.TrimToCap();
                        FavoriteAssetsWindow.RefreshOpenWindows();
                    }

                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            };
        }
    }
}
