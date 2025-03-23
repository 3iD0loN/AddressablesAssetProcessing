using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using DocumentFormat.OpenXml.Presentation;
    using GluonGui.Dialog;
    using UnityEngine.WSA;
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
        #region Fields
        protected HashSet<MetaAddressables.UserData> processedData;

        protected HashSet<ComparisonEntry> comparisonEntries;
        #endregion

        #region Properties
        public string Id { get; }

        public bool IsEnabled { get; set; }

        public IRuleProcessor RuleProcessor { get; }

        public ComparisonEntryFactory ComparisonEntryFactory { get; }

        public IReadOnlyCollection<MetaAddressables.UserData> ProcessedData => processedData;

        public virtual int IdentifiedCount => 1;

        public IReadOnlyCollection<ComparisonEntry> ComparisonEntries => comparisonEntries;
        #endregion

        #region Methods
        public Asset(string assetGuid, IRuleProcessor ruleProcessor)
        {
            Id = assetGuid;
            RuleProcessor = ruleProcessor;
            IsEnabled = true;
            processedData = new HashSet<MetaAddressables.UserData>(new UserDataComparer());

            if (ruleProcessor.Rules.AssetApplicationMethod is CombinedAssetApplicator combinedAssetApplicator)
            {
                ComparisonEntryFactory = new ComparisonEntryFactory(ruleProcessor.Settings, ruleProcessor.AssetFilePath, combinedAssetApplicator);

                var comparisonEntryComparer = new PropertyComparer<ComparisonEntry>((x => x.entryName, StringComparer.Ordinal));
                comparisonEntries = new HashSet<ComparisonEntry>(comparisonEntryComparer);
            }
        }

        public virtual void ProcessRules()
        {
            if (!IsEnabled)
            {
                return;
            }

            RuleProcessor.ProcessRules();

            bool found = processedData.TryGetValue(RuleProcessor.ProcessedData,
                out MetaAddressables.UserData userData);

            if (!found)
            {
                userData = RuleProcessor.ProcessedData;

                processedData.Add(userData);
            }
        }

        public virtual void Compare()
        {
            if (!IsEnabled)
            {
                return;
            }

            ComparisonEntryFactory?.Create();
            
            if (comparisonEntries == null)
            {
                return;
            }

            var comparisonEntry = ComparisonEntryFactory?.ComparisonEntry;

            bool found = comparisonEntries.Contains(comparisonEntry);

            if (!found)
            {
                comparisonEntries.Add(comparisonEntry);
            }
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
            base(assetId, ruleProcessor)
        {
            this.AssetIdentifier = assetIdentifier;
            this.children = children;
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
                    children.Add(new Asset(assetfilePath, ruleProcessor));
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

        public override void ProcessRules()
        {
            if (!IsEnabled)
            {
                return;
            }

            foreach (Asset asset in Children)
            {
                asset.ProcessRules();

                processedData.UnionWith(asset.ProcessedData);
            }
        }

        public override void Compare()
        {
            if (!IsEnabled)
            {
                return;
            }

            foreach (Asset asset in Children)
            {
                asset.Compare();

                comparisonEntries.UnionWith(asset.ComparisonEntries);
            }
        }
        #endregion
    }
}
