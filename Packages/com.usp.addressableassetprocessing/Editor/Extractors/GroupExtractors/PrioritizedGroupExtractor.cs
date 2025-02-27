using System;
using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;
using UnityEngine.UI;

namespace USP.AddressablesAssetProcessing
{
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using USP.MetaAddressables;

    public class PrioritizedGroupExtractor : 
        MappedGroupExtractor<HashSet<MetaAddressables.GroupData>, int>
    {
        #region Methods
        public PrioritizedGroupExtractor()
        {
            // Uses a sorted dictionary to propritize the group templates.
            GroupTemplateByKey = new SortedDictionary<int, AddressableAssetGroupTemplate>();
        }

        protected override int GetInternalKey(HashSet<MetaAddressables.GroupData> groups)
        {
            // For every group template in the order of priority, perform the following: 
            foreach (var pair in GroupTemplateByKey)
            {
                // Create an instance of a group based on the template.
                var groupData = new MetaAddressables.GroupData(pair.Value);

                // If the set contains a matching group, then:
                if (groups.Contains(groupData))
                {
                    // Use this group template.
                    return pair.Key;
                }

                // Otherwise, the group does not contain a group created from the template.
            }

            // All items have been exhausted and no match has been found.

            throw new InvalidOperationException();
        }
        #endregion
    }
}