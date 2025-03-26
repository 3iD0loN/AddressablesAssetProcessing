using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

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
}
