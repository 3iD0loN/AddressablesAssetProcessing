using System.Collections.Generic;

namespace USP.AddressablesAssetProcessing
{
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
}
