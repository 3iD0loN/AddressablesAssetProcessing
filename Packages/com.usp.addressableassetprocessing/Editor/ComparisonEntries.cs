using System;
using System.Collections.Generic;

namespace USP.AddressablesAssetProcessing
{
    using DocumentFormat.OpenXml.Office2013.PowerPoint;
    using NUnit.Framework;
    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Security.Policy;
    using UnityEditor;
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;
    using static PlasticGui.LaunchDiffParameters;
#endif

    public class CompareOperand
    {
        public object value;

        public bool isReadonly;

        public CompareOperand(object value, bool isReadonly = false)
        {
            this.value = value;
            this.isReadonly = isReadonly;
        }
    }

    public struct CompareOperation
    {
        public IPropertyComparer comparer { get; }

        public CompareOperand leftHand { get; }

        public CompareOperand rightHand { get; }

        public bool result { get; }

        public CompareOperation(IPropertyComparer comparer, CompareOperand leftHand, CompareOperand rightHand)
        {
            this.comparer = comparer;
            this.leftHand = leftHand;
            this.rightHand = rightHand;
            result = false;
            this.result = Operate(this);
        }

        public static bool Operate(CompareOperation operation)
        {
            var leftHash = operation.leftHand.value != null ? operation.comparer.GetHashCode(operation.leftHand.value) : 0;
            var rightHash = operation.rightHand.value != null ? operation.comparer.GetHashCode(operation.rightHand.value) : 0;

            return leftHash == rightHash;
        }
    }

    public class ComparisonEntry
    {
        public string entryName;

        public Type entryType;

        public IPropertyComparer comparer;

        public IReadOnlyDictionary<string, CompareOperand> compareTargets;

        public IReadOnlyDictionary<string, CompareOperation> compareOperations;

        public IEnumerable<ComparisonEntry> children;
    }

    public class ComparisonEntries
    {
        private const string ProcessingDataKey = "processing-data";

        private const string MetafileDataKey = "metafile-data";

        private const string AddressablesDataKey = "addressables-data";

        private static UserDataComparer userDataComparer = new UserDataComparer();

        public static ComparisonEntry CreateEntry(CombinedAssetApplicator combinedAssetApplicator, string assetFilePath)
        {
            var properties = new Dictionary<string, CompareOperand>(3);
            
            combinedAssetApplicator.SimulatedAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData processingData);
            properties[ProcessingDataKey] = new CompareOperand(processingData, combinedAssetApplicator.SimulatedAssetStore.IsReadOnly);

            combinedAssetApplicator.MetaAddressablesAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData metafileData);
            properties[MetafileDataKey] = new CompareOperand(metafileData, combinedAssetApplicator.MetaAddressablesAssetStore.IsReadOnly);

            combinedAssetApplicator.AddressablesAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData addressablesData);
            properties[AddressablesDataKey] = new CompareOperand(addressablesData, combinedAssetApplicator.AddressablesAssetStore.IsReadOnly);

            var result = new ComparisonEntry();
            result.comparer = userDataComparer;
            result.entryType = typeof(MetaAddressables.UserData);
            result.entryName = assetFilePath;
            result.compareTargets = properties;
            result.compareOperations = PopulateCompare(result.comparer, result.compareTargets);
            result.children = PopulateChildren(result.entryType, result.comparer, result.compareTargets);

            return result;
        }

        private static IReadOnlyDictionary<string, CompareOperation> PopulateCompare(
            IPropertyComparer comparer, IReadOnlyDictionary<string, CompareOperand> compareTargets)
        {
            var processingData = compareTargets[ProcessingDataKey];
            var metafileData = compareTargets[MetafileDataKey];
            var addressablesData = compareTargets[AddressablesDataKey];

            var result = new Dictionary<string, CompareOperation>(2);
            result["processing-metafile-compare"] = new CompareOperation(comparer, processingData, metafileData);
            result["metafile-addressables-compare"] = new CompareOperation(comparer, metafileData, addressablesData);

            return result;
        }

        private static IEnumerable<ComparisonEntry> PopulateChildren(
            Type type, IPropertyComparer comparer,
            IReadOnlyDictionary<string, CompareOperand> compareTargets)
        {
            var comparerChildren = comparer.Children;

            if (comparerChildren == null)
            {
                if (comparer is GroupSchemaDataComparer abstractComparer)
                {
                    comparer = GroupSchemaDataComparer.GetComparer(type);
                    comparerChildren = comparer.Children;
                }
                else if (comparer is EnumerableComparer enumerableComparer)
                {
                    // Get the comparer for the elements in the container.
                    // It is now the comparer for the children, which will be the elements in the container.
                    comparer = enumerableComparer.ItemComparer;

                    var propertyComparerByHash = new Dictionary<object, PropertyComparerPair>();

                    var x = new HashSet<object[]>(new EnumerableComparer(ObjectComparer.Default));

                    // For every object that will be compared, perform the following:
                    foreach ((string key, CompareOperand operand) in compareTargets)
                    {
                        // The comparer is an enumerable comparer, so we assume that the values in the operands are enumerable.
                        var enumerable = operand.value as IEnumerable;

                        if (enumerable == null)
                        {
                            continue;
                        }

                        IEnumerator enumerator = enumerable.GetEnumerator();

                        // For every element in the container, perform the following:
                        while (enumerator.MoveNext())
                        {
                            object value = enumerator.Current;

                            x.Add(Get(value, enumerable));

                            bool found = propertyComparerByHash.TryGetValue(value, out PropertyComparerPair pair);

                            if (!found)
                            {
                                Expression<Func<IEnumerable, object>> z = (IEnumerable y) => GetValueOrDefault(x, value, enumerable); 
                                pair = new PropertyComparerPair(z, comparer);
                                 
                                propertyComparerByHash.Add(value, pair);
                            }
                        }
                    }

                    comparerChildren = propertyComparerByHash.Values;
                }
                else
                {
                    return null;
                }
            }

            var children = new List<ComparisonEntry>();
            int i = 0;
            foreach (PropertyComparerPair comparerChild in comparerChildren)
            {
                var properties = new Dictionary<string, CompareOperand>(compareTargets.Count);
                foreach ((string key, CompareOperand parentOperand) in compareTargets)
                {
                    if (parentOperand == null)
                    {
                        continue;
                    }

                    object childValue = (parentOperand.value != null) ? comparerChild.Access(parentOperand.value) : null;

                    var operand = new CompareOperand(childValue, parentOperand.isReadonly);
                    properties.Add(key, operand);
                }

                // The first item should exist along with at least one other to compare against.
                // It should be available to compare against all other items, which are alike enough to compare.
                // Therefore, there is at least one and it would be reasonable have the same type.
                CompareOperand firstOperand = properties.First().Value;
                object firstValue = firstOperand.value;

                var propertyInfo = comparerChild.GetMemberInfo<PropertyInfo>();
                var methodInfo = comparerChild.GetMethodInfo();

                // If the first value is valid, then use the concrete type, since a property, field, or method
                // can obscure the type of an inherited item. Otherwise, fallback to using the property type.  
                Type childType = (firstValue != null) ? firstValue.GetType() : (methodInfo != null) ? methodInfo.ReturnType : default;

                var childEntry = new ComparisonEntry();
                childEntry.entryName = propertyInfo != null ? propertyInfo.Name : $"Element {i}";
                childEntry.entryType = childType;
                childEntry.comparer = comparerChild.PropertyComparer;
                childEntry.compareTargets = properties;
                childEntry.compareOperations = PopulateCompare(childEntry.comparer, childEntry.compareTargets);
                childEntry.children = PopulateChildren(childEntry.entryType, childEntry.comparer, childEntry.compareTargets);

                children.Add(childEntry);
                i++;
            }

            return children;
        }

        private static object[] Get(object value, IEnumerable enumerable)
        {
            return new object[] { value, enumerable };
        }

        private static object GetValueOrDefault(ISet<object[]> x, object value, IEnumerable enumerable)
        {
            return x.Contains(Get(value, enumerable)) ? value : null;
        }

        private static object GetValueOrDefault(IReadOnlyDictionary<object[], object> x, object value, IEnumerable enumerable)
        {
            var key = Get(value, enumerable);

            x.TryGetValue(key, out object v);

            return v;
        }
    }
}
