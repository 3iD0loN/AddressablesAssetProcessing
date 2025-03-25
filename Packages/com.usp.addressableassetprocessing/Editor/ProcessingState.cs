using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using System.Diagnostics;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine.UIElements;
    using USP.MetaAddressables;

    public interface IAssetIdentifier
    {
        #region Properties
        string ParentDirectoryPath { get; }

        AssetPathCollector AssetCollectionMethod { get; }

        IReadOnlyDictionary<string, string> AssetFilePathsByGuids { get; }
        #endregion

        #region Methods
        void GetFiles();
        #endregion
    }

    public class AssetIdentifier : IAssetIdentifier
    {
        #region Field
        private Dictionary<string, string> assetFilePathsByGuids;
        #endregion

        #region Properties
        public string ParentDirectoryPath { get; }

        public AssetPathCollector AssetCollectionMethod { get; }

        public IReadOnlyDictionary<string, string> AssetFilePathsByGuids => assetFilePathsByGuids;
        #endregion

        #region Methods
        public AssetIdentifier(string parentDirectoryPath, AssetPathCollector assetCollectionMethod) :
            this(parentDirectoryPath, assetCollectionMethod, new Dictionary<string, string>())
        {
        }

        private AssetIdentifier(string parentDirectoryPath, AssetPathCollector assetCollectionMethod, Dictionary<string, string> assetFilePathsByGuids)
        {
            this.ParentDirectoryPath = parentDirectoryPath;
            this.AssetCollectionMethod = assetCollectionMethod;
            this.assetFilePathsByGuids = assetFilePathsByGuids;
        }

        public void GetFiles()
        {
            AssetCollectionMethod.GetFiles(ParentDirectoryPath, ref assetFilePathsByGuids);
        }
        #endregion
    }

    public interface IRuleProcessor
    {
        #region Properties
        AddressableAssetSettings Settings { get; }

        string AssetFilePath { get; }

        RuleSettings Rules { get; }

        MetaAddressables.UserData ProcessedData { get; }
        #endregion

        #region Methods
        void ProcessRules();
        #endregion
    }

    public class RuleProcessor : IRuleProcessor
    {
        #region Properties
        public AddressableAssetSettings Settings { get; }

        public string AssetFilePath { get; }

        public RuleSettings Rules { get; }

        public MetaAddressables.UserData ProcessedData
        {
            get
            {
                // After processing, the processed asest is located in the asset store.
                Rules.AssetApplicationMethod.AssetStore.DataByAssetPath.TryGetValue(AssetFilePath,
                    out MetaAddressables.UserData result);

                return result;
            }
        }

        public ComparisonEntry ComparisonEntry
        {
            get
            {
                return ComparisonEntries.CreateEntry(Settings, Rules.AssetApplicationMethod as CombinedAssetApplicator, AssetFilePath);
            }
        }
        #endregion

        #region Methods
        public RuleProcessor(AddressableAssetSettings settings, RuleSettings rules) :
            this(settings, string.Empty, rules)
        {
        }

        public RuleProcessor(AddressableAssetSettings settings, string assetFilePath, RuleSettings rules)
        {
            Settings = settings;
            AssetFilePath = assetFilePath;
            Rules = rules;
        }

        public void ProcessRules()
        {
            if (string.IsNullOrEmpty(AssetFilePath))
            {
                return;
            }

            AddressablesProcessing.ProcessAsset(Settings,
                AssetFilePath,
                Rules.GroupExtractionMethod,
                Rules.AddressExtractionMethod,
                Rules.LabelExtractionMethod,
                Rules.AssetApplicationMethod);
        }
        #endregion
    }

    public class ComparisonEntryFactory
    {
        #region Properties
        public AddressableAssetSettings Settings { get; }

        public string AssetFilePath { get; }

        public CombinedAssetApplicator AssetApplicationMethod { get; }

        public ComparisonEntry ComparisonEntry
        {
            get;
            private set;
        }
        #endregion

        #region Methods
        public ComparisonEntryFactory(AddressableAssetSettings settings, string assetFilePath,
            CombinedAssetApplicator assetApplicationMethod)
        {
            Settings = settings;
            AssetFilePath = assetFilePath;
            AssetApplicationMethod = assetApplicationMethod;
        }

        public void Create()
        {
            ComparisonEntry = ComparisonEntries.CreateEntry(Settings, AssetApplicationMethod, AssetFilePath);
        }
        #endregion
    }

    public class Asset
    {
        #region Static Methods
        private static Dictionary<string, ComparisonEntry> ComparisonEntriesByAsset = new Dictionary<string, ComparisonEntry>();

        private static Dictionary<string, MetaAddressables.UserData> ProcessedDataByAssetPath = new Dictionary<string, MetaAddressables.UserData>();

        public static Dictionary<string, Asset> ByComparisonEntryAssetPath = new Dictionary<string, Asset>();

        public static Dictionary<string, Asset> ByProcessedDataAssetPath = new Dictionary<string, Asset>();
        #endregion

        #region Fields
        internal Folder parent;

        private bool isProcessDirty;

        protected HashSet<MetaAddressables.UserData> processedData;

        private bool isCompareDirty;

        protected HashSet<ComparisonEntry> comparisonEntries;
        #endregion

        #region Properties
        public string Id { get; }

        public bool IsEnabled { get; set; }

        public virtual int IdentifiedCount => 1;

        public IRuleProcessor RuleProcessor { get; }

        public ComparisonEntryFactory ComparisonEntryFactory { get; }

        protected bool IsProcessDirty
        {
            get => isProcessDirty;
            set
            {
                isProcessDirty = value;

                if (IsProcessDirty && parent != null)
                {
                    parent.IsProcessDirty = true;
                }
            }
        }

        public virtual IReadOnlyCollection<MetaAddressables.UserData> ProcessedData
        {
            get
            {
                if (IsProcessDirty)
                {
                    processedData.Clear();
                    
                    // Attempt to find whether a entry associated with the asset path already exists in the global cache.
                    bool found = ProcessedDataByAssetPath.TryGetValue(RuleProcessor.AssetFilePath, out MetaAddressables.UserData userData);
                    if (found)
                    {
                        processedData.Add(userData);
                    }

                    IsProcessDirty = false;
                }

                return processedData;
            }
        }

        protected bool IsCompareDirty
        {
            get => isCompareDirty;
            set
            {
                isCompareDirty = value;

                if (IsCompareDirty && parent != null)
                {
                    parent.IsCompareDirty = true;
                }
            }
        }

        public virtual IReadOnlyCollection<ComparisonEntry> ComparisonEntries
        {
            get
            {
                if (IsCompareDirty)
                {
                    comparisonEntries.Clear();

                    // Attempt to find whether a entry associated with the asset path already exists in the global cache.
                    bool found = ComparisonEntriesByAsset.TryGetValue(RuleProcessor.AssetFilePath, out ComparisonEntry comparisonEntry);
                    if (found)
                    {
                        comparisonEntries.Add(comparisonEntry);
                    }

                    IsCompareDirty = false;
                }

                return comparisonEntries;
            }
        }
        #endregion

        #region Methods
        public Asset(Folder parent, string assetGuid, IRuleProcessor ruleProcessor)
        {
            this.parent = parent;
            this.Id = assetGuid;
            this.RuleProcessor = ruleProcessor;
            this.isProcessDirty = true;
            this.IsEnabled = true;
            this.isCompareDirty = true;
            this.processedData = new HashSet<MetaAddressables.UserData>(new UserDataComparer());

            if (ruleProcessor.Rules.AssetApplicationMethod is CombinedAssetApplicator combinedAssetApplicator)
            {
                ComparisonEntryFactory = new ComparisonEntryFactory(ruleProcessor.Settings, ruleProcessor.AssetFilePath, combinedAssetApplicator);

                var comparisonEntryComparer = new PropertyComparer<ComparisonEntry>((x => x.entryName, StringComparer.Ordinal));
                comparisonEntries = new HashSet<ComparisonEntry>(comparisonEntryComparer);
            }
        }

        public virtual void ProcessRules(bool overwrite = false)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (!overwrite)
            {
                // Attempt to find whether a entry associated with the asset path already exists in the global cache.
                bool found = ProcessedDataByAssetPath.TryGetValue(RuleProcessor.AssetFilePath, out MetaAddressables.UserData userData);

                // If there is an entry associated with the asset path, then:
                if (found)
                {
                    // Do nothing else.
                    return;
                }
            }

            // Process the rules to create a new entry.
            RuleProcessor.ProcessRules();

            // Associate the new entry with the asset path.
            ProcessedDataByAssetPath[RuleProcessor.AssetFilePath] = RuleProcessor.ProcessedData;

            ByProcessedDataAssetPath[RuleProcessor.AssetFilePath] = this;

            // Let the parent folders know that they should recache their processed data.
            IsProcessDirty = true;
        }

        public virtual void Compare(bool overwrite = false)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (ComparisonEntryFactory == null)
            {
                return;
            }

            if (!overwrite)
            {
                bool found = ComparisonEntriesByAsset.TryGetValue(ComparisonEntryFactory.AssetFilePath, out ComparisonEntry comparisonEntry);

                if (found)
                {
                    // Do nothing else.
                    return;
                }
            }

            // Attempt to create a new comparison.
            ComparisonEntryFactory.Create();

            ComparisonEntriesByAsset[ComparisonEntryFactory.AssetFilePath] = ComparisonEntryFactory.ComparisonEntry;

            ByComparisonEntryAssetPath[ComparisonEntryFactory.AssetFilePath] = this;

            // Let the parent folders know that they should recache their comparison data.
            IsCompareDirty = true;
        }
        #endregion
    }

    public class Folder : Asset
    {
        #region Fields
        private List<Asset> children;

        private int assetCount;
        #endregion

        #region Properties
        public IAssetIdentifier AssetIdentifier { get; }

        public IReadOnlyList<Asset> Children => children;

        public override int IdentifiedCount => assetCount;

        public override IReadOnlyCollection<MetaAddressables.UserData> ProcessedData
        {
            get
            {
                if (IsProcessDirty)
                {
                    processedData.Clear();

                    foreach (Asset asset in Children)
                    {
                        processedData.UnionWith(asset.ProcessedData);
                    }

                    IsProcessDirty = false;
                }

                return processedData;
            }
        }

        public override IReadOnlyCollection<ComparisonEntry> ComparisonEntries
        {
            get
            {
                if (IsCompareDirty)
                {
                    comparisonEntries.Clear();

                    foreach (Asset asset in Children)
                    {
                        comparisonEntries.UnionWith(asset.ComparisonEntries);
                    }

                    IsCompareDirty = false;
                }

                return comparisonEntries;
            }
        }
        #endregion

        #region Methods
        /// Used for parent folders that only have sub-folders.
        public Folder(string assetId, List<Asset> children) :
            this(assetId, null, null, children)
        {
        }

        // Used for parent folders that only have assets.
        public Folder(string assetId, IAssetIdentifier assetIdentifier, IRuleProcessor ruleProcessor) :
            this(assetId, assetIdentifier, ruleProcessor, new List<Asset>())
        {
        }

        // Used for parent folders that have subfolders and assets.
        public Folder(string assetId, IAssetIdentifier assetIdentifier, IRuleProcessor ruleProcessor, List<Asset> children) :
            base(null, assetId, ruleProcessor)
        {
            this.AssetIdentifier = assetIdentifier;
            this.children = children;

            if (children == null)
            {
                return;
            }

            foreach(var child in children)
            {
                child.parent = this;
            }
        }

        public void Identify()
        {
            if (!IsEnabled)
            {
                return;
            }

            if (AssetIdentifier != null && RuleProcessor != null)
            {
                AssetIdentifier.GetFiles();

                foreach ((string assetGuid, string assetfilePath) in AssetIdentifier.AssetFilePathsByGuids)
                {
                    var ruleProcessor = new RuleProcessor(RuleProcessor.Settings, assetfilePath, RuleProcessor.Rules);
                    children.Add(new Asset(this, assetfilePath, ruleProcessor));
                }
            }

            assetCount = 0;
            foreach (Asset asset in Children)
            {
                if (asset is Folder folder)
                {
                    folder.Identify();
                }

                assetCount += asset.IdentifiedCount;
            }
        }

        public override void ProcessRules(bool overwrite = false)
        {
            if (!IsEnabled)
            {
                return;
            }

            foreach (Asset asset in Children)
            {
                asset.ProcessRules(overwrite);
            }
        }

        public override void Compare(bool overwrite = false)
        {
            if (!IsEnabled)
            {
                return;
            }

            foreach (Asset asset in Children)
            {
                asset.Compare(overwrite);
            }
        }
        #endregion
    }

    public class TreeViewElement
    {
        public bool IsExpanded { get; set; }

        public TreeViewElement(bool isExpanded)
        {
            IsExpanded = isExpanded;
        }
    }

    public class TreeViewElement<T> : TreeViewElement
    {
        public T Value { get; set; }

        public TreeViewElement(bool isExpanded, T value) :
            base(isExpanded)
        {
            Value = value;
        }
    }

    public static class TreeViewExtensions
    {
        #region Static Methods
        public static void AddItem<T>(BaseTreeView treeView, int parentId, TreeViewItemData<T> item, int childIndex = -1, bool rebuildTree = true)
        {
            treeView.AddItem(item, parentId, childIndex, rebuildTree);

            AddItems(treeView, item.id, item.children, childIndex, rebuildTree);
        }

        public static void AddItems<T>(BaseTreeView treeView, int parentId, IEnumerable<TreeViewItemData<T>> items, int childIndex = -1, bool rebuildTree = true)
        {
            foreach (var item in items)
            {
                AddItem(treeView, parentId, item, childIndex, rebuildTree);
            }
        }

        public static void AddUniqueItem<T>(BaseTreeView treeView, int parentId, TreeViewItemData<T> item, int childIndex = -1, bool rebuildTree = true)
        {
            if (treeView.viewController.GetIndexForId(item.id) == -1)
            {
                treeView.AddItem(item, parentId, childIndex, rebuildTree);
            }
        }

        public static void AddUniqueItems<T>(BaseTreeView treeView, int parentId, IEnumerable<TreeViewItemData<T>> items, int childIndex = -1, bool rebuildTree = true)
        {
            var childItems = new List<TreeViewItemData<T>>(items);
            foreach (var item in childItems)
            {
                AddUniqueItem(treeView, parentId, item, childIndex, rebuildTree);
            }
        }

        public static int FindRootItemIdByIndex(BaseTreeView treeView, int index)
        {
            int id = treeView.viewController.GetIdForIndex(index);

            return FindRootItemIdById(treeView, id);
        }

        public static int FindRootItemIdById(BaseTreeView treeView, int id)
        {
            int nextId = treeView.viewController.GetParentId(id);

            if (nextId == -1)
            {
                return id;
            }

            return FindRootItemIdById(treeView, nextId);
        }

        public static void ReplaceItem<T>(BaseTreeView treeView, TreeViewItemData<T> item, int parentId = -1, int childIndex = -1, bool rebuildTree = true)
        {
            ReplaceItem(treeView, item.id, item, parentId, childIndex, rebuildTree);
        }

        public static void ReplaceItem<T>(BaseTreeView treeView, int oldId, TreeViewItemData<T> item, int parentId = -1, int childIndex = -1, bool rebuildTree = true)
        {
            if (childIndex == -1)
            {
                childIndex = treeView.viewController.GetIndexForId(oldId);
            }
            
            bool removed = treeView.TryRemoveItem(oldId, true);
            if (removed)
            {
                AddUniqueItem(treeView, parentId, item, childIndex, rebuildTree);
            }
        }

        public static void ExpandItem<T>(BaseTreeView treeView, TreeViewItemData<T> item, bool shouldRefresh)
            where T : TreeViewElement
        {
            if (item.data.IsExpanded)
            {
                treeView.ExpandItem(item.id, false, shouldRefresh);
            }
            else
            {
                treeView.CollapseItem(item.id, false, shouldRefresh);
            }

            ExpandItems(treeView, item.children, shouldRefresh);
        }

        public static void ExpandItems<T>(BaseTreeView treeView, IEnumerable<TreeViewItemData<T>> items, bool shouldRefresh)
            where T : TreeViewElement
        {
            foreach (var item in items)
            {
                ExpandItem(treeView, item, shouldRefresh);
            }
        }
        #endregion
    }
}
