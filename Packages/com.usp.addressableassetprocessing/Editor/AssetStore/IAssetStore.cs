using System.Collections.Generic;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public interface IAssetStore
    {
        #region Properties
        /// <summary>
        /// A value indicating whether the source of the assets can be written to or not.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// The Addressables data associated with the asset file path.
        /// </summary>
        IReadOnlyDictionary<string, MetaAddressables.UserData> DataByAssetPath { get; }

        /// <summary>
        /// The global set of labels.
        /// </summary>
        IReadOnlyCollection<string> GlobalLabels { get; }
        #endregion

        #region Methods
        void AddGlobalLabels(HashSet<string> labels);
        #endregion
    }
}