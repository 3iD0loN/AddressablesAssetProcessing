using System.Collections.Generic;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class AssetStore
    {
        #region Fields
        public Dictionary<string, MetaAddressables.UserData> DataByAssetPath { get; }  = new Dictionary<string, MetaAddressables.UserData>();

        public HashSet<string> GlobalLabels { get; }  = new HashSet<string>();
        #endregion

        #region Methods
        public virtual void AddGlobalLabels(HashSet<string> labels)
        {
            AddGlobalLabels(labels as IEnumerable<string>);
        }

        protected void AddGlobalLabels(IEnumerable<string> labels)
        {
            GlobalLabels.UnionWith(labels);
        }
        #endregion
    }
}