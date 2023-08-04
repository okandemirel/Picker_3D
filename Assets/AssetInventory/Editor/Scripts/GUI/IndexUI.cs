using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JD.EditorAudioUtils;
using SQLite;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Random = UnityEngine.Random;

namespace AssetInventory
{
    public sealed class IndexUI : BasicEditorUI
    {
        private const float CHECK_INTERVAL = 2f;

        private readonly Dictionary<string, string> _staticPreviews = new Dictionary<string, string>
        {
            {"cs", "cs Script Icon"},
            {"php", "TextAsset Icon"},
            {"cg", "TextAsset Icon"},
            {"cginc", "TextAsset Icon"},
            {"js", "d_Js Script Icon"},
            {"prefab", "d_Prefab Icon"},
            {"png", "d_RawImage Icon"},
            {"jpg", "d_RawImage Icon"},
            {"gif", "d_RawImage Icon"},
            {"tga", "d_RawImage Icon"},
            {"tiff", "d_RawImage Icon"},
            {"ico", "d_RawImage Icon"},
            {"bmp", "d_RawImage Icon"},
            {"fbx", "d_PrefabModel Icon"},
            {"dll", "dll Script Icon"},
            {"meta", "MetaFile Icon"},
            {"unity", "d_SceneAsset Icon"},
            {"asset", "EditorSettings Icon"},
            {"txt", "TextScriptImporter Icon"},
            {"md", "TextScriptImporter Icon"},
            {"doc", "TextScriptImporter Icon"},
            {"docx", "TextScriptImporter Icon"},
            {"pdf", "TextScriptImporter Icon"},
            {"rtf", "TextScriptImporter Icon"},
            {"readme", "TextScriptImporter Icon"},
            {"chm", "TextScriptImporter Icon"},
            {"compute", "ComputeShader Icon"},
            {"shader", "Shader Icon"},
            {"shadergraph", "Shader Icon"},
            {"shadersubgraph", "Shader Icon"},
            {"mat", "d_Material Icon"},
            {"wav", "AudioImporter Icon"},
            {"mp3", "AudioImporter Icon"},
            {"ogg", "AudioImporter Icon"},
            {"xml", "UxmlScript Icon"},
            {"html", "UxmlScript Icon"},
            {"uss", "UssScript Icon"},
            {"css", "StyleSheet Icon"},
            {"json", "StyleSheet Icon"},
            {"exr", "d_ReflectionProbe Icon"}
        };

        private List<AssetInfo> _files;
        private List<Tag> _tags;
        private GUIContent[] _contents;
        private string[] _assetNames;
        private string[] _tagNames;
        private string[] _publisherNames;
        private string[] _colorOptions;
        private string[] _categoryNames;
        private string[] _types;
        private string[] _resultSizes;
        private string[] _sortFields;
        private string[] _searchFields;
        private string[] _tileTitle;
        private string[] _previewOptions;
        private string[] _assetSortOptions;
        private string[] _groupByOptions;
        private string[] _packageListingOptions;
        private string[] _deprecationOptions;
        private string[] _maintenanceOptions;
        private string[] _importDestinationOptions;
        private string[] _importStructureOptions;
        private string[] _expertSearchFields;

        private int _gridSelection;
        private string _searchPhrase;
        private string _searchWidth;
        private string _searchHeight;
        private string _searchLength;
        private string _searchSize;
        private string _assetSearchPhrase;
        private bool _checkMaxWidth;
        private bool _checkMaxHeight;
        private bool _checkMaxLength;
        private bool _checkMaxSize;
        private int _selectedPublisher;
        private int _selectedCategory;
        private int _selectedExpertSearchField;
        private int _selectedAsset;
        private int _selectedPackageTypes = 1;
        private int _selectedPackageTag;
        private int _selectedFileTag;
        private int _selectedMaintenance;
        private int _selectedColorOption;
        private Color _selectedColor;
        private bool _showSettings;
        private int _tab;
        private string _newTag;
        private int _lastMainProgress;
        private bool _usageCalculationInProgress;
        private string _importFolder;

        private Vector2 _searchScrollPos;
        private Vector2 _folderScrollPos;
        private Vector2 _assetScrollPos;
        private Vector2 _reportScrollPos;
        private Vector2 _inspectorScrollPos;
        private Vector2 _usedAssetsScrollPos;
        private Vector2 _assetsScrollPos;
        private Vector2 _statsScrollPos;
        private Vector2 _settingsScrollPos;
        private Vector2 _bulkScrollPos;

        private int _curPage = 1;
        private int _pageCount;

        private bool _previewInProgress;
        private EditorCoroutine _textureLoading;
        private EditorCoroutine _textureLoading2;
        private EditorCoroutine _textureLoading3;

        private string[] _pvSelection;
        private string _pvSelectedPath;
        private string _pvSelectedFolder;
        private bool _pvSelectionChanged;
        private List<AssetInfo> _pvSelectedAssets;
        private int _selectedFolderIndex = -1;
        private AssetInfo _selectedEntry;
        private AssetInfo _selectedReportEntry;
        private bool _showMaintenance;
        private bool _showDiskSpace;
        private bool _packageAvailable;
        private long _dbSize;
        private long _backupSize;
        private long _cacheSize;
        private long _persistedCacheSize;
        private long _previewSize;
        private int _resultCount;
        private int _packageCount;
        private int _deprecatedAssetsCount;
        private int _excludedAssetsCount;
        private int _registryPackageCount;
        private int _customPackageCount;
        private int _packageFileCount;
        private int _availablePackageUpdates;
        private int _activePackageDownloads;

        private AssetPurchases _purchasedAssets = new AssetPurchases();
        private int _purchasedAssetsCount;
        private List<AssetInfo> _assets;
        private int _indexedPackageCount;

        private List<AssetInfo> _assetUsage;
        private List<string> _usedAssets;
        private List<AssetInfo> _identifiedFiles;

        private SearchField SearchField => _searchField = _searchField ?? new SearchField();
        private SearchField _searchField;
        private SearchField AssetSearchField => _assetSearchField = _assetSearchField ?? new SearchField();
        private SearchField _assetSearchField;

        private float _nextSearchTime;

        [SerializeField] private MultiColumnHeaderState assetMchState;
        private Rect AssetTreeRect => new Rect(20, 0, position.width - 40, position.height - 60);
        private TreeViewWithTreeModel<AssetInfo> AssetTreeView
        {
            get
            {
                if (_assetTreeViewState == null) _assetTreeViewState = new TreeViewState();

                MultiColumnHeaderState headerState = AssetTreeViewControl.CreateDefaultMultiColumnHeaderState(AssetTreeRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(assetMchState, headerState)) MultiColumnHeaderState.OverwriteSerializedFields(assetMchState, headerState);
                assetMchState = headerState;

                if (_assetTreeView == null)
                {
                    MultiColumnHeader mch = new MultiColumnHeader(headerState);
                    mch.canSort = false;
                    mch.height = MultiColumnHeader.DefaultGUI.minimumHeight;
                    mch.ResizeToFit();

                    _assetTreeView = new AssetTreeViewControl(_assetTreeViewState, mch, AssetTreeModel);
                    _assetTreeView.OnSelectionChanged += OnAssetTreeSelectionChanged;
                    _assetTreeView.OnDoubleClickedItem += OnAssetTreeDoubleClicked;
                    _assetTreeView.Reload();
                }
                return _assetTreeView;
            }
        }

        private TreeViewWithTreeModel<AssetInfo> _assetTreeView;
        private TreeViewState _assetTreeViewState;

        private TreeModel<AssetInfo> AssetTreeModel
        {
            get
            {
                if (_assetTreeModel == null) _assetTreeModel = new TreeModel<AssetInfo>(new List<AssetInfo> {new AssetInfo().WithTreeData("Root", depth: -1)});
                return _assetTreeModel;
            }
        }
        private TreeModel<AssetInfo> _assetTreeModel;

        private AssetInfo _selectedTreeAsset;
        private List<AssetInfo> _selectedTreeAssets;
        private List<AssetInfo> _selectedReportEntries;

        private long _assetTreeSelectionSize;
        private long _reportTreeSelectionSize;
        private readonly Dictionary<string, Tuple<int, Color>> _assetBulkTags = new Dictionary<string, Tuple<int, Color>>();
        private readonly Dictionary<string, Tuple<int, Color>> _reportBulkTags = new Dictionary<string, Tuple<int, Color>>();

        [SerializeField] private MultiColumnHeaderState reportMchState;
        private Rect ReportTreeRect => new Rect(20, 0, position.width - 40, position.height - 60);
        private TreeViewWithTreeModel<AssetInfo> ReportTreeView
        {
            get
            {
                if (_reportTreeViewState == null) _reportTreeViewState = new TreeViewState();

                MultiColumnHeaderState headerState = ReportTreeViewControl.CreateDefaultMultiColumnHeaderState(ReportTreeRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(reportMchState, headerState)) MultiColumnHeaderState.OverwriteSerializedFields(reportMchState, headerState);
                reportMchState = headerState;

                if (_reportTreeView == null)
                {
                    MultiColumnHeader mch = new MultiColumnHeader(headerState);
                    mch.canSort = false;
                    mch.height = MultiColumnHeader.DefaultGUI.minimumHeight;
                    mch.ResizeToFit();

                    _reportTreeView = new ReportTreeViewControl(_reportTreeViewState, mch, ReportTreeModel);
                    _reportTreeView.OnSelectionChanged += OnReportTreeSelectionChanged;
                    _reportTreeView.OnDoubleClickedItem += OnReportTreeDoubleClicked;
                    _reportTreeView.Reload();
                }
                return _reportTreeView;
            }
        }
        private TreeViewWithTreeModel<AssetInfo> _reportTreeView;
        private TreeViewState _reportTreeViewState;

        private TreeModel<AssetInfo> ReportTreeModel
        {
            get
            {
                if (_reportTreeModel == null) _reportTreeModel = new TreeModel<AssetInfo>(new List<AssetInfo> {new AssetInfo().WithTreeData("Root", depth: -1)});
                return _reportTreeModel;
            }
        }
        private TreeModel<AssetInfo> _reportTreeModel;

        private sealed class AdditionalFoldersWrapper : ScriptableObject
        {
            public List<FolderSpec> folders = new List<FolderSpec>();
        }

        private ReorderableList FolderListControl
        {
            get
            {
                if (_folderListControl == null) InitFolderControl();
                return _folderListControl;
            }
        }

        private ReorderableList _folderListControl;

        private SerializedObject SerializedFoldersObject
        {
            get
            {
                // reference can become null on reload
                if (_serializedFoldersObject == null || _serializedFoldersObject.targetObjects.FirstOrDefault() == null) InitFolderControl();
                return _serializedFoldersObject;
            }
        }

        private SerializedObject _serializedFoldersObject;
        private SerializedProperty _foldersProperty;

        private static bool _requireAssetTreeRebuild;
        private static bool _requireReportTreeRebuild;
        private static bool _requireLookupUpdate;
        private static bool _requireSearchUpdate;
        private static bool _keepSearchResultPage = true;
        private int _awakeTime;
        private DateTime _lastCheck;
        private Rect _tagButtonRect;
        private Rect _tag2ButtonRect;
        private Rect _versionButtonRect;
        private Rect _pageButtonRect;
        private Rect _connectButtonRect;
        private bool _initDone;
        private bool _updateAvailable;
        private AssetDetails _onlineInfo;
        private bool _calculatingFolderSizes;
        private bool _cleanupInProgress;
        private DateTime _lastFolderSizeCalculation;
        private DateTime _lastTileSizeChange;
        private bool _mouseOverSearchResultRect;
        private bool _mouseOverAssetTreeRect;
        private bool _allowLogic;
        private string _searchError;
        private bool _dragging;
        private bool _searchDone;

        [MenuItem("Assets/Asset Inventory", priority = 9000)]
        public static void ShowWindow()
        {
            IndexUI window = GetWindow<IndexUI>("Asset Inventory");
            window.minSize = new Vector2(550, 200);
        }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            _initDone = true;
            _awakeTime = Time.frameCount;
            AssetInventory.Init();
            InitFolderControl();

            _requireLookupUpdate = true;
            _requireSearchUpdate = true;

            CheckForUpdates();
        }

        private void OnEnable()
        {
            AssetInventory.OnIndexingDone += OnTagsChanged;
            AssetInventory.OnTagsChanged += OnTagsChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssetStore.OnPackageListUpdated += OnPackageListUpdated;
        }

        private void OnDisable()
        {
            AssetInventory.OnIndexingDone -= OnTagsChanged;
            AssetInventory.OnTagsChanged -= OnTagsChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssetStore.OnPackageListUpdated -= OnPackageListUpdated;

            EditorAudioUtility.StopAllPreviewClips();
        }

        private void OnPackageListUpdated()
        {
            if (_assets == null) return;

            List<PackageInfo> packages = AssetStore.GetPackages();
            foreach (PackageInfo package in packages)
            {
                AssetInfo info = _assets.FirstOrDefault(a => a.AssetSource == Asset.Source.Package && a.SafeName == package.name);
                if (info == null) continue;

                if (package.versions.latestCompatible != info.LatestVersion && !package.versions.latestCompatible.ToLowerInvariant().Contains("pre"))
                {
                    AssetInventory.SetPackageVersion(info, package);

                    Debug.Log($"Found new available version for '{info.GetDisplayName()}': {package.version}");
                }
            }
        }

        private void OnTagsChanged()
        {
            _requireLookupUpdate = true;
            _requireAssetTreeRebuild = true;
        }

        private void InitFolderControl()
        {
            AdditionalFoldersWrapper obj = CreateInstance<AdditionalFoldersWrapper>();
            obj.folders = AssetInventory.Config.folders;

            _serializedFoldersObject = new SerializedObject(obj);
            _foldersProperty = _serializedFoldersObject.FindProperty("folders");
            _folderListControl = new ReorderableList(_serializedFoldersObject, _foldersProperty, true, true, true, true);
            _folderListControl.drawElementCallback = DrawFoldersListItems;
            _folderListControl.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Additional Folders to Index");
            _folderListControl.onAddCallback = OnAddCallback;
            _folderListControl.onRemoveCallback = OnRemoveCallback;
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            if (_selectedFolderIndex < 0 || _selectedFolderIndex >= AssetInventory.Config.folders.Count) return;
            AssetInventory.Config.folders.RemoveAt(_selectedFolderIndex);
            AssetInventory.SaveConfig();
        }

        private void OnAddCallback(ReorderableList list)
        {
            string folder = EditorUtility.OpenFolderPanel("Select folder to index", "", "");
            if (string.IsNullOrEmpty(folder)) return;

            // make absolute and conform to OS separators
            folder = Path.GetFullPath(folder);

            // special case: a relative key is already defined for the folder to be added, replace it immediately
            folder = AssetInventory.MakeRelative(folder);

            FolderSpec spec = new FolderSpec();
            spec.location = folder;
            if (AssetInventory.IsRel(folder))
            {
                spec.storeRelative = true;
                spec.relativeKey = AssetInventory.GetRelKey(folder);
            }
            AssetInventory.Config.folders.Add(spec);
            AssetInventory.SaveConfig();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            EditorAudioUtility.StopAllPreviewClips();

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // will crash editor otherwise
                if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
                if (_textureLoading2 != null) EditorCoroutineUtility.StopCoroutine(_textureLoading2);
            }

            // UI will have lost all preview textures during play mode
            if (state == PlayModeStateChange.ExitingPlayMode) _requireSearchUpdate = true;
        }

        private void ReloadLookups()
        {
            if (AssetInventory.DEBUG_MODE) Debug.LogWarning("Reload Lookups");

            _requireLookupUpdate = false;
            _resultSizes = new[] {"-all-", string.Empty, "10", "25", "50", "100", "250", "500", "1000", "1500", "2000", "2500", "3000", "4000", "5000"};
            _searchFields = new[] {"Asset Path", "File Name"};
            _sortFields = new[] {"Asset Path", "File Name", "Size", "Type", "Length", "Width", "Height", "Color", "Category", "Last Updated", "Rating", "#Ratings", string.Empty, "-unsorted-"};
            _assetSortOptions = new[] {"Name", "Purchase Date", "Last Update"};
            _groupByOptions = new[] {"-none-", string.Empty, "Category", "Publisher", "Tag", "State"};
            _colorOptions = new[] {"-all-", "matching"};
            _tileTitle = new[] {"-Intelligent-", string.Empty, "Asset Path", "File Name", "File Name without Extension", string.Empty, "None"};
            _previewOptions = new[] {"-all-", string.Empty, "Only With Preview", "Only Without Preview"};
            _packageListingOptions = new[] {"-all-", "Exclude Registry Packages", "Only Registry Packages", "Only Custom Packages", "Only Media Folders", "Only Archives"};
            _deprecationOptions = new[] {"-all-", "Exclude Deprecated", "Show Only Deprecated"};
            _maintenanceOptions = new[] {"-all-", "Update Available", "Outdated in Unity Cache", "Disabled by Unity", "Custom Asset Store Link", "Indexed", "Not Indexed", "Custom Registry", "Installed", "Downloading", "Not Downloaded", "Duplicate", "Marked for Backup", "Not Marked for Backup"};
            _importDestinationOptions = new[] {"Into Folder Selected in Project View", "Into Assets Root", "Into Specific Folder"};
            _importStructureOptions = new[] {"All Files flat in Target Folder", "Keep Original Folder Structure"};
            _expertSearchFields = new[]
            {
                "-Add Field-", string.Empty,
                "Asset/AssetRating", "Asset/AssetSource", "Asset/CompatibilityInfo", "Asset/CurrentState", "Asset/CurrentSubState", "Asset/Description", "Asset/DisplayCategory", "Asset/DisplayName", "Asset/DisplayPublisher", "Asset/ETag", "Asset/Exclude", "Asset/ForeignId", "Asset/Hue", "Asset/Id", "Asset/IsHidden", "Asset/IsLatestVersion", "Asset/KeyFeatures", "Asset/Keywords", "Asset/LastOnlineRefresh", "Asset/LastRelease", "Asset/LatestVersion", "Asset/License", "Asset/LicenseLocation", "Asset/Location", "Asset/MainImage", "Asset/MainImageIcon", "Asset/MainImageSmall", "Asset/OriginalLocation", "Asset/OriginalLocationKey", "Asset/PackageSize", "Asset/PackageSource", "Asset/PreferredVersion", "Asset/PreviewImage", "Asset/RatingCount", "Asset/Registry", "Asset/ReleaseNotes", "Asset/Repository", "Asset/Revision", "Asset/SafeCategory", "Asset/SafeName", "Asset/SafePublisher", "Asset/Slug", "Asset/SupportedUnityVersions", "Asset/Version",
                "AssetFile/AssetId", "AssetFile/Hue", "AssetFile/FileName", "AssetFile/Guid", "AssetFile/Height", "AssetFile/Id", "AssetFile/Length", "AssetFile/Path", "AssetFile/PreviewState", "AssetFile/Size", "AssetFile/SourcePath", "AssetFile/Type",
                "Tag/Color", "Tag/FromAssetStore", "Tag/Id", "Tag/Name",
                "TagAssignment/Id", "TagAssignment/TagId", "TagAssignment/TagTarget", "TagAssignment/TagTargetId"
            };

            UpdateStatistics();
            AssetStore.FillBufferOnDemand();

            _assetNames = AssetInventory.ExtractAssetNames(_assets);
            _publisherNames = AssetInventory.ExtractPublisherNames(_assets);
            _categoryNames = AssetInventory.ExtractCategoryNames(_assets);
            _types = AssetInventory.LoadTypes();
            _tagNames = AssetInventory.ExtractTagNames(_tags);
            _purchasedAssetsCount = AssetInventory.CountPurchasedAssets(_assets);
        }

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            _requireAssetTreeRebuild = true;
            _requireReportTreeRebuild = true;
            _requireSearchUpdate = true;
            PreviewGenerator.Clear();
            AssetInventory.Init();
            EditorAudioUtility.StopAllPreviewClips();
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("The Asset Inventory is not available during play mode.", MessageType.Info);
                return;
            }

            _allowLogic = Event.current.type == EventType.Layout; // nothing must be changed during repaint
            if (!_initDone) Init(); // in some docking scenarios OnGUI is called before Awake

            if (UpgradeUtil.UpgradeRequired)
            {
                UpgradeUtil.DrawUpgradeRequired();
                return;
            }

            // determine import targets
            switch (AssetInventory.Config.importDestination)
            {
                case 0:
                    _importFolder = _pvSelectedFolder;
                    break;

                case 1:
                    _importFolder = "Assets";
                    break;

                case 2:
                    _importFolder = AssetInventory.Config.importFolder;
                    break;
            }

            if (Event.current.type == EventType.Repaint) _mouseOverSearchResultRect = false;
            if (DragDropAvailable()) HandleDragDrop();

            if (_requireLookupUpdate || _resultSizes == null || _resultSizes.Length == 0) ReloadLookups();
            if (_allowLogic)
            {
                if (_lastTileSizeChange != DateTime.MinValue && (DateTime.Now - _lastTileSizeChange).TotalMilliseconds > 300f)
                {
                    _requireSearchUpdate = true;
                    _lastTileSizeChange = DateTime.MinValue;
                }

                // don't perform more expensive checks every frame
                if ((DateTime.Now - _lastCheck).TotalSeconds > CHECK_INTERVAL)
                {
                    _availablePackageUpdates = _assets.Count(a => a.IsUpdateAvailable(_assets));
                    _activePackageDownloads = _assets?.Count(a => a.PackageDownloader != null && a.PackageDownloader.GetState().state == AssetDownloader.State.Downloading) ?? 0;
                    _lastCheck = DateTime.Now;
                }
            }

            UIStyles.tile.fixedHeight = AssetInventory.Config.tileSize;
            UIStyles.tile.fixedWidth = AssetInventory.Config.tileSize;

            bool isNewTab = DrawToolbar();
            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(CloudProjectSettings.accessToken))
            {
                EditorGUILayout.HelpBox("Asset Store connectivity is currently not possible. Please restart Unity and make sure you are logged in in the Unity hub.", MessageType.Warning);
                EditorGUILayout.Space();
            }

            // centrally handle project view selections since used in multiple views
            CheckProjectViewSelection();
            switch (_tab)
            {
                case 0:
                    if (_allowLogic && _requireSearchUpdate && AssetInventory.Config.searchAutomatically) PerformSearch(_keepSearchResultPage);
                    DrawSearchTab();
                    break;

                case 1:
                    // will have lost asset tree on reload due to missing serialization
                    if (_requireAssetTreeRebuild) CreateAssetTree();
                    DrawPackagesTab();
                    break;

                case 2:
                    if (_requireReportTreeRebuild) CreateReportTree();
                    DrawReportingTab();
                    break;

                case 3:
                    if (isNewTab) UpdateStatistics();
                    DrawSettingsTab();
                    break;

                case 4:
                    DrawAboutTab();
                    break;
            }

            // reload if there is new data
            if (_lastMainProgress != AssetProgress.MainProgress)
            {
                _lastMainProgress = AssetProgress.MainProgress;
                _requireLookupUpdate = true;
                _requireSearchUpdate = true;
            }

            if (_allowLogic)
            {
                // handle double-clicks
                if (Event.current.clickCount > 1)
                {
                    if (_mouseOverSearchResultRect && AssetInventory.Config.doubleClickImport && _selectedEntry != null)
                    {
                        PerformCopyTo(_selectedEntry, _importFolder);
                    }
                }
            }
        }

        private void CheckProjectViewSelection()
        {
            _pvSelection = Selection.assetGUIDs;
            string oldPvSelectedPath = _pvSelectedPath;
            _pvSelectedPath = null;
            if (_pvSelection != null && _pvSelection.Length > 0)
            {
                _pvSelectedPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                _pvSelectedFolder = Directory.Exists(_pvSelectedPath) ? _pvSelectedPath : Path.GetDirectoryName(_pvSelectedPath);
                if (!string.IsNullOrWhiteSpace(_pvSelectedFolder)) _pvSelectedFolder = _pvSelectedFolder.Replace('/', Path.DirectorySeparatorChar);
            }
            _pvSelectionChanged = oldPvSelectedPath != _pvSelectedPath;
            if (_pvSelectionChanged) _pvSelectedAssets = null;
        }

        private bool DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            List<string> strings = new List<string>
            {
                "Search",
                "Packages",
                "Reporting",
                "Settings" + (AssetInventory.CurrentMain != null || AssetInventory.IndexingInProgress ? " (indexing)" : "")
            };
            _tab = GUILayout.Toolbar(_tab, strings.ToArray(), GUILayout.Height(32), GUILayout.MinWidth(500));

            bool newTab = EditorGUI.EndChangeCheck();

            GUILayout.FlexibleSpace();
            if (_updateAvailable && _onlineInfo != null && GUILayout.Button(UIStyles.Content($"v{_onlineInfo.version.name} available!", $"Released {_onlineInfo.version.publishedDate}"), EditorStyles.linkLabel))
            {
                Application.OpenURL(AssetInventory.ASSET_STORE_LINK);
            }
            if (_activePackageDownloads > 0 && GUILayout.Button(EditorGUIUtility.IconContent("Loading", $"|{_activePackageDownloads} Downloads Active"), EditorStyles.label))
            {
                _tab = 1;
                _selectedMaintenance = 9;
                _requireAssetTreeRebuild = true;
                AssetInventory.Config.showPackageFilterBar = true;
                AssetInventory.SaveConfig();
            }
            if (_availablePackageUpdates > 0 && GUILayout.Button(EditorGUIUtility.IconContent("Update-Available", $"|{_availablePackageUpdates} Updates Available"), EditorStyles.label))
            {
                _tab = 1;
                _selectedMaintenance = 1;
                _requireAssetTreeRebuild = true;
                AssetInventory.Config.showPackageFilterBar = true;
                AssetInventory.SaveConfig();
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("_Help", "|About"), EditorStyles.label)) _tab = 4;
            GUILayout.EndHorizontal();

            return newTab;
        }

        private void DrawSearchTab()
        {
            if (_packageFileCount == 0)
            {
                bool canStillSearch = AssetInventory.IndexingInProgress || _packageCount == 0 || AssetInventory.Config.indexAssetPackageContents;
                if (canStillSearch)
                {
                    EditorGUILayout.HelpBox("The search index needs to be initialized. Start it right from here or go to the Settings tab to configure the details.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The search is only available if package contents was indexed.", MessageType.Info);
                }

                EditorGUILayout.Space(30);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (canStillSearch)
                {
                    EditorGUILayout.Space(30);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(AssetInventory.IndexingInProgress);
                    if (GUILayout.Button("Start Indexing", GUILayout.Height(50), GUILayout.MaxWidth(400))) PerformFullUpdate();
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox("Since the search index is shared across Unity projects it is highly recommended for performance to perform initial indexing from an empty project on a new Unity version and if possible on an SSD drive.", MessageType.Warning);
                }
            }
            else
            {
                bool dirty = false;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(EditorGUIUtility.IconContent("Preset.Context", "|Search Filters")))
                {
                    AssetInventory.Config.showFilterBar = !AssetInventory.Config.showFilterBar;
                    AssetInventory.SaveConfig();
                    dirty = true;
                }
                EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
                EditorGUIUtility.labelWidth = 60;
                EditorGUI.BeginChangeCheck();
                _searchPhrase = SearchField.OnGUI(_searchPhrase, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    // delay search to allow fast typing
                    _nextSearchTime = Time.realtimeSinceStartup + AssetInventory.Config.searchDelay;
                }
                else if (_nextSearchTime > 0 && Time.realtimeSinceStartup > _nextSearchTime)
                {
                    _nextSearchTime = 0;
                    if (AssetInventory.Config.searchAutomatically && !_searchPhrase.StartsWith("=")) dirty = true;
                }
                if (_allowLogic && Event.current.keyCode == KeyCode.Return) dirty = true;
                if (!AssetInventory.Config.searchAutomatically)
                {
                    if (GUILayout.Button("Go", GUILayout.Width(30))) PerformSearch();
                }

                if (_searchPhrase != null && _searchPhrase.StartsWith("="))
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Space(2);
                    _selectedExpertSearchField = EditorGUILayout.Popup(_selectedExpertSearchField, _expertSearchFields, GUILayout.Width(90));
                    if (EditorGUI.EndChangeCheck())
                    {
                        string field = _expertSearchFields[_selectedExpertSearchField];
                        if (!string.IsNullOrEmpty(field) && !field.StartsWith("-"))
                        {
                            _searchPhrase += field.Replace('/', '.');
                        }
                        _selectedExpertSearchField = 0;
                    }
                }

                EditorGUI.BeginChangeCheck();
                GUILayout.Space(2);
                AssetInventory.Config.searchType = EditorGUILayout.Popup(AssetInventory.Config.searchType, _types, GUILayout.ExpandWidth(false), GUILayout.MinWidth(85));
                if (EditorGUI.EndChangeCheck()) dirty = true;
                GUILayout.Space(2);
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(_searchError))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(90);
                    EditorGUILayout.LabelField($"Error: {_searchError}", UIStyles.ColoredText(Color.red));
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                if (AssetInventory.Config.showFilterBar)
                {
                    GUILayout.BeginVertical("Filter Bar", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();
                    AssetInventory.Config.showDetailFilters = EditorGUILayout.Foldout(AssetInventory.Config.showDetailFilters, "Additional Filters");
                    if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                    if (AssetInventory.Config.showDetailFilters)
                    {
                        EditorGUI.BeginChangeCheck();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Package Tag", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedPackageTag = EditorGUILayout.Popup(_selectedPackageTag, _tagNames, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("File Tag", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedFileTag = EditorGUILayout.Popup(_selectedFileTag, _tagNames, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Package", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedAsset = EditorGUILayout.Popup(_selectedAsset, _assetNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(200));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Publisher", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedPublisher = EditorGUILayout.Popup(_selectedPublisher, _publisherNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Category", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedCategory = EditorGUILayout.Popup(_selectedCategory, _categoryNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Width", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxWidth ? "<=" : ">=", GUILayout.Width(25))) _checkMaxWidth = !_checkMaxWidth;
                        _searchWidth = EditorGUILayout.TextField(_searchWidth, GUILayout.Width(58));
                        EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Height", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxHeight ? "<=" : ">=", GUILayout.Width(25))) _checkMaxHeight = !_checkMaxHeight;
                        _searchHeight = EditorGUILayout.TextField(_searchHeight, GUILayout.Width(58));
                        EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Length", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxLength ? "<=" : ">=", GUILayout.Width(25))) _checkMaxLength = !_checkMaxLength;
                        _searchLength = EditorGUILayout.TextField(_searchLength, GUILayout.Width(58));
                        EditorGUILayout.LabelField("sec", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("File Size", "File size in kilobytes"), EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxSize ? "<=" : ">=", GUILayout.Width(25))) _checkMaxSize = !_checkMaxSize;
                        _searchSize = EditorGUILayout.TextField(_searchSize, GUILayout.Width(58));
                        EditorGUILayout.LabelField("kb", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        if (AssetInventory.Config.extractColors)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel, GUILayout.Width(85));
                            _selectedColorOption = EditorGUILayout.Popup(_selectedColorOption, _colorOptions, GUILayout.Width(87));
                            if (_selectedColorOption > 0) _selectedColor = EditorGUILayout.ColorField(_selectedColor);
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedPackageTypes = EditorGUILayout.Popup(_selectedPackageTypes, _packageListingOptions, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck()) dirty = true;

                        if (GUILayout.Button("Reset Filters"))
                        {
                            ResetSearch(true);
                            _requireSearchUpdate = true;
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();
                    AssetInventory.Config.showSavedSearches = EditorGUILayout.Foldout(AssetInventory.Config.showSavedSearches, "Saved Searches");
                    if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                    if (AssetInventory.Config.showSavedSearches)
                    {
                        if (AssetInventory.Config.searches.Count == 0)
                        {
                            EditorGUILayout.HelpBox("Save different search settings to quickly pull up the results later again.", MessageType.Info);
                        }
                        if (GUILayout.Button("Save current search..."))
                        {
                            NameUI nameUI = new NameUI();
                            nameUI.Init(string.IsNullOrEmpty(_searchPhrase) ? "My Search" : _searchPhrase, SaveSearch);
                            PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), nameUI);
                        }

                        EditorGUILayout.Space();
                        Color oldCol = GUI.backgroundColor;
                        for (int i = 0; i < AssetInventory.Config.searches.Count; i++)
                        {
                            SavedSearch search = AssetInventory.Config.searches[i];
                            GUILayout.BeginHorizontal();

                            if (ColorUtility.TryParseHtmlString($"#{search.color}", out Color color)) GUI.backgroundColor = color;
                            if (GUILayout.Button(UIStyles.Content(search.name, search.searchPhrase), GUILayout.MaxWidth(250))) LoadSearch(search);
                            GUI.backgroundColor = oldCol;

                            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash", "|Delete saved search"), GUILayout.Width(30)))
                            {
                                AssetInventory.Config.searches.RemoveAt(i);
                                AssetInventory.SaveConfig();
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    if (AssetInventory.DEBUG_MODE && GUILayout.Button("Reload Lookups")) ReloadLookups();

                    GUILayout.EndVertical();
                }

                // result
                if (_contents != null && _contents.Length > 0 && _files == null) PerformSearch(); // happens during recompilation
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                // assets
                GUILayout.BeginVertical();
                bool isAudio = AssetInventory.IsFileType(_selectedEntry?.Path, "Audio");
                if (_contents != null && _contents.Length > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    _searchScrollPos = GUILayout.BeginScrollView(_searchScrollPos, false, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // TODO: implement paged endless scrolling, needs some pixel calculations though
                        // if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
                        // _textureLoading = EditorCoroutineUtility.StartCoroutine(LoadTextures(false), this);
                    }

                    int cells = Mathf.RoundToInt(Mathf.Clamp(Mathf.Floor((position.width - UIStyles.INSPECTOR_WIDTH * (AssetInventory.Config.showFilterBar ? 2 : 1) - UIStyles.BORDER_WIDTH - 25) / AssetInventory.Config.tileSize), 1, 99));
                    if (cells < 2) cells = 2;

                    EditorGUI.BeginChangeCheck();
                    _gridSelection = GUILayout.SelectionGrid(_gridSelection, _contents, cells, UIStyles.tile);
                    if (Event.current.type == EventType.Repaint)
                    {
                        _mouseOverSearchResultRect = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
                    }
                    if (EditorGUI.EndChangeCheck() || (_allowLogic && _searchDone))
                    {
                        _searchDone = false;
                        if (AssetInventory.Config.autoHideSettings) _showSettings = false;
                        if (_gridSelection >= _files.Count) _gridSelection = 0;
                        _selectedEntry = _files[_gridSelection];
                        EditorAudioUtility.StopAllPreviewClips();
                        isAudio = AssetInventory.IsFileType(_selectedEntry?.Path, "Audio");
                        if (_selectedEntry != null)
                        {
                            _selectedEntry.CheckIfInProject();
                            _selectedEntry.IsMaterialized = AssetInventory.IsMaterialized(_selectedEntry.ToAsset(), _selectedEntry);
                            EditorCoroutineUtility.StartCoroutine(AssetUtils.LoadPackageTexture(_selectedEntry), this);

                            // if entry is already materialized calculate dependencies immediately
                            if (!_previewInProgress && _selectedEntry.DependencyState == AssetInfo.DependencyStateOptions.Unknown && _selectedEntry.IsMaterialized)
                            {
#pragma warning disable CS4014
                                // must run in same thread
                                CalculateDependencies(_selectedEntry);
#pragma warning restore CS4014
                            }

                            _packageAvailable = _selectedEntry.AssetSource == Asset.Source.Directory || _selectedEntry.AssetSource == Asset.Source.Package || File.Exists(_selectedEntry.GetLocation(true));
                            if (AssetInventory.Config.pingSelected && _selectedEntry.InProject) PingAsset(_selectedEntry);
                        }
                        else
                        {
                            _packageAvailable = false;
                        }

                        // Used event is thrown if user manually selected the entry
                        if (AssetInventory.Config.autoPlayAudio && isAudio && Event.current.type == EventType.Used) PlayAudio(_selectedEntry);
                    }

                    GUILayout.EndScrollView();

                    // navigation
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (_pageCount > 1)
                    {
                        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageUp) SetPage(1);
                        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageDown) SetPage(_pageCount);

                        EditorGUI.BeginDisabledGroup(_curPage <= 1);
                        if ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow) ||
                            GUILayout.Button("<", GUILayout.ExpandWidth(false))) SetPage(_curPage - 1);
                        EditorGUI.EndDisabledGroup();

                        if (EditorGUILayout.DropdownButton(UIStyles.Content($"Page {_curPage:N0}/{_pageCount:N0}", $"{_resultCount:N0} results in total"), FocusType.Keyboard, UIStyles.centerPopup, GUILayout.MinWidth(100)))
                        {
                            DropDownUI pageUI = new DropDownUI();
                            pageUI.Init(1, _pageCount, _curPage, "Page ", null, SetPage);
                            PopupWindow.Show(_pageButtonRect, pageUI);
                        }
                        if (Event.current.type == EventType.Repaint) _pageButtonRect = GUILayoutUtility.GetLastRect();

                        EditorGUI.BeginDisabledGroup(_curPage >= _pageCount);
                        if ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow) ||
                            GUILayout.Button(">", GUILayout.ExpandWidth(false))) SetPage(_curPage + 1);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{_resultCount:N0} results", UIStyles.centerLabel, GUILayout.ExpandWidth(true));
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Settings", "|Show/Hide Settings Tab"))) _showSettings = !_showSettings;
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                else
                {
                    _selectedEntry = null;
                    GUILayout.Label("No matching results", UIStyles.whiteCenter, GUILayout.MinHeight(AssetInventory.Config.tileSize));
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Settings", "|Show/Hide Settings Tab"))) _showSettings = !_showSettings;
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                GUILayout.EndVertical();

                // inspector
                GUILayout.BeginVertical();
                GUILayout.BeginVertical("Details Inspector", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                EditorGUILayout.Space();
                _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                if (_selectedEntry == null || string.IsNullOrEmpty(_selectedEntry.SafeName))
                {
                    // will happen after script reload
                    EditorGUILayout.HelpBox("Select an asset for details", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField("File", EditorStyles.largeLabel);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Name", $"Internal Id: {_selectedEntry.Id}"), EditorStyles.boldLabel, GUILayout.Width(85));
                    EditorGUILayout.LabelField(UIStyles.Content(Path.GetFileName(_selectedEntry.GetPath(true)), _selectedEntry.GetPath(true)));
                    GUILayout.EndHorizontal();
                    if (_selectedEntry.AssetSource == Asset.Source.Directory) GUILabelWithText("Location", $"{Path.GetDirectoryName(_selectedEntry.GetPath(true))}");
                    GUILabelWithText("Size", EditorUtility.FormatBytes(_selectedEntry.Size));
                    if (_selectedEntry.Width > 0) GUILabelWithText("Dimensions", $"{_selectedEntry.Width}x{_selectedEntry.Height} px");
                    if (_selectedEntry.Length > 0) GUILabelWithText("Length", $"{_selectedEntry.Length:0.##} seconds");
                    GUILabelWithText("In Project", _selectedEntry.InProject ? "Yes" : "No");
                    if (_packageAvailable)
                    {
                        if (AssetInventory.ScanDependencies.Contains(_selectedEntry.Type))
                        {
                            switch (_selectedEntry.DependencyState)
                            {
                                case AssetInfo.DependencyStateOptions.Unknown:
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel, GUILayout.Width(85));
                                    EditorGUI.BeginDisabledGroup(_previewInProgress);
                                    if (GUILayout.Button("Calculate", GUILayout.ExpandWidth(false)))
                                    {
#pragma warning disable CS4014
                                        // must run in same thread
                                        CalculateDependencies(_selectedEntry);
#pragma warning restore CS4014
                                    }
                                    EditorGUI.EndDisabledGroup();
                                    GUILayout.EndHorizontal();
                                    break;

                                case AssetInfo.DependencyStateOptions.Calculating:
                                    GUILabelWithText("Dependencies", "Calculating...");
                                    break;

                                case AssetInfo.DependencyStateOptions.NotPossible:
                                    GUILabelWithText("Dependencies", "Cannot determine (binary)");
                                    break;

                                case AssetInfo.DependencyStateOptions.Done:
                                    GUILayout.BeginHorizontal();
                                    string scriptDeps = _selectedEntry.ScriptDependencies?.Count > 0 ? $" + {_selectedEntry.ScriptDependencies?.Count}" : string.Empty;
                                    GUILabelWithText("Dependencies", $"{_selectedEntry.MediaDependencies?.Count}{scriptDeps} ({EditorUtility.FormatBytes(_selectedEntry.DependencySize)})");
                                    if (_selectedEntry.Dependencies.Count > 0 && GUILayout.Button("Show..."))
                                    {
                                        DependenciesUI depUI = DependenciesUI.ShowWindow();
                                        depUI.Init(_selectedEntry);
                                    }

                                    GUILayout.EndHorizontal();
                                    break;
                            }
                        }

                        if (!_selectedEntry.InProject && string.IsNullOrEmpty(_importFolder))
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Select a folder in Project View for import options", EditorStyles.centeredGreyMiniLabel);
                            EditorGUI.BeginDisabledGroup(true);
                            GUILayout.Button("Import File");
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUI.BeginDisabledGroup(_previewInProgress);
                        if ((!_selectedEntry.InProject || Event.current.control) && !string.IsNullOrEmpty(_importFolder))
                        {
                            string command = _selectedEntry.InProject ? "Reimport" : "Import";
                            GUILabelWithText($"{command} To", _importFolder);
                            EditorGUILayout.Space();
                            if (GUILayout.Button($"{command} File" + (_selectedEntry.DependencySize > 0 ? " Only" : ""))) CopyTo(_selectedEntry, _importFolder);
                            if (_selectedEntry.DependencySize > 0 && AssetInventory.ScanDependencies.Contains(_selectedEntry.Type))
                            {
                                if (GUILayout.Button($"{command} With Dependencies")) CopyTo(_selectedEntry, _importFolder, true);
                                if (_selectedEntry.ScriptDependencies.Count > 0)
                                {
                                    if (GUILayout.Button($"{command} With Dependencies + Scripts")) CopyTo(_selectedEntry, _importFolder, true, true);
                                }

                                EditorGUILayout.Space();
                            }
                        }

                        if (isAudio)
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(EditorGUIUtility.IconContent("d_PlayButton", "|Play"), GUILayout.Width(40))) PlayAudio(_selectedEntry);
                            if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreMatQuad", "|Stop"), GUILayout.Width(40))) EditorAudioUtility.StopAllPreviewClips();
                            EditorGUILayout.Space();
                            EditorGUI.BeginChangeCheck();
                            AssetInventory.Config.autoPlayAudio = GUILayout.Toggle(AssetInventory.Config.autoPlayAudio, "Play automatically");
                            if (EditorGUI.EndChangeCheck())
                            {
                                AssetInventory.SaveConfig();
                                if (AssetInventory.Config.autoPlayAudio) PlayAudio(_selectedEntry);
                            }
                            GUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                        }

                        if (_selectedEntry.InProject)
                        {
                            if (GUILayout.Button("Ping")) PingAsset(_selectedEntry);
                        }

                        if (GUILayout.Button("Open")) Open(_selectedEntry);
                        if (GUILayout.Button(Application.platform == RuntimePlatform.OSXEditor ? "Open in Finder" : "Open in Explorer")) OpenExplorer(_selectedEntry);
                        EditorGUI.BeginDisabledGroup(_previewInProgress);
                        if (Event.current.control && GUILayout.Button("Recreate Preview")) RecreatePreview(_selectedEntry);
                        EditorGUI.EndDisabledGroup();
                        if (Event.current.control) EditorGUILayout.Space();
                        if (Event.current.control && GUILayout.Button(UIStyles.Content("Delete from Index", "Will delete the indexed file from the database. The package will need to be reindexed in order for it to appear again."))) DeleteFromIndex(_selectedEntry);

                        if (!_selectedEntry.IsMaterialized && !_previewInProgress)
                        {
                            EditorGUILayout.LabelField("Asset will be extracted before actions are performed", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    else if (_selectedEntry.IsLocationUnmappedRelative())
                    {
                        EditorGUILayout.HelpBox("The location of this package is stored relative and no mapping has been done yet for this system in the settings: " + _selectedEntry.Location, MessageType.Info);
                    }

                    if (_previewInProgress)
                    {
                        EditorGUILayout.LabelField("Extracting...", EditorStyles.centeredGreyMiniLabel);
                    }

                    // tags
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(UIStyles.Content("Add Tag..."), GUILayout.Width(70)))
                    {
                        TagSelectionUI tagUI = new TagSelectionUI();
                        tagUI.Init(TagAssignment.Target.Asset);
                        tagUI.SetAsset(new List<AssetInfo> {_selectedEntry});
                        PopupWindow.Show(_tag2ButtonRect, tagUI);
                    }
                    if (Event.current.type == EventType.Repaint) _tag2ButtonRect = GUILayoutUtility.GetLastRect();
                    GUILayout.Space(15);

                    if (_selectedEntry.AssetTags != null && _selectedEntry.AssetTags.Count > 0)
                    {
                        float x = 0f;
                        foreach (TagInfo tagInfo in _selectedEntry.AssetTags)
                        {
                            x = CalcTagSize(x, tagInfo.Name);
                            UIStyles.DrawTag(tagInfo, () =>
                            {
                                AssetInventory.RemoveTagAssignment(_selectedEntry, tagInfo);
                                _requireAssetTreeRebuild = true;
                                _requireSearchUpdate = true;
                            });
                        }
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                    UIStyles.DrawUILine(Color.gray * 0.6f);
                    EditorGUILayout.Space();

                    DrawPackageDetails(_selectedEntry, false, true, false);
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                if (_showSettings)
                {
                    EditorGUILayout.Space();
                    GUILayout.BeginVertical("View Settings", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();

                    int width = 125;

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Search In", "Field to use for finding assets when doing plain searches and no expert search."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.searchField = EditorGUILayout.Popup(AssetInventory.Config.searchField, _searchFields);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Sort by", "Specify sort order"), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.sortField = EditorGUILayout.Popup(AssetInventory.Config.sortField, _sortFields);
                    if (GUILayout.Button(AssetInventory.Config.sortDescending ? UIStyles.Content("˅", "Descending") : UIStyles.Content("˄", "Ascending"), GUILayout.Width(15)))
                    {
                        AssetInventory.Config.sortDescending = !AssetInventory.Config.sortDescending;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Results", $"Maximum number of results to show. A (configurable) hard limit of {AssetInventory.Config.maxResultsLimit} will be enforced to keep Unity responsive."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.maxResults = EditorGUILayout.Popup(AssetInventory.Config.maxResults, _resultSizes);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Hide Extensions", "File extensions to hide from search results, e.g. asset;json;txt. These will still be indexed."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.excludedExtensions = EditorGUILayout.DelayedTextField(AssetInventory.Config.excludedExtensions);
                    GUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                        _curPage = 1;
                        AssetInventory.SaveConfig();
                    }

                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Tile Size", "Dimensions of search result previews. Preview images will still be 128x128 max."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.tileSize = EditorGUILayout.IntSlider(AssetInventory.Config.tileSize, 50, 300);
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        _lastTileSizeChange = DateTime.Now;
                        AssetInventory.SaveConfig();
                    }

                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Tile Text", "Text to be shown on the tile"), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.tileText = EditorGUILayout.Popup(AssetInventory.Config.tileText, _tileTitle);
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                        AssetInventory.SaveConfig();
                    }

                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Search While Typing", "Will search immediately while typing and update results constantly."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.searchAutomatically = EditorGUILayout.Toggle(AssetInventory.Config.searchAutomatically);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Auto-Play Audio", "Will automatically extract unity packages to play the sound file if they were not extracted yet. This is the most convenient option but will require sufficient hard disk space."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.autoPlayAudio = EditorGUILayout.Toggle(AssetInventory.Config.autoPlayAudio);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Ping Selected", "Highlight selected items in the Unity project tree if they are found in the current project."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.pingSelected = EditorGUILayout.Toggle(AssetInventory.Config.pingSelected);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Double-Click Import", "Highlight selected items in the Unity project tree if they are found in the current project."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.doubleClickImport = EditorGUILayout.Toggle(AssetInventory.Config.doubleClickImport);
                    GUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Group List", "Add a second level hierarchy to dropdowns if they become too long to scroll."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.groupLists = EditorGUILayout.Toggle(AssetInventory.Config.groupLists);
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        AssetInventory.SaveConfig();
                        ReloadLookups();
                    }

                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Previews", "Optionally restricts search results to those with either preview images available or not."), EditorStyles.boldLabel, GUILayout.Width(width));
                    AssetInventory.Config.previewVisibility = EditorGUILayout.Popup(AssetInventory.Config.previewVisibility, _previewOptions);
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                        AssetInventory.SaveConfig();
                    }

                    EditorGUILayout.Space();
                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                if (dirty)
                {
                    _requireSearchUpdate = true;
                    _keepSearchResultPage = false;
                }
                EditorGUIUtility.labelWidth = 0;
            }
        }

        private void DeleteFromIndex(AssetInfo info)
        {
            AssetInventory.ForgetAssetFile(info);
            _requireSearchUpdate = true;
        }

        private void DrawPackageDownload(AssetInfo info, bool updateMode = false)
        {
            if (!string.IsNullOrEmpty(info.OriginalLocation))
            {
                if (!updateMode) EditorGUILayout.HelpBox("Not cached currently. Download the asset to access its content.", MessageType.Warning);

                if (info.PackageDownloader == null) info.PackageDownloader = new AssetDownloader(info);
                AssetDownloadState state = info.PackageDownloader.GetState();
                switch (state.state)
                {
                    case AssetDownloader.State.Downloading:
                        UIStyles.DrawProgressBar(state.progress, $"{EditorUtility.FormatBytes(state.bytesDownloaded)}");
                        break;

                    case AssetDownloader.State.Unavailable:
                        if (info.PackageDownloader.IsDownloadSupported() && GUILayout.Button("Download")) info.PackageDownloader.Download();
                        break;

                    case AssetDownloader.State.UpdateAvailable:
                        if (info.PackageDownloader.IsDownloadSupported() && GUILayout.Button("Download Update"))
                        {
                            info.WasOutdated = true;
                            info.PackageDownloader.Download();
                        }
                        break;

                    case AssetDownloader.State.Downloaded:
                        if (info.WasOutdated)
                        {
                            // update early in assumption it worked, reindexing will correct it if necessary
                            info.Version = info.LatestVersion;
                            DBAdapter.DB.Execute("update Asset set CurrentSubState=0, Version=? where Id=?", info.LatestVersion, info.AssetId);

                            // if (!info.Exclude) AssetInventory.RefreshIndex(info.Id, true);
                        }
                        info.PackageDownloader = null;
                        info.Refresh();

                        _packageAvailable = true;
                        _requireAssetTreeRebuild = true;
                        break;
                }
            }
            else
            {
                if (!updateMode)
                {
                    if (info.IsLocationUnmappedRelative())
                    {
                        EditorGUILayout.HelpBox("The location of this package is stored relative and no mapping has been done yet in the settings for this system.", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("This package is new and metadata has not been collected yet. Update the index to have all metadata up to date.", MessageType.Warning);
                    }
                }
            }
        }

        private async void RecreatePreview(AssetInfo info)
        {
            _previewInProgress = true;
            if (await PreviewImporter.RecreatePreview(info)) _requireSearchUpdate = true;
            _previewInProgress = false;
        }

        private async void RecreatePreviews(AssetInfo info, bool missingOnly, bool retryErroneous)
        {
            AssetInventory.IndexingInProgress = true;
            AssetProgress.CancellationRequested = false;

            PreviewImporter.ScheduleRecreatePreviews(info?.ToAsset(), missingOnly, retryErroneous);
            int created = await new PreviewImporter().RecreatePreviews(info?.ToAsset());
            Debug.Log($"Preview recreation done. {created} created.");

            _requireSearchUpdate = true;
            AssetInventory.IndexingInProgress = false;
        }

        private void LoadSearch(SavedSearch search)
        {
            _searchPhrase = search.searchPhrase;
            _selectedPackageTypes = search.packageTypes;
            _selectedColorOption = search.colorOption;
            _selectedColor = ImageUtils.FromHex(search.searchColor);
            _searchWidth = search.width;
            _searchHeight = search.height;
            _searchLength = search.length;
            _searchSize = search.size;
            _checkMaxWidth = search.checkMaxWidth;
            _checkMaxHeight = search.checkMaxHeight;
            _checkMaxLength = search.checkMaxLength;
            _checkMaxSize = search.checkMaxSize;

            AssetInventory.Config.searchType = Mathf.Max(0, Array.FindIndex(_types, s => s == search.type || s.EndsWith($"/{search.type}")));
            _selectedPublisher = Mathf.Max(0, Array.FindIndex(_publisherNames, s => s == search.publisher || s.EndsWith($"/{search.publisher}")));
            _selectedAsset = Mathf.Max(0, Array.FindIndex(_assetNames, s => s == search.package || s.EndsWith($"/{search.package}")));
            _selectedCategory = Mathf.Max(0, Array.FindIndex(_categoryNames, s => s == search.category || s.EndsWith($"/{search.category}")));
            _selectedPackageTag = Mathf.Max(0, Array.FindIndex(_tagNames, s => s == search.packageTag || s.EndsWith($"/{search.packageTag}")));
            _selectedFileTag = Mathf.Max(0, Array.FindIndex(_tagNames, s => s == search.fileTag || s.EndsWith($"/{search.fileTag}")));

            _requireSearchUpdate = true;
        }

        private void SaveSearch(string value)
        {
            SavedSearch spec = new SavedSearch();
            spec.name = value;
            spec.searchPhrase = _searchPhrase;
            spec.packageTypes = _selectedPackageTypes;
            spec.colorOption = _selectedColorOption;
            spec.searchColor = "#" + ColorUtility.ToHtmlStringRGB(_selectedColor);
            spec.width = _searchWidth;
            spec.height = _searchHeight;
            spec.length = _searchLength;
            spec.size = _searchSize;
            spec.checkMaxWidth = _checkMaxWidth;
            spec.checkMaxHeight = _checkMaxHeight;
            spec.checkMaxLength = _checkMaxLength;
            spec.checkMaxSize = _checkMaxSize;
            spec.color = ColorUtility.ToHtmlStringRGB(Random.ColorHSV());

            if (AssetInventory.Config.searchType > 0 && _types.Length > AssetInventory.Config.searchType)
            {
                spec.type = _types[AssetInventory.Config.searchType].Split('/').LastOrDefault();
            }

            if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
            {
                spec.publisher = _publisherNames[_selectedPublisher].Split('/').LastOrDefault();
            }

            if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
            {
                spec.package = _assetNames[_selectedAsset].Split('/').LastOrDefault();
            }

            if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
            {
                spec.category = _categoryNames[_selectedCategory].Split('/').LastOrDefault();
            }

            if (_selectedPackageTag > 0 && _tagNames.Length > _selectedPackageTag)
            {
                spec.packageTag = _tagNames[_selectedPackageTag].Split('/').LastOrDefault();
            }

            if (_selectedFileTag > 0 && _tagNames.Length > _selectedFileTag)
            {
                spec.fileTag = _tagNames[_selectedFileTag].Split('/').LastOrDefault();
            }

            AssetInventory.Config.searches.Add(spec);
            AssetInventory.SaveConfig();
        }

        private void DrawPackageDetails(AssetInfo info, bool showMaintenance = false, bool showActions = true, bool startNewSection = true)
        {
            if (info.Id == 0) return;

            if (startNewSection)
            {
                GUILayout.BeginVertical("Package Details", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.LabelField("Package", EditorStyles.largeLabel);
            }
            GUILabelWithText("Name", info.GetDisplayName(false), 85, info.Location);
            if (info.AssetSource == Asset.Source.Package)
            {
                GUILabelWithText("Id", $"{info.SafeName}");

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Preferred", "Version defined by you to use for imports and indexing activities."), EditorStyles.boldLabel, GUILayout.Width(85));
                if (EditorGUILayout.DropdownButton(UIStyles.Content(info.GetVersionToUse(), "Version to use as your default"), FocusType.Keyboard, GUILayout.ExpandWidth(false)))
                {
                    VersionSelectionUI versionUI = new VersionSelectionUI();
                    versionUI.Init(info, newVersion =>
                    {
                        info.PreferredVersion = newVersion;
                        AssetInventory.SetAssetVersionPreference(info, newVersion);
                        _requireAssetTreeRebuild = true;
                    });
                    PopupWindow.Show(_versionButtonRect, versionUI);
                }
                if (Event.current.type == EventType.Repaint) _versionButtonRect = GUILayoutUtility.GetLastRect();
                GUILayout.EndHorizontal();

                if (!string.IsNullOrWhiteSpace(info.SupportedUnityVersions)) GUILabelWithText("Minimal Unity", $"{info.SupportedUnityVersions}");
            }
            if (!string.IsNullOrWhiteSpace(info.License)) GUILabelWithText("Publisher", $"{info.License}");
            if (!string.IsNullOrWhiteSpace(info.GetDisplayPublisher())) GUILabelWithText("Publisher", $"{info.GetDisplayPublisher()}");
            if (!string.IsNullOrWhiteSpace(info.GetDisplayCategory())) GUILabelWithText("Category", $"{info.GetDisplayCategory()}");
            if (info.PackageSize > 0) GUILabelWithText("Size", EditorUtility.FormatBytes(info.PackageSize));
            if (!string.IsNullOrWhiteSpace(info.AssetRating))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Rating", "Rating given by Asset Store users"), EditorStyles.boldLabel, GUILayout.Width(85));
                if (int.TryParse(info.AssetRating, out int rating))
                {
                    if (rating <= 0)
                    {
                        EditorGUILayout.LabelField("Not enough ratings", GUILayout.MaxWidth(108));
                    }
                    else
                    {
                        Color oldCC = GUI.contentColor;
#if UNITY_2021_1_OR_NEWER
                        // favicon is not gold anymore                    
                        GUI.contentColor = new Color(0.992f, 0.694f, 0.004f);
#endif
                        for (int i = 0; i < rating; i++)
                        {
                            GUILayout.Button(EditorGUIUtility.IconContent("Favorite Icon"), EditorStyles.label, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        }
                        GUI.contentColor = oldCC;
                        for (int i = rating; i < 5; i++)
                        {
                            GUILayout.Button(EditorGUIUtility.IconContent("Favorite"), EditorStyles.label, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"{info.AssetRating} ");
                }
                EditorGUILayout.LabelField($"({info.RatingCount} ratings)", GUILayout.MaxWidth(80));
                GUILayout.EndHorizontal();
            }
            if (info.AssetSource == Asset.Source.AssetStorePackage && info.LastRelease.Year > 1)
            {
                GUILabelWithText("Last Update", info.LastRelease.ToString("ddd, MMM d yyyy") + (!string.IsNullOrEmpty(info.LatestVersion) ? $" ({info.LatestVersion})" : string.Empty));
            }

            string packageTooltip = $"Internal Id: {info.AssetId}\nForeign Id: {info.ForeignId}\nCurrent State: {info.CurrentState}\nLocation: {info.GetLocation(true)}";
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Source", packageTooltip), EditorStyles.boldLabel, GUILayout.Width(85));
            switch (info.AssetSource)
            {
                case Asset.Source.AssetStorePackage:
                    if (info.ForeignId > 0)
                    {
                        if (GUILayout.Button(UIStyles.Content("Asset Store"), EditorStyles.linkLabel)) Application.OpenURL(info.GetItemLink());
                    }
                    else
                    {
                        EditorGUILayout.LabelField(UIStyles.Content("Asset Store", packageTooltip), UIStyles.GetLabelMaxWidth());
                    }
                    break;

                case Asset.Source.Package:
                    EditorGUILayout.LabelField(UIStyles.Content($"{info.AssetSource} ({info.PackageSource})", info.SafeName), UIStyles.GetLabelMaxWidth());
                    break;

                default:
                    EditorGUILayout.LabelField(UIStyles.Content(info.AssetSource.ToString(), packageTooltip), UIStyles.GetLabelMaxWidth());
                    break;
            }
            GUILayout.EndHorizontal();
            if (info.AssetSource != Asset.Source.AssetStorePackage && info.ForeignId > 0)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Asset Link", EditorStyles.boldLabel, GUILayout.Width(85));
                if (GUILayout.Button(UIStyles.Content("Asset Store"), EditorStyles.linkLabel)) Application.OpenURL(info.GetItemLink());
                GUILayout.EndHorizontal();
            }

            if (showMaintenance)
            {
                if (AssetInventory.Config.createBackups)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Backup", "Activate to create backups for this asset (done after every update cycle)."), EditorStyles.boldLabel, GUILayout.Width(85));
                    EditorGUI.BeginChangeCheck();
                    info.Backup = EditorGUILayout.Toggle(info.Backup);
                    if (EditorGUI.EndChangeCheck()) AssetInventory.SetAssetBackup(info, info.Backup);
                    GUILayout.EndHorizontal();
                }

                if (Event.current.control)
                {
                    if (info.AssetSource == Asset.Source.CustomPackage || info.AssetSource == Asset.Source.Archive || info.AssetSource == Asset.Source.AssetStorePackage)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Extract", "Will keep the package extracted in the cache to minimize access delays at the cost of more hard disk space."), EditorStyles.boldLabel, GUILayout.Width(85));
                        EditorGUI.BeginChangeCheck();
                        info.KeepExtracted = EditorGUILayout.Toggle(info.KeepExtracted);
                        if (EditorGUI.EndChangeCheck()) AssetInventory.SetAssetExtraction(info, info.KeepExtracted);
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Exclude", "Will not index the asset and not show existing index results in the search."), EditorStyles.boldLabel, GUILayout.Width(85));
                    EditorGUI.BeginChangeCheck();
                    info.Exclude = EditorGUILayout.Toggle(info.Exclude);
                    if (EditorGUI.EndChangeCheck())
                    {
                        AssetInventory.SetAssetExclusion(info, info.Exclude);
                        _requireLookupUpdate = true;
                        _requireSearchUpdate = true;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            if (info.IsDeprecated) EditorGUILayout.HelpBox("This asset is deprecated.", MessageType.Warning);
            if (info.IsAbandoned) EditorGUILayout.HelpBox("This asset is no longer available.", MessageType.Error);

            if (showActions)
            {
                if (info.CurrentSubState == Asset.SubState.Outdated) EditorGUILayout.HelpBox("This asset is outdated in the cache. It is recommended to delete it from the database and the file system.", MessageType.Info);
                if (info.AssetSource == Asset.Source.AssetStorePackage || info.AssetSource == Asset.Source.CustomPackage || info.AssetSource == Asset.Source.Package || info.AssetSource == Asset.Source.Archive)
                {
                    EditorGUILayout.Space();
                    if (info.Downloaded)
                    {
                        if (info.AssetSource != Asset.Source.Archive)
                        {
                            if (showMaintenance && (info.IsUpdateAvailable(_assets) || info.WasOutdated))
                            {
                                DrawPackageDownload(info, true);
                            }
                            if (AssetStore.IsInstalled(info))
                            {
                                if (GUILayout.Button("Remove Package")) RemovePackage(info);
                            }
                            else
                            {
                                if (GUILayout.Button(UIStyles.Content("Import Package...", "Show import dialog.")))
                                {
                                    ImportUI importUI = ImportUI.ShowWindow();
                                    importUI.OnImportDone += () =>
                                    {
                                        _requireLookupUpdate = true;
                                        _requireAssetTreeRebuild = true;
                                    };
                                    importUI.Init(new List<AssetInfo> {info});
                                }
                                if (Event.current.control && GUILayout.Button(UIStyles.Content("Open Package Location..."))) EditorUtility.RevealInFinder(info.GetLocation(true));
                            }
                        }
                        else
                        {
                            if (Event.current.control && GUILayout.Button(UIStyles.Content("Open Archive Location..."))) EditorUtility.RevealInFinder(info.GetLocation(true));
                        }
                    }
                    if (_tab == 0 && _selectedAsset == 0 && GUILayout.Button("Filter for this Package only")) OpenInSearch(info, true);
                    if (_tab == 0 && Event.current.control && GUILayout.Button("Show in Package View")) OpenInPackageView(info);
                    if (showMaintenance && info.IsIndexed)
                    {
                        if (GUILayout.Button("Open in Search")) OpenInSearch(info);
                        if (Event.current.control) EditorGUILayout.Space();
                        if (GUILayout.Button(UIStyles.Content("Reindex Package on Next Run")))
                        {
                            AssetInventory.ForgetAsset(info, true);
                            _requireLookupUpdate = true;
                            _requireSearchUpdate = true;
                            _requireAssetTreeRebuild = true;
                        }
                        if (Event.current.control && GUILayout.Button(UIStyles.Content("Reindex Package Now")))
                        {
                            AssetInventory.ForgetAsset(info, true);
                            AssetInventory.RefreshIndex(info.Id);
                            _requireLookupUpdate = true;
                            _requireSearchUpdate = true;
                            _requireAssetTreeRebuild = true;
                        }
                    }
                    if (info.Downloaded)
                    {
                        if ((_tab == 1 || Event.current.control) && GUILayout.Button("Recreate Missing Previews")) RecreatePreviews(info, true, false);
                        if (Event.current.control && GUILayout.Button("Recreate All Previews")) RecreatePreviews(info, false, true);
                    }
                    else if (!info.IsAbandoned && info.AssetSource != Asset.Source.Archive)
                    {
                        EditorGUILayout.Space();
                        DrawPackageDownload(info);
                    }
                    if (showMaintenance && info.AssetSource == Asset.Source.CustomPackage)
                    {
                        if (info.ForeignId <= 0)
                        {
                            if (GUILayout.Button("Connect to Asset Store..."))
                            {
                                AssetConnectionUI assetUI = new AssetConnectionUI();
                                assetUI.Init(details => ConnectToAssetStore(info, details));
                                PopupWindow.Show(_connectButtonRect, assetUI);
                            }
                            if (Event.current.type == EventType.Repaint) _connectButtonRect = GUILayoutUtility.GetLastRect();
                        }
                        else
                        {
                            if (GUILayout.Button("Remove Asset Store Connection"))
                            {
                                bool removeMetadata = EditorUtility.DisplayDialog("Remove Metadata", "Remove or keep the additional metadata from the Asset Store like ratings, category etc.?", "Remove", "Keep");
                                AssetInventory.DisconnectFromAssetStore(info, removeMetadata);
                                _requireAssetTreeRebuild = true;
                            }
                            if (Event.current.type == EventType.Repaint) _connectButtonRect = GUILayoutUtility.GetLastRect();
                        }
                    }
                }
                if (Event.current.control && (info.ForeignId > 0 || info.AssetSource == Asset.Source.Package) && GUILayout.Button(UIStyles.Content("Show in Package Manager...")))
                {
                    AssetStore.OpenInPackageManager(info);
                }
                if (showMaintenance)
                {
                    if (Event.current.control) EditorGUILayout.Space();
                    if (Event.current.control && info.Downloaded && GUILayout.Button(UIStyles.Content("Delete Package...", "Delete the package from the database and optionally the filesystem.")))
                    {
                        bool removeFiles = info.Downloaded && EditorUtility.DisplayDialog("Delete Package", "Do you also want to remove the file from the Unity cache? If not the package will reappear after the next index update.", "Remove from Cache", "Keep Files");
                        AssetInventory.RemoveAsset(info, removeFiles);
                        _requireLookupUpdate = true;
                        _requireAssetTreeRebuild = true;
                    }
                    if (Event.current.control && !info.Downloaded && GUILayout.Button(UIStyles.Content("Delete Package", "Delete the package from the database.")))
                    {
                        AssetInventory.RemoveAsset(info, false);
                        _requireLookupUpdate = true;
                        _requireAssetTreeRebuild = true;
                    }
                    if (Event.current.control && info.Downloaded && GUILayout.Button(UIStyles.Content("Delete Package from File System", "Delete the package only from the cache in the file system and leave index intact.")))
                    {
                        if (File.Exists(info.GetLocation(true)))
                        {
                            File.Delete(info.GetLocation(true));
                            info.Location = null;
                            info.PackageSize = 0;
                            info.CurrentState = Asset.State.New;
                            info.Refresh();
                            DBAdapter.DB.Execute("update Asset set Location=null, PackageSize=0, CurrentState=? where Id=?", info.AssetId, Asset.State.New);
                        }
                    }
                    _requireSearchUpdate = true;
                }

                DrawAddTag(new List<AssetInfo> {info});

                if (info.PackageTags != null && info.PackageTags.Count > 0)
                {
                    float x = 0f;
                    foreach (TagInfo tagInfo in info.PackageTags)
                    {
                        x = CalcTagSize(x, tagInfo.Name);
                        UIStyles.DrawTag(tagInfo, () =>
                        {
                            AssetInventory.RemoveTagAssignment(info, tagInfo);
                            _requireAssetTreeRebuild = true;
                        });
                    }
                }
                GUILayout.EndHorizontal();
            }
            if (info.PreviewTexture != null)
            {
                EditorGUILayout.Space();
                GUILayout.FlexibleSpace();
                GUILayout.Box(info.PreviewTexture, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(UIStyles.INSPECTOR_WIDTH), GUILayout.MaxHeight(100));
                GUILayout.FlexibleSpace();
            }
            if (startNewSection) GUILayout.EndVertical();
        }

        private void RemovePackage(AssetInfo info)
        {
            Client.Remove(info.SafeName);
        }

        private async void ConnectToAssetStore(AssetInfo info, AssetDetails details)
        {
            AssetInventory.ConnectToAssetStore(info, details);
            await AssetInventory.FetchAssetsDetails();
            _requireLookupUpdate = true;
            _requireAssetTreeRebuild = true;
        }

        private static float CalcTagSize(float x, string name)
        {
            x += UIStyles.tag.CalcSize(UIStyles.Content(name)).x + UIStyles.TAG_SIZE_SPACING + EditorGUIUtility.singleLineHeight + UIStyles.tag.margin.right * 2f;
            if (x > UIStyles.INSPECTOR_WIDTH - UIStyles.TAG_OUTER_MARGIN * 3)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(85 + 3);
                x = UIStyles.tag.CalcSize(UIStyles.Content(name)).x + UIStyles.TAG_SIZE_SPACING + EditorGUIUtility.singleLineHeight + UIStyles.tag.margin.right * 2f;
            }
            return x;
        }

        private void DrawAddTag(List<AssetInfo> info)
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(UIStyles.Content("Add Tag..."), GUILayout.Width(70)))
            {
                TagSelectionUI tagUI = new TagSelectionUI();
                tagUI.Init(TagAssignment.Target.Package);
                tagUI.SetAsset(info);
                PopupWindow.Show(_tagButtonRect, tagUI);
            }
            if (Event.current.type == EventType.Repaint) _tagButtonRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(15);
        }

        private void DrawSettingsTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            _folderScrollPos = GUILayout.BeginScrollView(_folderScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));

            // folders
            EditorGUI.BeginChangeCheck();
            AssetInventory.Config.showIndexingSettings = EditorGUILayout.Foldout(AssetInventory.Config.showIndexingSettings, "Index Locations");
            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            if (AssetInventory.Config.showIndexingSettings)
            {
                EditorGUILayout.LabelField("Unity Asset Store downloads will be indexed automatically. Specify custom locations below to scan for unitypackages downloaded from somewhere else than the Asset Store or for any arbitrary media files like your model or sound library you want to access.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                AssetInventory.Config.indexAssetStore = GUILayout.Toggle(AssetInventory.Config.indexAssetStore, "Asset Store Downloads");
                AssetInventory.Config.indexPackageCache = GUILayout.Toggle(AssetInventory.Config.indexPackageCache, "Package Cache");
                if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();
#if UNITY_2022_1_OR_NEWER
                EditorGUILayout.LabelField("Only the default asset cache location will be scanned. If you moved it to a different location add that location as an additional folder below.", EditorStyles.miniLabel);
#endif
                EditorGUILayout.Space();
                if (SerializedFoldersObject != null)
                {
                    SerializedFoldersObject.Update();
                    FolderListControl.DoLayoutList();
                    SerializedFoldersObject.ApplyModifiedProperties();
                }
            }

            int labelWidth = 190;

            // relative locations
            if (AssetInventory.RelativeLocations.Count > 0)
            {
                EditorGUILayout.LabelField("Relative Location Mappings", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("Location", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();
                foreach (RelativeLocation location in AssetInventory.RelativeLocations)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(location.Key, GUILayout.Width(200));

                    string otherSystems = "Mappings on other systems:\n\n";
                    string otherLocs = string.Join("\n", location.otherLocations);
                    otherSystems += string.IsNullOrWhiteSpace(otherLocs) ? "-None-" : otherLocs;

                    if (string.IsNullOrWhiteSpace(location.Location))
                    {
                        EditorGUILayout.LabelField(UIStyles.Content("-Not yet connected-", otherSystems));

                        // TODO: add ability to force delete relative mapping in case it is not used in additional folders anymore
                    }
                    else
                    {
                        EditorGUILayout.LabelField(UIStyles.Content(location.Location, otherSystems));
                        if (string.IsNullOrWhiteSpace(otherLocs))
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash", "|Cannot delete only remaining mapping"), GUILayout.Width(30));
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {
                            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash", "|Delete mapping"), GUILayout.Width(30)))
                            {
                                DBAdapter.DB.Delete(location);
                                AssetInventory.LoadRelativeLocations();
                            }
                        }
                    }
                    if (GUILayout.Button(UIStyles.Content("...", "Select folder"), GUILayout.Width(30)))
                    {
                        SelectRelativeFolderMapping(location);
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.Space(20);
            }

            // importing
            EditorGUI.BeginChangeCheck();
            AssetInventory.Config.showImportSettings = EditorGUILayout.Foldout(AssetInventory.Config.showImportSettings, "Import");
            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            if (AssetInventory.Config.showImportSettings)
            {
                EditorGUILayout.LabelField("You can always drag & drop assets form the search into a folder of your choice in the project view. What can be configured is the behavior when using the Import button or double-clicking an asset.", EditorStyles.wordWrappedLabel);

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Structure", "Structure to materialize the imported files in"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                AssetInventory.Config.importStructure = EditorGUILayout.Popup(AssetInventory.Config.importStructure, _importStructureOptions, GUILayout.Width(300));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Destination", "Target folder for imported files"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                AssetInventory.Config.importDestination = EditorGUILayout.Popup(AssetInventory.Config.importDestination, _importDestinationOptions, GUILayout.Width(300));
                GUILayout.EndHorizontal();

                if (AssetInventory.Config.importDestination == 2)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Target Folder", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(string.IsNullOrWhiteSpace(AssetInventory.Config.importFolder) ? "[Assets Root]" : AssetInventory.Config.importFolder, GUILayout.ExpandWidth(true));
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button("Select...", GUILayout.ExpandWidth(false))) SelectImportFolder();
                    if (!string.IsNullOrWhiteSpace(AssetInventory.Config.importFolder) && GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                    {
                        AssetInventory.Config.importFolder = null;
                        AssetInventory.SaveConfig();
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();
            }

            // preview images
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            AssetInventory.Config.showPreviewSettings = EditorGUILayout.Foldout(AssetInventory.Config.showPreviewSettings, "Preview Images");
            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            if (AssetInventory.Config.showPreviewSettings)
            {
                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Extract Preview Images", "Keep a folder with preview images for each asset file. Will require a moderate amount of space if there are many files."), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                AssetInventory.Config.extractPreviews = EditorGUILayout.Toggle(AssetInventory.Config.extractPreviews);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Use Fallback-Icons as Previews", "Will show generic icons in case a file preview is missing instead of an empty tile."), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                AssetInventory.Config.showIconsForMissingPreviews = EditorGUILayout.Toggle(AssetInventory.Config.showIconsForMissingPreviews);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Upscale Preview Images", "Resize preview images to make them fill a bigger area of the tiles."), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                AssetInventory.Config.upscalePreviews = EditorGUILayout.Toggle(AssetInventory.Config.upscalePreviews);
                GUILayout.EndHorizontal();

                if (AssetInventory.Config.upscalePreviews)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Minimum Size", "Minimum size the preview image should have. Bigger images are not changed."), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    AssetInventory.Config.upscaleSize = EditorGUILayout.DelayedIntField(AssetInventory.Config.upscaleSize, GUILayout.Width(50));
                    EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    AssetInventory.SaveConfig();
                    _requireSearchUpdate = true;
                }
                EditorGUILayout.Space();
            }

            // backup
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            AssetInventory.Config.showBackupSettings = EditorGUILayout.Foldout(AssetInventory.Config.showBackupSettings, "Auto-Backup");
            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            if (AssetInventory.Config.showBackupSettings)
            {
                EditorGUILayout.LabelField("Asset Inventory can automatically create backups of your asset purchases. Unity does not store old versions and assets get regularly deprecated. Backups will allow you to go back to previous versions easily. Backups will be done at the end of each update cycle.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Create Backups", "Store downloaded assets in a separate folder"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                AssetInventory.Config.createBackups = EditorGUILayout.Toggle(AssetInventory.Config.createBackups);
                GUILayout.EndHorizontal();

                if (AssetInventory.Config.createBackups)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Active for New Packages", "Will mark newly encountered packages to be backed up automatically. Otherwise you need to select packages manually which will save a lot of disk space potentially."), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    AssetInventory.Config.backupByDefault = EditorGUILayout.Toggle(AssetInventory.Config.backupByDefault);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Override Patch Versions", "Will remove all but the latest patch version of an asset inside the same minor version (e.g. 5.4.3 instead of 5.4.2)"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    AssetInventory.Config.onlyLatestPatchVersion = EditorGUILayout.Toggle(AssetInventory.Config.onlyLatestPatchVersion);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Backups per Asset", "Number of versions to keep per asset"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    AssetInventory.Config.backupsPerAsset = EditorGUILayout.IntSlider(AssetInventory.Config.backupsPerAsset, 1, 10, GUILayout.Width(150));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Storage Folder", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(string.IsNullOrWhiteSpace(AssetInventory.Config.backupFolder) ? "[Default] " + AssetInventory.GetBackupFolder(false) : AssetInventory.Config.backupFolder, GUILayout.ExpandWidth(true));
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button("Select...", GUILayout.ExpandWidth(false))) SelectBackupFolder();
                    if (!string.IsNullOrWhiteSpace(AssetInventory.Config.backupFolder) && GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                    {
                        AssetInventory.Config.backupFolder = null;
                        AssetInventory.SaveConfig();
                    }
                    GUILayout.EndHorizontal();
                }
                if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.BeginVertical("Update", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH), GUILayout.ExpandHeight(false));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ensure to regularly update the index and to fetch the newest updates from the Asset Store.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            bool easyMode = AssetInventory.Config.allowEasyMode && !Event.current.control;
            if (_usageCalculationInProgress)
            {
                EditorGUILayout.LabelField("Other activity in progress...", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(AssetProgress.CurrentMain);
            }
            else
            {
                if (easyMode)
                {
                    if (AssetInventory.IndexingInProgress || AssetInventory.CurrentMain != null)
                    {
                        EditorGUI.BeginDisabledGroup(AssetProgress.CancellationRequested);
                        if (GUILayout.Button("Stop Indexing"))
                        {
                            AssetProgress.CancellationRequested = true;
                            AssetStore.CancellationRequested = true;
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        if (GUILayout.Button(UIStyles.Content("Update Index", "Update everything in one go and perform all necessary actions."), GUILayout.Height(40))) PerformFullUpdate();
                    }
                }
                else
                {
                    // local
                    if (AssetInventory.IndexingInProgress)
                    {
                        EditorGUI.BeginDisabledGroup(AssetProgress.CancellationRequested);
                        if (GUILayout.Button("Stop Indexing")) AssetProgress.CancellationRequested = true;
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        if (GUILayout.Button(UIStyles.Content("Update Local Index", "Update all local folders and scan for cache and file changes."))) AssetInventory.RefreshIndex(0);
                        if (Event.current.control && GUILayout.Button(UIStyles.Content("Force Update Local Index", "Will parse all package metadata again (not the contents if unchanged) and update the index."))) AssetInventory.RefreshIndex(0, true);
                    }
                }
            }

            // status
            if (AssetInventory.IndexingInProgress)
            {
                EditorGUILayout.Space();
                if (AssetProgress.MainCount > 0)
                {
                    EditorGUILayout.LabelField("Package Progress", EditorStyles.boldLabel);
                    UIStyles.DrawProgressBar(AssetProgress.MainProgress / (float)AssetProgress.MainCount, $"{AssetProgress.MainProgress}/{AssetProgress.MainCount}");
                    EditorGUILayout.LabelField("Package", EditorStyles.boldLabel);

                    string package = !string.IsNullOrEmpty(AssetProgress.CurrentMain) ? Path.GetFileName(AssetProgress.CurrentMain) : "scanning...";
                    EditorGUILayout.LabelField(UIStyles.Content(package, package));
                }

                if (AssetProgress.SubCount > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("File Progress", EditorStyles.boldLabel);
                    UIStyles.DrawProgressBar(AssetProgress.SubProgress / (float)AssetProgress.SubCount, $"{AssetProgress.SubProgress}/{AssetProgress.SubCount} - " + Path.GetFileName(AssetProgress.CurrentSub));
                }
            }

            if (!easyMode)
            {
                // asset store
                EditorGUILayout.Space();
                EditorGUI.BeginDisabledGroup(AssetInventory.CurrentMain != null);
                if (GUILayout.Button(UIStyles.Content("Update Asset Store Data", "Refresh purchases and metadata from Unity Asset Store."))) FetchAssetPurchases(false);
                if (Event.current.control && GUILayout.Button(UIStyles.Content("Force Update Asset Store Data", "Force updating all assets instead of only changed ones."))) FetchAssetPurchases(true);
                EditorGUI.EndDisabledGroup();
                if (AssetInventory.CurrentMain != null)
                {
                    if (GUILayout.Button("Cancel")) AssetStore.CancellationRequested = true;
                }
            }

            if (AssetInventory.CurrentMain != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"{AssetInventory.CurrentMain} {AssetInventory.MainProgress}/{AssetInventory.MainCount}", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.BeginVertical("Preferences", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
            EditorGUILayout.Space();
            _settingsScrollPos = GUILayout.BeginScrollView(_settingsScrollPos, false, false);
            EditorGUI.BeginChangeCheck();

            int width = 205;

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Download Assets for Indexing", "Automatically download uncached items from the Asset Store for indexing. Will delete them again afterwards."), EditorStyles.boldLabel, GUILayout.Width(width));
            AssetInventory.Config.downloadAssets = EditorGUILayout.Toggle(AssetInventory.Config.downloadAssets);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Extract Color Information", "Determines the hue of an image which will enable search by color. Increases indexing time. Can be turned on & off as needed."), EditorStyles.boldLabel, GUILayout.Width(width));
            AssetInventory.Config.extractColors = EditorGUILayout.Toggle(AssetInventory.Config.extractColors);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Extract Full Metadata", "Will extract dimensions from images and length from audio files to make these searchable at the cost of a slower indexing process."), EditorStyles.boldLabel, GUILayout.Width(width));
            AssetInventory.Config.gatherExtendedMetadata = EditorGUILayout.Toggle(AssetInventory.Config.gatherExtendedMetadata);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Index Asset Package Contents", "Will extract asset packages (.unitypackage) and make contents searchable. This is the foundation for the search. Deactivate only if you are solely interested in package metadata."), EditorStyles.boldLabel, GUILayout.Width(width));
            AssetInventory.Config.indexAssetPackageContents = EditorGUILayout.Toggle(AssetInventory.Config.indexAssetPackageContents);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Index Registry Package Contents", "Will index packages (from a registry) and make contents searchable. Can result in a lot of indexed files, depending on how many versions of a package there are. "), EditorStyles.boldLabel, GUILayout.Width(width));
            AssetInventory.Config.indexPackageContents = EditorGUILayout.Toggle(AssetInventory.Config.indexPackageContents);
            GUILayout.EndHorizontal();

            if (Event.current.control)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Exclude New Packages By Default", "Will not cause automatic indexing of newly downloaded assets. Instead this needs to be triggered manually per package."), EditorStyles.boldLabel, GUILayout.Width(width));
                AssetInventory.Config.excludeByDefault = EditorGUILayout.Toggle(AssetInventory.Config.excludeByDefault);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Pause indexing regularly", "Will pause all hard disk activity regularly to allow the disk to cool down."), EditorStyles.boldLabel, GUILayout.Width(width));
            AssetInventory.Config.useCooldown = EditorGUILayout.Toggle(AssetInventory.Config.useCooldown);
            GUILayout.EndHorizontal();

            if (AssetInventory.Config.useCooldown)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("Pause every", GUILayout.ExpandWidth(false));
                AssetInventory.Config.cooldownInterval = EditorGUILayout.DelayedIntField(AssetInventory.Config.cooldownInterval, GUILayout.Width(30));
                GUILayout.Label("minutes for", GUILayout.ExpandWidth(false));
                AssetInventory.Config.cooldownDuration = EditorGUILayout.DelayedIntField(AssetInventory.Config.cooldownDuration, GUILayout.Width(30));
                GUILayout.Label("seconds", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            if (EditorGUI.EndChangeCheck())
            {
                AssetInventory.SaveConfig();
                _requireLookupUpdate = true;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.BeginVertical("Statistics", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
            EditorGUILayout.Space();
            _statsScrollPos = GUILayout.BeginScrollView(_statsScrollPos, false, false);
            GUILabelWithText("Indexed Packages", $"{_indexedPackageCount:N0}/{_packageCount:N0}", 120);
            GUILabelWithText("Indexed Files", $"{_packageFileCount:N0}", 120);
            GUILabelWithText("Database Size", EditorUtility.FormatBytes(_dbSize), 120);

            if (_indexedPackageCount < _packageCount - _deprecatedAssetsCount - _registryPackageCount && !AssetInventory.IndexingInProgress)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("To index the remaining assets, download them first. Tip: You can multi-select packages here to start a bulk download.", MessageType.Info);
            }

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            _showDiskSpace = EditorGUILayout.Foldout(_showDiskSpace, "Used Disk Space");
            EditorGUI.BeginDisabledGroup(_calculatingFolderSizes);
            if (GUILayout.Button(_calculatingFolderSizes ? "Calculating..." : "Refresh", GUILayout.ExpandWidth(false)))
            {
                _showDiskSpace = true;
                CalcFolderSizes();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            if (_showDiskSpace)
            {
                if (_lastFolderSizeCalculation != DateTime.MinValue)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Previews", "Size of folder containing asset preview images."), EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField(EditorUtility.FormatBytes(_previewSize), GUILayout.Width(80));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Cache", "Size of folder containing temporary cache. Can be deleted at any time."), EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField(EditorUtility.FormatBytes(_cacheSize), GUILayout.Width(80));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Persistent Cache", "Size of extracted packages in cache that are marked 'extracted' and not automatically removed."), EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField(EditorUtility.FormatBytes(_persistedCacheSize), GUILayout.Width(80));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Backups", "Size of folder containing asset preview images."), EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField(EditorUtility.FormatBytes(_backupSize), GUILayout.Width(80));
                    GUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("last updated " + _lastFolderSizeCalculation.ToShortTimeString(), EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Not calculated yet....", EditorStyles.centeredGreyMiniLabel);
                }
            }

            EditorGUILayout.Space();
            _showMaintenance = EditorGUILayout.Foldout(_showMaintenance, "Maintenance Functions");
            if (_showMaintenance)
            {
                EditorGUI.BeginDisabledGroup(AssetInventory.CurrentMain != null || AssetInventory.IndexingInProgress);
                if (GUILayout.Button("Recreate Missing Previews")) RecreatePreviews(null, true, false);

                EditorGUI.BeginDisabledGroup(_cleanupInProgress);
                if (Event.current.control && GUILayout.Button("Remove Orphaned Preview Images")) RemoveOrphans();
                if (Event.current.control && GUILayout.Button("Remove Duplicate Media Indexes")) RemoveDuplicateMediaIndexes();

                if (GUILayout.Button("Optimize Database"))
                {
                    long savings = DBAdapter.Compact();
                    UpdateStatistics();
                    EditorUtility.DisplayDialog("Success", $"Database was compacted. Size reduction: {EditorUtility.FormatBytes(savings)}", "OK");
                }
                EditorGUI.EndDisabledGroup();

                if (Event.current.control) EditorGUILayout.Space();
                EditorGUI.BeginDisabledGroup(AssetInventory.ClearCacheInProgress);
                if (GUILayout.Button("Clear Cache")) AssetInventory.ClearCache(UpdateStatistics);
                EditorGUI.EndDisabledGroup();
                if (Event.current.control && GUILayout.Button("Clear Database"))
                {
                    if (DBAdapter.DeleteDB())
                    {
                        AssetUtils.ClearCache();
                        if (Directory.Exists(AssetInventory.GetPreviewFolder())) Directory.Delete(AssetInventory.GetPreviewFolder(), true);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Database seems to be in use by another program and could not be cleared.", "OK");
                    }
                    UpdateStatistics();
                    _assets = new List<AssetInfo>();
                }
                if (Event.current.control && GUILayout.Button("Reset Configuration")) AssetInventory.ResetConfig();
                if (Event.current.control) EditorGUILayout.Space();

                if (DBAdapter.IsDBOpen())
                {
                    if (Event.current.control && GUILayout.Button("Close Database (to allow copying)")) DBAdapter.Close();
                }

                EditorGUI.BeginDisabledGroup(AssetInventory.CurrentMain != null || AssetInventory.IndexingInProgress);
                if (GUILayout.Button("Change Database Location...")) SetDatabaseLocation();
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Database & Cache Location", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(AssetInventory.GetStorageFolder(), EditorStyles.wordWrappedLabel);

                EditorGUILayout.LabelField(UIStyles.Content("Config Location", "Copy the file into your project to use a project-specific configuration instead."), EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(AssetInventory.UsedConfigLocation, EditorStyles.wordWrappedLabel);

                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void SelectRelativeFolderMapping(RelativeLocation location)
        {
            string folder = EditorUtility.OpenFolderPanel("Select folder to map to", location.Location, "");
            if (!string.IsNullOrEmpty(folder))
            {
                location.Location = Path.GetFullPath(folder);
                if (location.Id > 0)
                {
                    DBAdapter.DB.Execute("UPDATE RelativeLocation SET Location = ? WHERE Id = ?", location.Location, location.Id);
                }
                else
                {
                    DBAdapter.DB.Insert(location);
                }
                AssetInventory.LoadRelativeLocations();
            }
        }

        private async void RemoveOrphans()
        {
            _cleanupInProgress = true;
            await AssetInventory.RemoveOrphans();
            _cleanupInProgress = false;
        }

        private async void RemoveDuplicateMediaIndexes()
        {
            _cleanupInProgress = true;

            await AssetInventory.RemoveDuplicateMediaIndexes();
            _requireLookupUpdate = true;
            _requireSearchUpdate = true;
            _requireAssetTreeRebuild = true;

            _cleanupInProgress = false;
        }

        private void SelectBackupFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select storage folder for backups", AssetInventory.Config.backupFolder, "");
            if (!string.IsNullOrEmpty(folder))
            {
                AssetInventory.Config.backupFolder = Path.GetFullPath(folder);
                AssetInventory.SaveConfig();
            }
        }

        private void SelectImportFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select folder for imports", AssetInventory.Config.importFolder, "");
            if (!string.IsNullOrEmpty(folder))
            {
                if (!folder.ToLowerInvariant().StartsWith(Application.dataPath.ToLowerInvariant()))
                {
                    EditorUtility.DisplayDialog("Error", "Folder must be inside current project", "OK");
                    return;
                }

                // store only part relative to /Assets
                AssetInventory.Config.importFolder = folder.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1);
                AssetInventory.SaveConfig();
            }
        }

        private async void CalcFolderSizes()
        {
            if (_calculatingFolderSizes) return;
            _calculatingFolderSizes = true;
            _lastFolderSizeCalculation = DateTime.Now;

            _backupSize = await AssetInventory.GetBackupFolderSize();
            _cacheSize = await AssetInventory.GetCacheFolderSize();
            _persistedCacheSize = await AssetInventory.GetPersistedCacheSize();
            _previewSize = await AssetInventory.GetPreviewFolderSize();

            _calculatingFolderSizes = false;
        }

        private void DrawPackagesTab()
        {
            // asset list
            if (_packageCount == 0)
            {
                EditorGUILayout.HelpBox("No packages were indexed yet. Start the indexing process to fill this list.", MessageType.Info);
                GUILayout.BeginHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(EditorGUIUtility.IconContent("Preset.Context", "|Search Filters")))
                {
                    AssetInventory.Config.showPackageFilterBar = !AssetInventory.Config.showPackageFilterBar;
                    AssetInventory.SaveConfig();
                }
                EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
                EditorGUI.BeginChangeCheck();
                _assetSearchPhrase = AssetSearchField.OnGUI(_assetSearchPhrase, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck()) AssetTreeView.searchString = _assetSearchPhrase;

                if (AssetInventory.Config.assetGrouping == 0)
                {
                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();
                    EditorGUIUtility.labelWidth = 50;
                    AssetInventory.Config.assetSorting = EditorGUILayout.Popup(UIStyles.Content("Sort by:", "Specify how packages should be sorted"), AssetInventory.Config.assetSorting, _assetSortOptions, GUILayout.Width(160));
                    if (GUILayout.Button(AssetInventory.Config.sortAssetsDescending ? UIStyles.Content("˅", "Descending") : UIStyles.Content("˄", "Ascending"), GUILayout.Width(15)))
                    {
                        AssetInventory.Config.sortAssetsDescending = !AssetInventory.Config.sortAssetsDescending;
                    }
                }

                EditorGUILayout.Space();
                EditorGUIUtility.labelWidth = 60;
                AssetInventory.Config.assetGrouping = EditorGUILayout.Popup(UIStyles.Content("Group by:", "Select if packages should be grouped or not"), AssetInventory.Config.assetGrouping, _groupByOptions, GUILayout.Width(140));
                if (EditorGUI.EndChangeCheck())
                {
                    CreateAssetTree();
                    AssetInventory.SaveConfig();
                }
                EditorGUIUtility.labelWidth = 0;

                if (AssetInventory.Config.assetGrouping > 0 && GUILayout.Button("Collapse All", GUILayout.ExpandWidth(false)))
                {
                    AssetTreeView.CollapseAll();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                if (AssetInventory.Config.showPackageFilterBar)
                {
                    GUILayout.BeginVertical("Filter Bar", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel, GUILayout.Width(85));
                    AssetInventory.Config.packagesListing = EditorGUILayout.Popup(AssetInventory.Config.packagesListing, _packageListingOptions, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Deprecation", EditorStyles.boldLabel, GUILayout.Width(85));
                    AssetInventory.Config.assetDeprecation = EditorGUILayout.Popup(AssetInventory.Config.assetDeprecation, _deprecationOptions, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Maintenance", "A collection of various special-purpose filters"), EditorStyles.boldLabel, GUILayout.Width(85));
                    _selectedMaintenance = EditorGUILayout.Popup(_selectedMaintenance, _maintenanceOptions, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                    {
                        AssetInventory.SaveConfig();
                        _requireAssetTreeRebuild = true;
                    }

                    GUILayout.EndVertical();
                }

                // packages
                GUILayout.BeginVertical();
                int left = AssetInventory.Config.showPackageFilterBar ? UIStyles.INSPECTOR_WIDTH + 5 : 0;
                int yStart = string.IsNullOrEmpty(CloudProjectSettings.accessToken) ? 128 : 80;
                AssetTreeView.OnGUI(new Rect(left, yStart, position.width - UIStyles.INSPECTOR_WIDTH - left - 5, position.height - yStart));
                GUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            // FIXME: scrolling is broken for some reason, bar will often overlap
            _assetsScrollPos = GUILayout.BeginScrollView(_assetsScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            GUILayout.BeginVertical("Overview", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH), GUILayout.ExpandHeight(false));
            EditorGUILayout.Space();
            int labelWidth = 120;
            GUILabelWithText("Indexed Packages", $"{_indexedPackageCount:N0}/{_assets.Count:N0}", labelWidth);
            if (_purchasedAssetsCount > 0) GUILabelWithText("From Asset Store", $"{_purchasedAssetsCount:N0}", labelWidth);
            if (_registryPackageCount > 0) GUILabelWithText("From Registries", $"{_registryPackageCount:N0}", labelWidth);
            if (_customPackageCount > 0) GUILabelWithText("From Other Sources", $"{_customPackageCount:N0}", labelWidth);
            if (_deprecatedAssetsCount > 0) GUILabelWithText("Deprecated", $"{_deprecatedAssetsCount:N0}", labelWidth);
            if (_excludedAssetsCount > 0) GUILabelWithText("Excluded", $"{_excludedAssetsCount:N0}", labelWidth);
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Package Manager")) UnityEditor.PackageManager.UI.Window.Open("");
            GUILayout.EndVertical();

            if (_selectedTreeAsset != null)
            {
                EditorGUILayout.Space();
                DrawPackageDetails(_selectedTreeAsset, true);
            }

            if (_selectedTreeAsset == null && _selectedTreeAssets != null && _selectedTreeAssets.Count > 0)
            {
                DrawBulkPackageActions(_selectedTreeAssets, _assetBulkTags, _assetTreeSelectionSize, true);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawBulkPackageActions(List<AssetInfo> bulkAssets, Dictionary<string, Tuple<int, Color>> bulkTags, long size, bool useScroll)
        {
            int labelWidth = 85;

            EditorGUILayout.Space();
            GUILayout.BeginVertical("Bulk Actions", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
            if (useScroll) _bulkScrollPos = GUILayout.BeginScrollView(_bulkScrollPos, false, false);
            GUILabelWithText("Selected", $"{bulkAssets.Count:N0}", labelWidth);
            GUILabelWithText("Size", EditorUtility.FormatBytes(size), labelWidth);

            if (AssetInventory.Config.createBackups)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Backup", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                if (GUILayout.Button("All", GUILayout.ExpandWidth(false)))
                {
                    bulkAssets.ForEach(info => AssetInventory.SetAssetBackup(info, true));
                }
                if (GUILayout.Button("None", GUILayout.ExpandWidth(false)))
                {
                    bulkAssets.ForEach(info => AssetInventory.SetAssetBackup(info, false));
                }
                GUILayout.EndHorizontal();
            }

            if (Event.current.control)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Extract", "Will keep the package extracted in the cache to minimize access delays at the cost of more hard disk space."), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                if (GUILayout.Button("All", GUILayout.ExpandWidth(false)))
                {
                    bulkAssets.ForEach(info => AssetInventory.SetAssetExtraction(info, true));
                }
                if (GUILayout.Button("None", GUILayout.ExpandWidth(false)))
                {
                    bulkAssets.ForEach(info => AssetInventory.SetAssetExtraction(info, false));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Exclude", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                if (GUILayout.Button("All", GUILayout.ExpandWidth(false)))
                {
                    bulkAssets.ForEach(info => AssetInventory.SetAssetExclusion(info, true));
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                }
                if (GUILayout.Button("None", GUILayout.ExpandWidth(false)))
                {
                    bulkAssets.ForEach(info => AssetInventory.SetAssetExclusion(info, false));
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                }
                GUILayout.EndHorizontal();
            }

            // determine download status, a bit expensive but happens only in bulk selections
            int notDownloaded = 0;
            int updateAvailable = 0;
            int downloading = 0;
            long remainingBytes = 0;
            foreach (AssetInfo info in bulkAssets.Where(a => a.WasOutdated || !a.Downloaded || a.IsUpdateAvailable(_assets)))
            {
                if (info.PackageDownloader == null) info.PackageDownloader = new AssetDownloader(info);
                AssetDownloadState state = info.PackageDownloader.GetState();
                switch (state.state)
                {
                    case AssetDownloader.State.Unavailable:
                        notDownloaded++;
                        break;

                    case AssetDownloader.State.Downloading:
                        downloading++;
                        remainingBytes += state.bytesTotal - state.bytesDownloaded;
                        break;

                    case AssetDownloader.State.UpdateAvailable:
                        updateAvailable++;
                        break;

                    case AssetDownloader.State.Downloaded:
                        if (info.WasOutdated)
                        {
                            // update early in assumption it worked, reindexing will correct it if necessary
                            info.Version = info.LatestVersion;
                            DBAdapter.DB.Execute("update Asset set CurrentSubState=0, Version=? where Id=?", info.LatestVersion, info.AssetId);
                        }

                        info.PackageDownloader = null;
                        info.Refresh();

                        _requireAssetTreeRebuild = true;
                        break;
                }
            }

            if (notDownloaded > 0)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Not Cached", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                if (GUILayout.Button($"Download remaining {notDownloaded}", GUILayout.ExpandWidth(false)))
                {
                    foreach (AssetInfo info in bulkAssets.Where(a => !a.Downloaded))
                    {
                        info.PackageDownloader.Download();
                    }
                }
                GUILayout.EndHorizontal();
            }
            if (updateAvailable > 0)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Updates", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                if (GUILayout.Button($"Update remaining {updateAvailable}", GUILayout.ExpandWidth(false)))
                {
                    foreach (AssetInfo info in bulkAssets.Where(a => a.IsUpdateAvailable(_assets)))
                    {
                        info.WasOutdated = true;
                        info.PackageDownloader.Download();
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (downloading > 0)
            {
                GUILabelWithText("Downloading", $"{downloading}", labelWidth);
                GUILabelWithText("Remaining", $"{EditorUtility.FormatBytes(remainingBytes)}", labelWidth);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Import..."))
            {
                ImportUI importUI = ImportUI.ShowWindow();
                importUI.OnImportDone += () =>
                {
                    _requireLookupUpdate = true;
                    _requireAssetTreeRebuild = true;
                };
                importUI.Init(bulkAssets);
            }
            if (Event.current.control && GUILayout.Button(UIStyles.Content("Open Package Locations...")))
            {
                bulkAssets.ForEach(info => { EditorUtility.RevealInFinder(info.GetLocation(true)); });
            }

            if (GUILayout.Button("Reindex Packages on Next Run"))
            {
                bulkAssets.ForEach(info => AssetInventory.ForgetAsset(info, true));
                _requireLookupUpdate = true;
                _requireSearchUpdate = true;
                _requireAssetTreeRebuild = true;
            }
            if (GUILayout.Button(UIStyles.Content("Delete Packages...", "Delete the packages from the database and optionally the filesystem.")))
            {
                bool removeFiles = bulkAssets.Any(a => a.Downloaded) && EditorUtility.DisplayDialog("Delete Packages", "Do you also want to remove the files from the Unity cache? If not the packages will reappear after the next index update.", "Remove from Cache", "Keep Files");
                bulkAssets.ForEach(info => AssetInventory.RemoveAsset(info, removeFiles));
                _requireLookupUpdate = true;
                _requireAssetTreeRebuild = true;
                _requireSearchUpdate = true;
            }
            if (Event.current.control && GUILayout.Button(UIStyles.Content("Delete Packages from File System", "Delete the packages directly from the cache in the file system.")))
            {
                bulkAssets.ForEach(info =>
                {
                    if (File.Exists(info.GetLocation(true)))
                    {
                        File.Delete(info.GetLocation(true));
                        info.Refresh();
                    }
                });
                _requireSearchUpdate = true;
            }

            DrawAddTag(bulkAssets);

            float x = 0f;
            foreach (KeyValuePair<string, Tuple<int, Color>> bulkTag in bulkTags)
            {
                string tagName = $"{bulkTag.Key} ({bulkTag.Value.Item1})";
                x = CalcTagSize(x, tagName);
                UIStyles.DrawTag(tagName, bulkTag.Value.Item2, () =>
                {
                    AssetInventory.RemoveTagAssignment(bulkAssets, bulkTag.Key);
                    _requireAssetTreeRebuild = true;
                }, UIStyles.TagStyle.Remove);
            }
            GUILayout.EndHorizontal();
            if (useScroll) GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawReportingTab()
        {
            int assetUsageCount = _assetUsage?.Count ?? 0;
            int identifiedFilesCount = _identifiedFiles?.Count ?? 0;
            int identifiedAssetsCount = _usedAssets?.Count ?? 0;
            int width = 120;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            EditorGUILayout.HelpBox("This view tries to identify used packages inside the current project. It will use guids. If package authors have shared files between projects this can result in multiple hits.", MessageType.Info);
            EditorGUILayout.Space();

            GUILabelWithText("Project files", $"{assetUsageCount:N0}", width);
            if (assetUsageCount > 0)
            {
                GUILabelWithText("Identified packages", $"{identifiedAssetsCount:N0}", width);
                GUILabelWithText("Identified files", $"{identifiedFilesCount:N0}" + " (" + Mathf.RoundToInt((float)identifiedFilesCount / assetUsageCount * 100f) + "%)", width);
            }
            else
            {
                GUILabelWithText("Identified packages", "None", width);
                GUILabelWithText("Identified files", "None", width);
            }

            if (_usedAssets != null && _usedAssets.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Identified Packages", EditorStyles.largeLabel);

                GUILayout.BeginVertical();
                int left = 0;
                int yStart = 160;
                ReportTreeView.OnGUI(new Rect(left, yStart, position.width - UIStyles.INSPECTOR_WIDTH - left - 5, position.height - yStart));
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.BeginVertical("Actions", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
            EditorGUILayout.Space();

            if (_usageCalculationInProgress)
            {
                EditorGUI.BeginDisabledGroup(AssetProgress.CancellationRequested);
                if (GUILayout.Button("Stop Identification")) AssetProgress.CancellationRequested = true;
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.LabelField("Identification Progress", EditorStyles.boldLabel);
                UIStyles.DrawProgressBar(AssetProgress.MainProgress / (float)AssetProgress.MainCount, $"{AssetProgress.MainProgress}/{AssetProgress.MainCount}");
                EditorGUILayout.LabelField(AssetProgress.CurrentMain);
                EditorGUILayout.Space();
            }
            else
            {
                if (GUILayout.Button("Identify Used Packages", GUILayout.Height(50))) CalculateAssetUsage();
            }
            if (GUILayout.Button("Export Data..."))
            {
                ExportUI exportUI = ExportUI.ShowWindow();
                exportUI.Init(_assets);
            }
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            EditorGUILayout.Space();

            _reportScrollPos = GUILayout.BeginScrollView(_reportScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            if (_selectedReportEntry != null)
            {
                DrawPackageDetails(_selectedReportEntry, true);
                EditorGUILayout.Space();
            }
            if (_selectedReportEntry == null && _selectedReportEntries != null && _selectedReportEntries.Count > 0)
            {
                DrawBulkPackageActions(_selectedReportEntries, _reportBulkTags, _reportTreeSelectionSize, false);
                EditorGUILayout.Space();
            }

            GUILayout.BeginVertical("Project View Selection", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
            EditorGUILayout.Space();

            if (_pvSelection != null && _pvSelection.Length > 0)
            {
                if (_pvSelection.Length > 1)
                {
                    EditorGUILayout.HelpBox("Multiple files are selected. This is not supported.", MessageType.Warning);
                }
            }
            if (string.IsNullOrEmpty(_pvSelectedPath))
            {
                EditorGUILayout.HelpBox("Select any file in the Unity Project View to identify what package it belongs to.", MessageType.Info);
            }
            else
            {
                GUILabelWithText("Folder", Path.GetDirectoryName(_pvSelectedPath));
                GUILabelWithText("Selection", Path.GetFileName(_pvSelectedPath));

                if (_pvSelectionChanged || _pvSelectedAssets == null)
                {
                    _pvSelectedAssets = AssetUtils.Guid2File(Selection.assetGUIDs[0]);
                    EditorCoroutineUtility.StartCoroutine(AssetUtils.LoadTextures(_pvSelectedAssets), this);
                }
                if (_pvSelectedAssets.Count == 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Could not identify package. Guid not found in local database.", MessageType.Info);
                }
                if (_pvSelectedAssets.Count > 1)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("The file was matched with multiple packages. This can happen if identical files were contained in multiple packages.", MessageType.Info);
                }
                foreach (AssetInfo info in _pvSelectedAssets)
                {
                    EditorGUILayout.Space();
                    DrawPackageDetails(info, false, true, false);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawAboutTab()
        {
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("A tool by Impossible Robert", UIStyles.whiteCenter);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Developer: Robert Wetzold", UIStyles.whiteCenter);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("www.wetzold.com/tools", UIStyles.centerLinkLabel)) Application.OpenURL("https://www.wetzold.com/tools");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"Version {AssetInventory.TOOL_VERSION}", UIStyles.whiteCenter);
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("If you like this asset please consider leaving a review on the Unity Asset Store. Thanks a million!", MessageType.Info);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Leave Review")) Application.OpenURL(AssetInventory.ASSET_STORE_LINK);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (Event.current.control && GUILayout.Button("Create Debug Support Report")) CreateDebugReport();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (AssetInventory.DEBUG_MODE && GUILayout.Button("Get Token", GUILayout.ExpandWidth(false))) Debug.Log(CloudProjectSettings.accessToken);
            if (AssetInventory.DEBUG_MODE && GUILayout.Button("Reload Lookups")) ReloadLookups();
        }

        private async void CalculateAssetUsage()
        {
            AssetProgress.CancellationRequested = false;
            _usageCalculationInProgress = true;

            _assetUsage = await new AssetUsage().Calculate();
            _usedAssets = _assetUsage.Select(info => info.GetDisplayName(false)).Distinct().Where(a => !string.IsNullOrEmpty(a)).ToList();
            _identifiedFiles = _assetUsage.Where(info => info.CurrentState != Asset.State.Unknown).ToList();

            // add installed packages
            PackageCollection packageCollection = AssetStore.GetProjectPackages();
            if (packageCollection != null)
            {
                int unmatchedCount = 0;
                foreach (PackageInfo packageInfo in packageCollection)
                {
                    if (packageInfo.source == PackageSource.BuiltIn) continue;

                    AssetInfo matchedAsset = _assets.FirstOrDefault(info => info.SafeName == packageInfo.name);
                    if (matchedAsset == null)
                    {
                        Debug.Log($"Registry package '{packageInfo.name}' is not yet indexed, information will be incomplete.");
                        matchedAsset = new AssetInfo();
                        matchedAsset.AssetSource = Asset.Source.Package;
                        matchedAsset.SafeName = packageInfo.name;
                        matchedAsset.DisplayName = packageInfo.displayName;
                        matchedAsset.Version = packageInfo.version;
                        matchedAsset.Id = int.MaxValue - unmatchedCount;
                        matchedAsset.AssetId = int.MaxValue - unmatchedCount;
                        unmatchedCount++;
                    }
                    _assetUsage.Add(matchedAsset);

                    string packageName = packageInfo.displayName + " - " + packageInfo.version;
                    if (!_usedAssets.Contains(packageName)) _usedAssets.Add(packageName);
                }
            }
            _usedAssets.Sort();
            _requireReportTreeRebuild = true;
            _usageCalculationInProgress = false;
        }

        private void PerformFullUpdate()
        {
            AssetInventory.RefreshIndex();

            // start also asset download if not already done before manually
            if (string.IsNullOrEmpty(AssetInventory.CurrentMain)) FetchAssetPurchases(false);
        }

        private void GUILabelWithText(string label, string text, int width = 85, string tooltip = null)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content(label, string.IsNullOrWhiteSpace(tooltip) ? label : tooltip), EditorStyles.boldLabel, GUILayout.Width(width));
            EditorGUILayout.LabelField(UIStyles.Content(text, text), GUILayout.MaxWidth(UIStyles.INSPECTOR_WIDTH - width - 20), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        private void SetDatabaseLocation()
        {
            string targetFolder = EditorUtility.OpenFolderPanel("Select folder for database and cache", AssetInventory.GetStorageFolder(), "");
            if (string.IsNullOrEmpty(targetFolder)) return;

            // check if same folder selected
            if (IOUtils.IsSameDirectory(targetFolder, AssetInventory.GetStorageFolder())) return;

            // check for existing database
            if (File.Exists(Path.Combine(targetFolder, DBAdapter.DB_NAME)))
            {
                if (EditorUtility.DisplayDialog("Use Existing?", "The target folder contains a database. Switch to this one? Otherwise please select an empty directory.", "Switch", "Cancel"))
                {
                    AssetInventory.SwitchDatabase(targetFolder);
                    ReloadLookups();
                    PerformSearch();
                }

                return;
            }

            // target must be empty
            if (!IOUtils.IsDirectoryEmpty(targetFolder))
            {
                EditorUtility.DisplayDialog("Error", "The target folder needs to be empty or contain an existing database.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Keep Old Database", "Should a new database be created or the current one moved?", "New", "Move"))
            {
                AssetInventory.SwitchDatabase(targetFolder);
                ReloadLookups();
                PerformSearch();
                return;
            }

            _previewInProgress = true;
            AssetInventory.MoveDatabase(targetFolder);
            _previewInProgress = false;
        }

        private async void PlayAudio(AssetInfo assetInfo)
        {
            // play instantly if no extraction is required
            if (_previewInProgress)
            {
                if (AssetInventory.IsMaterialized(assetInfo.ToAsset(), assetInfo)) await AssetInventory.PlayAudio(assetInfo);
                return;
            }

            _previewInProgress = true;

            await AssetInventory.PlayAudio(assetInfo);

            _previewInProgress = false;
        }

        private async void PingAsset(AssetInfo assetInfo)
        {
            // requires pauses in-between to allow editor to catch up
            EditorApplication.ExecuteMenuItem("Window/General/Project");
            await Task.Yield();

            Selection.activeObject = null;
            await Task.Yield();

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetInfo.ProjectPath);
            if (Selection.activeObject == null) assetInfo.ProjectPath = null; // probably got deleted again
        }

        private async Task CalculateDependencies(AssetInfo assetInfo)
        {
            _previewInProgress = true;
            assetInfo.DependencyState = AssetInfo.DependencyStateOptions.Calculating;
            await AssetInventory.CalculateDependencies(_selectedEntry);
            if (assetInfo.DependencyState == AssetInfo.DependencyStateOptions.Calculating) assetInfo.DependencyState = AssetInfo.DependencyStateOptions.Done; // otherwise error along the way
            _previewInProgress = false;
        }

        private async void Open(AssetInfo assetInfo)
        {
            _previewInProgress = true;
            string targetPath;
            if (assetInfo.InProject)
            {
                targetPath = assetInfo.ProjectPath;
            }
            else
            {
                targetPath = await AssetInventory.EnsureMaterializedAsset(assetInfo);
            }

            if (targetPath != null) EditorUtility.OpenWithDefaultApp(targetPath);
            _previewInProgress = false;
        }

        private async void OpenExplorer(AssetInfo assetInfo)
        {
            _previewInProgress = true;
            string targetPath;
            if (assetInfo.InProject)
            {
                targetPath = assetInfo.ProjectPath;
            }
            else
            {
                targetPath = await AssetInventory.EnsureMaterializedAsset(assetInfo);
            }

            if (targetPath != null) EditorUtility.RevealInFinder(targetPath);
            _previewInProgress = false;
        }

        private async void CopyTo(AssetInfo assetInfo, string targetFolder, bool withDependencies = false, bool withScripts = false, bool autoPing = true, bool fromDragDrop = false)
        {
            _previewInProgress = true;

            string mainFile = await AssetInventory.CopyTo(assetInfo, targetFolder, withDependencies, withScripts, fromDragDrop);
            if (autoPing && mainFile != null)
            {
                PingAsset(new AssetInfo().WithProjectPath(mainFile));
                if (AssetInventory.Config.statsImports == 5) ShowInterstitial();
            }

            _previewInProgress = false;
        }

        private void ShowInterstitial()
        {
            if (EditorUtility.DisplayDialog("Your Support Counts", "This message will only appear once. Thanks for using Asset Inventory! I hope you enjoy using it.\n\n" +
                "Developing a rather ground-braking asset like this as a solo-dev requires a huge amount of time and work.\n\n" +
                "Please consider leaving a review and spreading the word. This is so important on the Asset Store and is the only way to make asset development viable.\n\n"
                , "Leave Review", "Maybe Later"))
            {
                Application.OpenURL(AssetInventory.ASSET_STORE_LINK);
            }
        }

        private void CreateReportTree()
        {
            _requireReportTreeRebuild = false;
            List<AssetInfo> data = new List<AssetInfo>();
            AssetInfo root = new AssetInfo().WithTreeData("Root", depth: -1);
            data.Add(root);

            if (_assetUsage != null)
            {
                // apply filters
                IEnumerable<AssetInfo> filteredAssets = _assetUsage.GroupBy(a => a.AssetId).Select(a => a.First()).Where(a => !string.IsNullOrEmpty(a.GetDisplayName(false)));

                IOrderedEnumerable<AssetInfo> orderedAssets = filteredAssets.OrderBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);
                orderedAssets.ToList().ForEach(a => data.Add(a.WithTreeData(a.GetDisplayName(false), a.AssetId)));
            }

            ReportTreeModel.SetData(data, true);
            ReportTreeView.Reload();
            OnReportTreeSelectionChanged(ReportTreeView.GetSelection());

            if (_textureLoading3 != null) EditorCoroutineUtility.StopCoroutine(_textureLoading3);
            _textureLoading3 = EditorCoroutineUtility.StartCoroutine(AssetUtils.LoadTextures(data), this);
        }

        private void CreateAssetTree()
        {
            _requireAssetTreeRebuild = false;
            List<AssetInfo> data = new List<AssetInfo>();
            AssetInfo root = new AssetInfo().WithTreeData("Root", depth: -1);
            data.Add(root);

            // apply filters
            IEnumerable<AssetInfo> filteredAssets = _assets;
            switch (AssetInventory.Config.assetDeprecation)
            {
                case 1:
                    filteredAssets = filteredAssets.Where(a => !a.IsDeprecated && !a.IsAbandoned);
                    break;

                case 2:
                    filteredAssets = filteredAssets.Where(a => a.IsDeprecated || a.IsAbandoned);
                    break;
            }
            switch (AssetInventory.Config.packagesListing)
            {
                case 1:
                    filteredAssets = filteredAssets.Where(a => a.AssetSource != Asset.Source.Package);
                    break;

                case 2:
                    filteredAssets = filteredAssets.Where(a => a.AssetSource == Asset.Source.Package);
                    break;

                case 3:
                    filteredAssets = filteredAssets.Where(a => a.AssetSource == Asset.Source.CustomPackage);
                    break;

                case 4:
                    filteredAssets = filteredAssets.Where(a => a.AssetSource == Asset.Source.Directory);
                    break;

                case 5:
                    filteredAssets = filteredAssets.Where(a => a.AssetSource == Asset.Source.Archive);
                    break;
            }
            switch (_selectedMaintenance)
            {
                case 1:
                    filteredAssets = filteredAssets.Where(a => a.IsUpdateAvailable(_assets) || a.WasOutdated);
                    break;

                case 2:
                    filteredAssets = filteredAssets.Where(a => a.CurrentSubState == Asset.SubState.Outdated);
                    break;

                case 3:
                    filteredAssets = filteredAssets.Where(a => a.AssetSource == Asset.Source.AssetStorePackage && a.OfficialState == "disabled");
                    break;

                case 4:
                    filteredAssets = filteredAssets.Where(a => a.AssetSource == Asset.Source.CustomPackage && a.ForeignId > 0);
                    break;

                case 5:
                    filteredAssets = filteredAssets.Where(a => a.FileCount > 0);
                    break;

                case 6:
                    filteredAssets = filteredAssets.Where(a => a.FileCount == 0);
                    break;

                case 7:
                    filteredAssets = filteredAssets.Where(a => !string.IsNullOrEmpty(a.Registry) && a.Registry != "Unity");
                    break;

                case 8:
                    filteredAssets = filteredAssets.Where(AssetStore.IsInstalled);
                    break;

                case 9:
                    filteredAssets = filteredAssets.Where(a => a.IsDownloading());
                    break;

                case 10:
                    filteredAssets = filteredAssets.Where(a => !a.Downloaded);
                    break;

                case 11:
                    List<int> duplicates = filteredAssets.Where(a => a.ForeignId > 0).GroupBy(a => a.ForeignId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                    filteredAssets = filteredAssets.Where(a => duplicates.Contains(a.ForeignId));
                    break;

                case 12:
                    filteredAssets = filteredAssets.Where(a => a.Backup);
                    break;

                case 13:
                    filteredAssets = filteredAssets.Where(a => !a.Backup);
                    break;
            }

            string[] lastGroups = Array.Empty<string>();
            int catIdx = 0;
            IOrderedEnumerable<AssetInfo> orderedAssets;
            switch (AssetInventory.Config.assetGrouping)
            {
                case 0: // none
                    orderedAssets = AddPackageOrdering(filteredAssets);
                    orderedAssets.ToList().ForEach(a => data.Add(a.WithTreeData(a.GetDisplayName(), a.AssetId)));
                    break;

                case 2: // category
                    orderedAssets = filteredAssets.OrderBy(a => a.GetDisplayCategory(), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);

                    string[] noCat = {"-no category-"};
                    foreach (AssetInfo info in orderedAssets)
                    {
                        // create hierarchy
                        string[] cats = string.IsNullOrEmpty(info.GetDisplayCategory()) ? noCat : info.GetDisplayCategory().Split('/');

                        lastGroups = AddCategorizedItem(cats, lastGroups, data, info, ref catIdx);
                    }
                    break;

                case 3: // publisher
                    IOrderedEnumerable<AssetInfo> orderedAssetsPub = filteredAssets.OrderBy(a => a.GetDisplayPublisher(), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);

                    string[] noPub = {"-no publisher-"};
                    foreach (AssetInfo info in orderedAssetsPub)
                    {
                        // create hierarchy
                        string[] pubs = string.IsNullOrEmpty(info.GetDisplayPublisher()) ? noPub : new[] {info.GetDisplayPublisher()};

                        lastGroups = AddCategorizedItem(pubs, lastGroups, data, info, ref catIdx);
                    }
                    break;

                case 4: // tags
                    List<Tag> tags = AssetInventory.LoadTags();
                    foreach (Tag tag in tags)
                    {
                        IOrderedEnumerable<AssetInfo> taggedAssets = filteredAssets
                            .Where(a => a.PackageTags != null && a.PackageTags.Any(t => t.Name == tag.Name))
                            .OrderBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);

                        string[] cats = {tag.Name};
                        foreach (AssetInfo info in taggedAssets)
                        {
                            // create hierarchy
                            lastGroups = AddCategorizedItem(cats, lastGroups, data, info, ref catIdx);
                        }
                    }

                    IOrderedEnumerable<AssetInfo> remainingAssets = filteredAssets
                        .Where(a => a.PackageTags == null || a.PackageTags.Count == 0)
                        .OrderBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);
                    string[] untaggedCat = {"-untagged-"};
                    foreach (AssetInfo info in remainingAssets)
                    {
                        lastGroups = AddCategorizedItem(untaggedCat, lastGroups, data, info, ref catIdx);
                    }
                    break;

                case 5: // state
                    IOrderedEnumerable<AssetInfo> orderedAssetsState = filteredAssets.OrderBy(a => a.OfficialState, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);

                    string[] noState = {"-no state-"};
                    foreach (AssetInfo info in orderedAssetsState)
                    {
                        // create hierarchy
                        string[] pubs = string.IsNullOrEmpty(info.OfficialState) ? noState : new[] {info.OfficialState};

                        lastGroups = AddCategorizedItem(pubs, lastGroups, data, info, ref catIdx);
                    }
                    break;
            }

            AssetTreeModel.SetData(data, true);
            AssetTreeView.Reload();
            OnAssetTreeSelectionChanged(AssetTreeView.GetSelection());

            if (_textureLoading2 != null) EditorCoroutineUtility.StopCoroutine(_textureLoading2);
            _textureLoading2 = EditorCoroutineUtility.StartCoroutine(AssetUtils.LoadTextures(data), this);
        }

        private IOrderedEnumerable<AssetInfo> AddPackageOrdering(IEnumerable<AssetInfo> list)
        {
            IOrderedEnumerable<AssetInfo> result = null;
            if (!AssetInventory.Config.sortAssetsDescending)
            {
                switch (AssetInventory.Config.assetSorting)
                {
                    case 0:
                        result = list.OrderBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);
                        break;

                    case 1:
                        result = list.OrderBy(a => a.PurchaseDate);
                        break;

                    case 2:
                        result = list.OrderBy(a => a.LastRelease);
                        break;
                }
            }
            else
            {
                switch (AssetInventory.Config.assetSorting)
                {
                    case 0:
                        result = list.OrderByDescending(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);
                        break;

                    case 1:
                        result = list.OrderByDescending(a => a.PurchaseDate);
                        break;

                    case 2:
                        result = list.OrderByDescending(a => a.LastRelease);
                        break;
                }
            }
            if (result == null) result = list.OrderBy(a => a.LastRelease);

            return result.ThenBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);
        }

        private static string[] AddCategorizedItem(string[] cats, string[] lastCats, List<AssetInfo> data, AssetInfo info, ref int catIdx)
        {
            // find first difference to previous cat
            if (!ArrayUtility.ArrayEquals(cats, lastCats))
            {
                int firstDiff = 0;
                bool diffFound = false;
                for (int i = 0; i < Mathf.Min(cats.Length, lastCats.Length); i++)
                {
                    if (cats[i] != lastCats[i])
                    {
                        firstDiff = i;
                        diffFound = true;
                        break;
                    }
                }
                if (!diffFound) firstDiff = lastCats.Length;

                for (int i = firstDiff; i < cats.Length; i++)
                {
                    catIdx--;
                    AssetInfo catItem = new AssetInfo().WithTreeData(cats[i], catIdx, i);
                    data.Add(catItem);
                }
            }

            AssetInfo item = info.WithTreeData(info.GetDisplayName(), info.AssetId, cats.Length);
            data.Add(item);

            return cats;
        }

        private async void FetchAssetPurchases(bool forceUpdate)
        {
            AssetPurchases result = await AssetInventory.FetchOnlineAssets();
            if (AssetStore.CancellationRequested || result == null) return;

            _purchasedAssets = result;
            _purchasedAssetsCount = _purchasedAssets?.total ?? 0;
            ReloadLookups();
            FetchAssetDetails(forceUpdate);
        }

        private async void FetchAssetDetails(bool forceUpdate = false)
        {
            if (forceUpdate) DBAdapter.DB.Execute("update Asset set ETag=null, LastOnlineRefresh=0");
            await AssetInventory.FetchAssetsDetails();
            ReloadLookups();
            _requireAssetTreeRebuild = true;
        }

        private void SetPage(int newPage)
        {
            newPage = Mathf.Clamp(newPage, 1, _pageCount);
            if (newPage != _curPage)
            {
                _curPage = newPage;
                _gridSelection = 0;
                _searchScrollPos = Vector2.zero;
                if (_curPage > 0) PerformSearch(true);
            }
        }

        private void UpdateStatistics()
        {
            if (AssetInventory.DEBUG_MODE) Debug.LogWarning("Update Statistics");
            if (Application.isPlaying) return;

            _assets = AssetInventory.LoadAssets();
            _tags = AssetInventory.LoadTags();
            _packageCount = _assets.Count;
            _indexedPackageCount = _assets.Count(a => a.FileCount > 0);
            _deprecatedAssetsCount = _assets.Count(a => a.IsDeprecated);
            _excludedAssetsCount = _assets.Count(a => a.Exclude);
            _registryPackageCount = _assets.Count(a => a.AssetSource == Asset.Source.Package);
            _customPackageCount = _assets.Count(a => a.AssetSource == Asset.Source.CustomPackage || a.SafeName == Asset.NONE);
            _packageFileCount = DBAdapter.DB.Table<AssetFile>().Count();

            // only load slow statistics on Index tab when nothing else is running
            if (_tab == 3)
            {
                _dbSize = DBAdapter.GetDBSize();
            }

            _requireAssetTreeRebuild = true;
        }

        private void PerformSearch(bool keepPage = false)
        {
            if (AssetInventory.DEBUG_MODE) Debug.LogWarning("Perform Search");

            _requireSearchUpdate = false;
            _keepSearchResultPage = true;
            int lastCount = _resultCount; // a bit of a heuristic but works great and is very performant
            string selectedSize = _resultSizes[AssetInventory.Config.maxResults];
            int.TryParse(selectedSize, out int maxResults);
            if (maxResults <= 0 || maxResults > AssetInventory.Config.maxResultsLimit) maxResults = AssetInventory.Config.maxResultsLimit;
            List<string> wheres = new List<string>();
            List<object> args = new List<object>();
            string escape = "";
            string packageTagJoin = "";
            string fileTagJoin = "";
            string lastWhere = null;

            wheres.Add("(Asset.Exclude=0 or Asset.Exclude is null)");

            // only add detail filters if section is open to not have confusing search results
            if (AssetInventory.Config.showFilterBar)
            {
                // numerical conditions first
                if (!string.IsNullOrWhiteSpace(_searchWidth))
                {
                    if (int.TryParse(_searchWidth, out int width) && width > 0)
                    {
                        string widthComp = _checkMaxWidth ? "<=" : ">=";
                        wheres.Add($"AssetFile.Width > 0 and AssetFile.Width {widthComp} ?");
                        args.Add(width);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchHeight))
                {
                    if (int.TryParse(_searchHeight, out int height) && height > 0)
                    {
                        string heightComp = _checkMaxHeight ? "<=" : ">=";
                        wheres.Add($"AssetFile.Height > 0 and AssetFile.Height {heightComp} ?");
                        args.Add(height);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchLength))
                {
                    if (float.TryParse(_searchLength, out float length) && length > 0)
                    {
                        string lengthComp = _checkMaxLength ? "<=" : ">=";
                        wheres.Add($"AssetFile.Length > 0 and AssetFile.Length {lengthComp} ?");
                        args.Add(length);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchSize))
                {
                    if (int.TryParse(_searchSize, out int size) && size > 0)
                    {
                        string sizeComp = _checkMaxSize ? "<=" : ">=";
                        wheres.Add($"AssetFile.Size > 0 and AssetFile.Size {sizeComp} ?");
                        args.Add(size * 1024);
                    }
                }

                if (_selectedPackageTag > 0 && _tagNames.Length > _selectedPackageTag)
                {
                    string[] arr = _tagNames[_selectedPackageTag].Split('/');
                    string tag = arr[arr.Length - 1];
                    wheres.Add("tap.TagId = ?");
                    args.Add(_tags.First(t => t.Name == tag).Id);

                    packageTagJoin = "inner join TagAssignment as tap on (Asset.Id = tap.TargetId and tap.TagTarget = 0)";
                }

                if (_selectedFileTag > 0 && _tagNames.Length > _selectedFileTag)
                {
                    string[] arr = _tagNames[_selectedFileTag].Split('/');
                    string tag = arr[arr.Length - 1];
                    wheres.Add("taf.TagId = ?");
                    args.Add(_tags.First(t => t.Name == tag).Id);

                    fileTagJoin = "inner join TagAssignment as taf on (AssetFile.Id = taf.TargetId and taf.TagTarget = 1)";
                }

                switch (_selectedPackageTypes)
                {
                    case 1:
                        wheres.Add("Asset.AssetSource != ?");
                        args.Add(Asset.Source.Package);
                        break;

                    case 2:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.Package);
                        break;

                    case 3:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.CustomPackage);
                        break;

                    case 4:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.Directory);
                        break;

                    case 5:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.Archive);
                        break;
                }

                if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
                {
                    string[] arr = _publisherNames[_selectedPublisher].Split('/');
                    string publisher = arr[arr.Length - 1];
                    wheres.Add("Asset.SafePublisher = ?");
                    args.Add($"{publisher}");
                }

                if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
                {
                    string[] arr = _assetNames[_selectedAsset].Split('/');
                    string asset = arr[arr.Length - 1];
                    wheres.Add("Asset.SafeName = ?"); // TODO: going via In would be more efficient but not available at this point
                    args.Add($"{asset}");
                }

                if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
                {
                    string[] arr = _categoryNames[_selectedCategory].Split('/');
                    string category = arr[arr.Length - 1];
                    wheres.Add("Asset.SafeCategory = ?");
                    args.Add($"{category}");
                }

                if (_selectedColorOption > 0)
                {
                    wheres.Add("AssetFile.Hue >= ?");
                    wheres.Add("AssetFile.Hue <= ?");
                    args.Add(_selectedColor.ToHue() - AssetInventory.Config.hueRange / 2f);
                    args.Add(_selectedColor.ToHue() + AssetInventory.Config.hueRange / 2f);
                }
            }

            if (!string.IsNullOrWhiteSpace(_searchPhrase))
            {
                string phrase = _searchPhrase;
                string searchField = "AssetFile.Path";

                switch (AssetInventory.Config.searchField)
                {
                    case 1:
                        searchField = "AssetFile.FileName";
                        break;
                }

                // check for sqlite escaping requirements
                if (phrase.Contains("_"))
                {
                    phrase = phrase.Replace("_", "\\_");
                    escape = "ESCAPE '\\'";
                }

                if (_searchPhrase.StartsWith("=")) // expert mode
                {
                    if (_searchPhrase.Length > 1) lastWhere = _searchPhrase.Substring(1) + $" {escape}";
                }
                else if (_searchPhrase.StartsWith("~")) // exact mode
                {
                    string term = _searchPhrase.Substring(1);
                    wheres.Add($"{searchField} like ? {escape}");
                    args.Add($"%{term}%");
                }
                else
                {
                    string[] fuzzyWords = _searchPhrase.Split(' ');
                    foreach (string fuzzyWord in fuzzyWords.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        if (fuzzyWord.StartsWith("+"))
                        {
                            wheres.Add($"{searchField} like ? {escape}");
                            args.Add($"%{fuzzyWord.Substring(1)}%");
                        }
                        else if (fuzzyWord.StartsWith("-"))
                        {
                            wheres.Add($"{searchField} not like ? {escape}");
                            args.Add($"%{fuzzyWord.Substring(1)}%");
                        }
                        else
                        {
                            wheres.Add($"{searchField} like ? {escape}");
                            args.Add($"%{fuzzyWord}%");
                        }
                    }
                }
            }

            if (AssetInventory.Config.searchType > 0 && _types.Length > AssetInventory.Config.searchType)
            {
                string rawType = _types[AssetInventory.Config.searchType];
                string[] type = rawType.Split('/');
                if (type.Length > 1)
                {
                    wheres.Add("AssetFile.Type = ?");
                    args.Add(type.Last());
                }
                else if (AssetInventory.TypeGroups.ContainsKey(rawType))
                {
                    // sqlite does not support binding lists, parameters must be spelled out
                    List<string> paramCount = new List<string>();
                    foreach (string t in AssetInventory.TypeGroups[rawType])
                    {
                        paramCount.Add("?");
                        args.Add(t);
                    }

                    wheres.Add("AssetFile.Type in (" + string.Join(",", paramCount) + ")");
                }
            }

            if (!string.IsNullOrWhiteSpace(AssetInventory.Config.excludedExtensions))
            {
                string[] extensions = AssetInventory.Config.excludedExtensions.Split(';');
                List<string> paramCount = new List<string>();
                foreach (string ext in extensions)
                {
                    paramCount.Add("?");
                    args.Add(ext.Trim());
                }

                wheres.Add("AssetFile.Type not in (" + string.Join(",", paramCount) + ")");
            }

            switch (AssetInventory.Config.previewVisibility)
            {
                case 2:
                    wheres.Add("(AssetFile.PreviewState = 1 or AssetFile.PreviewState = 3)");
                    break;

                case 3:
                    wheres.Add("(AssetFile.PreviewState != 1 and AssetFile.PreviewState != 3)");
                    break;
            }

            // ordering, can only be done on DB side since post-processing results would only work on the paged results which is incorrect
            string orderBy = "order by ";
            switch (AssetInventory.Config.sortField)
            {
                case 0:
                    orderBy += "AssetFile.Path";
                    break;

                case 1:
                    orderBy += "AssetFile.FileName";
                    break;

                case 2:
                    orderBy += "AssetFile.Size";
                    break;

                case 3:
                    orderBy += "AssetFile.Type";
                    break;

                case 4:
                    orderBy += "AssetFile.Length";
                    break;

                case 5:
                    orderBy += "AssetFile.Width";
                    break;

                case 6:
                    orderBy += "AssetFile.Height";
                    break;

                case 7:
                    orderBy += "AssetFile.Hue";
                    wheres.Add("AssetFile.Hue >=0");
                    break;

                case 8:
                    orderBy += "Asset.DisplayCategory";
                    break;

                case 9:
                    orderBy += "Asset.LastRelease";
                    break;

                case 10:
                    orderBy += "Asset.AssetRating";
                    break;

                case 11:
                    orderBy += "Asset.RatingCount";
                    break;

                default:
                    orderBy = "";
                    break;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                orderBy += " COLLATE NOCASE";
                if (AssetInventory.Config.sortDescending) orderBy += " desc";
                orderBy += ", AssetFile.Path"; // always sort by path in case of equality of first level sorting
            }
            if (!string.IsNullOrEmpty(lastWhere)) wheres.Add(lastWhere);

            string where = wheres.Count > 0 ? "where " + string.Join(" and ", wheres) : "";
            string baseQuery = $"from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId {packageTagJoin} {fileTagJoin} {where}";
            string countQuery = $"select count(*) {baseQuery}";
            string dataQuery = $"select *, AssetFile.Id as Id {baseQuery} {orderBy}";
            if (maxResults > 0) dataQuery += $" limit {maxResults} offset {(_curPage - 1) * maxResults}";
            try
            {
                _searchError = null;
                _resultCount = DBAdapter.DB.ExecuteScalar<int>($"{countQuery}", args.ToArray());
                _files = DBAdapter.DB.Query<AssetInfo>($"{dataQuery}", args.ToArray());
            }
            catch (SQLiteException e)
            {
                _searchError = e.Message;
            }

            // preview images
            if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
            _textureLoading = EditorCoroutineUtility.StartCoroutine(LoadTextures(false), this); // TODO: should be true once pages endless scrolling is in

            // pagination
            _contents = _files.Select(file =>
            {
                string text = "";
                int tileTextToUse = AssetInventory.Config.tileText;
                if (tileTextToUse == 0) // intelligent
                {
                    if (AssetInventory.Config.tileSize < 70)
                    {
                        tileTextToUse = 6;
                    }
                    else if (AssetInventory.Config.tileSize < 90)
                    {
                        tileTextToUse = 4;
                    }
                    else if (AssetInventory.Config.tileSize < 120)
                    {
                        tileTextToUse = 3;
                    }
                    else
                    {
                        tileTextToUse = 2;
                    }
                }
                switch (tileTextToUse)
                {
                    case 2:
                        text = file.ShortPath;
                        break;

                    case 3:
                        text = file.FileName;
                        break;

                    case 4:
                        text = Path.GetFileNameWithoutExtension(file.FileName);
                        break;
                }
                text = text == null ? "" : text.Replace('/', Path.DirectorySeparatorChar);

                return new GUIContent(text);
            }).ToArray();
            _pageCount = AssetUtils.GetPageCount(_resultCount, maxResults);
            if (!keepPage && lastCount != _resultCount)
            {
                SetPage(1);
            }
            else
            {
                SetPage(_curPage);
            }
            _searchDone = true;
        }

        private IEnumerator LoadTextures(bool firstPageOnly)
        {
            string previewFolder = AssetInventory.GetPreviewFolder();
            int idx = -1;
            IEnumerable<AssetInfo> files = _files.Take(firstPageOnly ? 20 * 8 : _files.Count);
            foreach (AssetInfo file in files)
            {
                idx++;
                if (!file.HasPreview())
                {
                    if (!AssetInventory.Config.showIconsForMissingPreviews) continue;

                    // check if well-known extension
                    if (_staticPreviews.ContainsKey(file.Type))
                    {
                        _contents[idx].image = EditorGUIUtility.IconContent(_staticPreviews[file.Type]).image;
                    }
                    else
                    {
                        _contents[idx].image = EditorGUIUtility.IconContent("d_DefaultAsset Icon").image;
                    }
                    continue;
                }
                string previewFile = file.GetPreviewFile(previewFolder);
                if (!File.Exists(previewFile))
                {
                    Debug.LogWarning($"Preview file for {file} does not exist anymore. Marking it missing for recreation.");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.Redo, file.Id);
                    file.PreviewState = AssetFile.PreviewOptions.None;
                    continue;
                }

                yield return AssetUtils.LoadTexture(previewFile, result =>
                {
                    if (_contents.Length > idx) _contents[idx].image = result;
                }, false, AssetInventory.Config.upscalePreviews ? AssetInventory.Config.upscaleSize : 0);
            }
        }

        private void OpenInPackageView(AssetInfo info)
        {
            _tab = 1;
            AssetTreeView.SetSelection(new[] {info.AssetId});
            OnAssetTreeSelectionChanged(AssetTreeView.GetSelection());
        }

        private void OpenInSearch(AssetInfo info, bool force = false)
        {
            if (info.Id <= 0) return;
            if (!force && info.FileCount <= 0) return;
            AssetInfo oldEntry = _selectedEntry;

            if (info.Exclude)
            {
                if (!EditorUtility.DisplayDialog("Package is Excluded", "This package is currently excluded from the search. Should it be included again?", "Include Again", "Cancel"))
                {
                    return;
                }
                AssetInventory.SetAssetExclusion(info, false);
                ReloadLookups();
            }
            ResetSearch(false);
            if (force) _selectedEntry = oldEntry;

            _tab = 0;

            // search for exact match first
            _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a == info.SafeName));
            if (_selectedAsset < 0) _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a == info.SafeName.Substring(0, 1) + "/" + info.SafeName));
            if (_selectedAsset < 0) _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a.EndsWith(info.SafeName)));

            if (info.AssetSource == Asset.Source.Package && _selectedPackageTypes == 1) _selectedPackageTypes = 0;
            _requireSearchUpdate = true;
            AssetInventory.Config.showFilterBar = true;
        }

        private void ResetSearch(bool filterBarOnly)
        {
            if (!filterBarOnly)
            {
                _searchPhrase = "";
                AssetInventory.Config.searchType = 0;
            }

            _selectedEntry = null;
            _selectedAsset = 0;
            _selectedPackageTypes = 1;
            _selectedColorOption = 0;
            _selectedColor = Color.clear;
            _selectedPackageTag = 0;
            _selectedFileTag = 0;
            _selectedPublisher = 0;
            _selectedCategory = 0;
            _searchHeight = "";
            _searchWidth = "";
            _searchLength = "";
            _searchSize = "";
        }

        private void OnReportTreeDoubleClicked(int id)
        {
            if (id <= 0) return;

            AssetInfo info = ReportTreeModel.Find(id);
            OpenInSearch(info, true);
        }

        private void OnReportTreeSelectionChanged(IList<int> ids)
        {
            _selectedReportEntry = null;
            _selectedReportEntries = _selectedReportEntries ?? new List<AssetInfo>();
            _selectedReportEntries.Clear();

            if (ids.Count == 1 && ids[0] > 0)
            {
                _selectedReportEntry = ReportTreeModel.Find(ids[0]);
                _selectedReportEntry?.Refresh();
            }
            foreach (int id in ids)
            {
                GatherTreeChildren(id, _selectedReportEntries, ReportTreeModel);
            }
            _reportBulkTags.Clear();
            _selectedReportEntries.ForEach(info => info.PackageTags?.ForEach(t =>
            {
                if (!_reportBulkTags.ContainsKey(t.Name)) _reportBulkTags.Add(t.Name, new Tuple<int, Color>(0, t.GetColor()));
                _reportBulkTags[t.Name] = new Tuple<int, Color>(_reportBulkTags[t.Name].Item1 + 1, _reportBulkTags[t.Name].Item2);
            }));

            _reportTreeSelectionSize = _selectedReportEntries.Sum(a => a.PackageSize);
        }

        private void OnAssetTreeSelectionChanged(IList<int> ids)
        {
            _selectedTreeAsset = null;
            _selectedTreeAssets = _selectedTreeAssets ?? new List<AssetInfo>();
            _selectedTreeAssets.Clear();

            if (ids.Count == 1 && ids[0] > 0)
            {
                _selectedTreeAsset = AssetTreeModel.Find(ids[0]);
                _selectedTreeAsset?.Refresh();
            }
            foreach (int id in ids)
            {
                GatherTreeChildren(id, _selectedTreeAssets, AssetTreeModel);
            }
            _assetBulkTags.Clear();
            _selectedTreeAssets.ForEach(info => info.PackageTags?.ForEach(t =>
            {
                if (!_assetBulkTags.ContainsKey(t.Name)) _assetBulkTags.Add(t.Name, new Tuple<int, Color>(0, t.GetColor()));
                _assetBulkTags[t.Name] = new Tuple<int, Color>(_assetBulkTags[t.Name].Item1 + 1, _assetBulkTags[t.Name].Item2);
            }));

            _assetTreeSelectionSize = _selectedTreeAssets.Sum(a => a.PackageSize);
        }

        private void GatherTreeChildren(int id, List<AssetInfo> result, TreeModel<AssetInfo> treeModel)
        {
            AssetInfo info = treeModel.Find(id);
            if (info == null) return;

            if (info.HasChildren)
            {
                foreach (TreeElement subInfo in info.Children)
                {
                    GatherTreeChildren(subInfo.TreeId, result, treeModel);
                }
            }
            else if (info.Id > 0)
            {
                if (result.All(existing => info.Id != existing.Id)) result.Add(info);
            }
        }

        private void OnAssetTreeDoubleClicked(int id)
        {
            if (id <= 0) return;

            AssetInfo info = AssetTreeModel.Find(id);
            OpenInSearch(info);
        }

        private void DrawFoldersListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= AssetInventory.Config.folders.Count) return;

            FolderSpec spec = AssetInventory.Config.folders[index];

            if (isFocused) _selectedFolderIndex = index;

            EditorGUI.BeginChangeCheck();
            spec.enabled = GUI.Toggle(new Rect(rect.x, rect.y, 20, EditorGUIUtility.singleLineHeight), spec.enabled, UIStyles.Content("", "Include folder when indexing"), UIStyles.toggleStyle);
            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            GUI.Label(new Rect(rect.x + 20, rect.y, rect.width - 250, EditorGUIUtility.singleLineHeight), spec.location, UIStyles.entryStyle);
            GUI.Label(new Rect(rect.x + rect.width - 230, rect.y, 200, EditorGUIUtility.singleLineHeight), UIStyles.FolderTypes[spec.folderType] + (spec.folderType == 1 ? " (" + UIStyles.MediaTypes[spec.scanFor] + ")" : ""), UIStyles.entryStyle);
            if (GUI.Button(new Rect(rect.x + rect.width - 30, rect.y + 1, 30, 20), EditorGUIUtility.IconContent("Settings", "|Show/Hide Settings Tab")))
            {
                FolderSettingsUI folderSettingsUI = new FolderSettingsUI();
                folderSettingsUI.Init(spec);
                PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), folderSettingsUI);
            }
        }

        private async void CheckForUpdates()
        {
            _updateAvailable = false;

            await Task.Delay(2000); // let remainder of window initialize first
            if (string.IsNullOrEmpty(CloudProjectSettings.accessToken)) return;

            _onlineInfo = await AssetStore.RetrieveAssetDetails(AssetInventory.ASSET_STORE_ID);
            if (_onlineInfo == null) return;

            _updateAvailable = new SemVer(_onlineInfo.version.name) > new SemVer(AssetInventory.TOOL_VERSION);
        }

        private void CreateDebugReport()
        {
            string reportFile = Path.Combine(AssetInventory.GetStorageFolder(), "DebugReport.log");
            File.WriteAllText(reportFile, AssetInventory.CreateDebugReport());
            EditorUtility.RevealInFinder(reportFile);
        }

        private async void PerformCopyTo(AssetInfo info, string path, bool fromDragDrop = false)
        {
            if (info.InProject) return;
            if (string.IsNullOrEmpty(path)) return;

            if (info.DependencyState == AssetInfo.DependencyStateOptions.Unknown) await CalculateDependencies(_selectedEntry);
            if (info.DependencySize > 0 && AssetInventory.ScanDependencies.Contains(info.Type))
            {
                CopyTo(info, path, true, false, false, fromDragDrop);
            }
            else
            {
                CopyTo(info, path, false, false, true, fromDragDrop);
            }
        }

        private static bool DragDropAvailable()
        {
#if UNITY_2021_2_OR_NEWER
            return true;
#else
            return false;
#endif
        }

        private void InitDragAndDrop()
        {
#if UNITY_2021_2_OR_NEWER
            DragAndDrop.ProjectBrowserDropHandler dropHandler = OnProjectWindowDrop;
            if (!DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.AddDropHandler(dropHandler);
            }
#endif
        }

        private void DeinitDragAndDrop()
        {
#if UNITY_2021_2_OR_NEWER
            DragAndDrop.ProjectBrowserDropHandler dropHandler = OnProjectWindowDrop;
            if (DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.RemoveDropHandler(dropHandler);
            }
#endif
        }

        private DragAndDropVisualMode OnProjectWindowDrop(int dragInstanceId, string dropUponPath, bool perform)
        {
            if (perform && _dragging)
            {
                _dragging = false;
                DeinitDragAndDrop();

                AssetInfo info = (AssetInfo)DragAndDrop.GetGenericData("AssetInfo");
                if (info != null) // can happen in some edge asynchronous scenarios
                {
                    if (File.Exists(dropUponPath)) dropUponPath = Path.GetDirectoryName(dropUponPath);
                    PerformCopyTo(info, dropUponPath, true);
                }
                DragAndDrop.AcceptDrag();
            }
            return DragAndDropVisualMode.Copy;
        }

        private void HandleDragDrop()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDrag:
                    if (!_mouseOverSearchResultRect) return;
                    if (!_dragging && _selectedEntry != null)
                    {
                        _dragging = true;

                        InitDragAndDrop();
                        DragAndDrop.PrepareStartDrag();

                        DragAndDrop.SetGenericData("AssetInfo", _selectedEntry);
                        DragAndDrop.objectReferences = new Object[] {Logo};
                        DragAndDrop.StartDrag("Dragging " + _selectedEntry);
                        Event.current.Use();
                    }
                    break;

                /* FIXME: not finishing the drag will cause no DeInit right now, needs subsequent mouse up event
                case EventType.DragExited:
                    // drag exit will also fire when out of drag-start-control bounds
                    if (!_mouseOverSearchResultRect) return;
                    _dragging = false;
                    DeinitDragAndDrop();
                    break;
                */

                case EventType.MouseUp:
                    _dragging = false;
                    DeinitDragAndDrop();

                    // clean up, also in case MouseDrag never occurred
                    DragAndDrop.PrepareStartDrag();
                    break;
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
