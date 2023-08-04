using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class ExportUI : EditorWindow
    {
        private string _separator = ";";
        private Vector2 _scrollPos;
        private List<AssetInfo> _assets;
        private List<ED> _exportFields;
        private string[] _exportOptions;
        private int _selectedExportOption;
        private bool _addHeader = true;
        private bool _showFields;

        public static ExportUI ShowWindow()
        {
            ExportUI window = GetWindow<ExportUI>("Export Asset Data");
            window.minSize = new Vector2(300, 300);

            return window;
        }

        public void Init(List<AssetInfo> assets)
        {
            _assets = assets;

            _exportOptions = new[] {"CSV"};
            _exportFields = new List<ED>
            {
                new ED("Asset/Id"),
                new ED("Asset/ForeignId"),
                new ED("Asset/AssetRating"),
                new ED("Asset/AssetSource"),
                new ED("Asset/CompatibilityInfo", false),
                new ED("Asset/CurrentState", false),
                new ED("Asset/CurrentSubState", false),
                new ED("Asset/Description", false),
                new ED("Asset/DisplayCategory"),
                new ED("Asset/DisplayName"),
                new ED("Asset/DisplayPublisher"),
                new ED("Asset/ETag", false),
                new ED("Asset/Exclude", false),
                new ED("Asset/Hue", false),
                new ED("Asset/IsHidden", false),
                new ED("Asset/IsLatestVersion"),
                new ED("Asset/KeyFeatures", false),
                new ED("Asset/Keywords"),
                new ED("Asset/LastOnlineRefresh", false),
                new ED("Asset/LastRelease"),
                new ED("Asset/LatestVersion"),
                new ED("Asset/License"),
                new ED("Asset/LicenseLocation", false),
                new ED("Asset/Location"),
                new ED("Asset/MainImage", false),
                new ED("Asset/MainImageIcon", false),
                new ED("Asset/MainImageSmall", false),
                new ED("Asset/OriginalLocation", false),
                new ED("Asset/OriginalLocationKey", false),
                new ED("Asset/PackageSize", false),
                new ED("Asset/PackageSource"),
                new ED("Asset/PreferredVersion"),
                new ED("Asset/PreviewImage", false),
                new ED("Asset/RatingCount"),
                new ED("Asset/Registry", false),
                new ED("Asset/ReleaseNotes", false),
                new ED("Asset/Repository", false),
                new ED("Asset/Revision"),
                new ED("Asset/SafeCategory"),
                new ED("Asset/SafeName"),
                new ED("Asset/SafePublisher"),
                new ED("Asset/Slug", false),
                new ED("Asset/SupportedUnityVersions"),
                new ED("Asset/Version")
            };
        }

        public void OnGUI()
        {
            int width = 70;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel, GUILayout.MaxWidth(width));
            EditorGUILayout.LabelField($"-All- ({_assets.Count})");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format", EditorStyles.boldLabel, GUILayout.Width(width));
            _selectedExportOption = EditorGUILayout.Popup(_selectedExportOption, _exportOptions, GUILayout.Width(87));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Header", EditorStyles.boldLabel, GUILayout.Width(width));
            _addHeader = EditorGUILayout.Toggle(_addHeader);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            _showFields = EditorGUILayout.Foldout(_showFields, "Fields");
            if (_showFields)
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All")) _exportFields.ForEach(f => f.isSelected = true);
                if (GUILayout.Button("Select None")) _exportFields.ForEach(f => f.isSelected = false);
                if (GUILayout.Button("Select Default")) _exportFields.ForEach(f => f.isSelected = f.isDefault);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
                foreach (ED ed in _exportFields)
                {
                    GUILayout.BeginHorizontal();
                    ed.isSelected = EditorGUILayout.Toggle(ed.isSelected, GUILayout.Width(20));
                    EditorGUILayout.LabelField(ed.field);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Export...", GUILayout.Height(50))) Export();
        }

        private void Export()
        {
            string file = EditorUtility.SaveFilePanel("Target file", null, "assets", "csv");
            if (string.IsNullOrEmpty(file)) return;

            List<string> result = new List<string>();

            if (_addHeader)
            {
                List<object> line = new List<object>();
                foreach (ED field in _exportFields.Where(f => f.isSelected))
                {
                    line.Add(field.field);
                }
                result.Add(string.Join(_separator, line));
            }

            foreach (AssetInfo info in _assets.Where(a => a.SafeName != Asset.NONE))
            {
                List<object> line = new List<object>();
                foreach (ED field in _exportFields.Where(f => f.isSelected))
                {
                    switch (field.field)
                    {
                        default:
                            line.Add(field.FieldInfo?.GetValue(info));
                            break;
                    }

                    // make sure delimiter and line breaks are not used 
                    if (line.Last() is string s)
                    {
                        line[line.Count - 1] = s.Replace(_separator, ",").Replace("\n", string.Empty).Replace("\r", string.Empty);
                    }
                }
                result.Add(string.Join(_separator, line));
            }
            File.WriteAllLines(file, result);
            EditorUtility.RevealInFinder(file);
        }
    }

    [Serializable]
    public sealed class ED
    {
        public string pointer;
        public string table;
        public string field;
        public bool isDefault;
        public bool isSelected;

        public PropertyInfo FieldInfo
        {
            get
            {
                if (_fieldInfo == null) _fieldInfo = typeof(AssetInfo).GetProperty(field);
                return _fieldInfo;
            }
        }

        private PropertyInfo _fieldInfo;

        public ED(string pointer, bool isDefault = true)
        {
            this.isDefault = isDefault;
            this.pointer = pointer;

            isSelected = isDefault;

            table = pointer.Split('/')[0];
            field = pointer.Split('/')[1];
        }
    }
}