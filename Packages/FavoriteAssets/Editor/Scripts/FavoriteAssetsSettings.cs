using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    public static class FavoriteAssetsSettings
    {
        public const string PreferencesPath = "Preferences/Favorite Assets";

        private const string _kSelectOnClickKey = "FavoriteAssets.SelectOnClick";

        /// <summary>
        /// When false (default), clicking a favorite only highlights (pings) the asset in the Project window.
        /// When true, clicking also selects the asset so it shows in the Inspector.
        /// </summary>
        public static bool SelectOnClick
        {
            get => EditorPrefs.GetBool(_kSelectOnClickKey, false);
            set => EditorPrefs.SetBool(_kSelectOnClickKey, value);
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(PreferencesPath, SettingsScope.User)
            {
                label = "Favorite Assets",
                keywords = new HashSet<string> { "favorite", "assets", "select", "click", "ping", "highlight" },
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

                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            };
        }
    }
}
