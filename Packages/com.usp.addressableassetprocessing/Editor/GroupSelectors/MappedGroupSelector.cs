using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public abstract class MappedGroupSelector<I, O> : IGroupSelector<I>
    {
        #region Properties
        public IDictionary<O, AddressableAssetGroupTemplate> GroupTemplateByKey
        {
            get;
            set;
        }
        #endregion

        #region Methods
        public MappedGroupSelector()
        {
            GroupTemplateByKey = new Dictionary<O, AddressableAssetGroupTemplate>();
        }

        public void Apply(I groups)
        {
            O key = Select(groups);

            // Get template associated with the key.
            AddressableAssetGroupTemplate groupTemplate = Get(key);

            MetaAddressables.factory.ActiveGroupTemplate = groupTemplate;
        }

        protected abstract O Select(I groups);

        public AddressableAssetGroupTemplate Get(O key)
        {
            bool found = GroupTemplateByKey.TryGetValue(key,
                    out AddressableAssetGroupTemplate result);

            if (found)
            {
                return result;
            }

            return default;
        }

        public void Set(O key, AddressableAssetGroupTemplate value)
        {
            GroupTemplateByKey.Add(key, value);
        }
        #endregion
    }
}