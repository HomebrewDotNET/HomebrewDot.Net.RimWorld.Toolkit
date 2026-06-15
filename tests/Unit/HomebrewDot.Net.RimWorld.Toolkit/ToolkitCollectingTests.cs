using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests
{
    public class ToolkitCollectingTests
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyContext = new Dictionary<string, object>();

        public ToolkitCollectingTests()
        {
            ClearCollections();
        }

        [Fact]
        public void Set_WithDefinition_AddsDefinition()
        {
            var definition = new CollectionDef();

            Toolkit.Collecting.Set("alpha", definition);

            var definitions = Toolkit.Collecting.GetAllDefinitions();
            Assert.True(definitions.ContainsKey("alpha"));
            Assert.Same(definition, definitions["alpha"]);
        }

        [Fact]
        public void Set_WithCollector_AddsCollectorAndDefinition()
        {
            var collector = new TrackingCollector(new CollectionDef());

            Toolkit.Collecting.Set("beta", collector);

            var collectors = Toolkit.Collecting.GetAllCollectors();
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            Assert.True(collectors.ContainsKey("beta"));
            Assert.Same(collector, collectors["beta"]);
            Assert.True(definitions.ContainsKey("beta"));
            Assert.Same(collector.Definition, definitions["beta"]);
        }

        [Fact]
        public void SetGeneric_WithCollector_AddsCollectorAndDefinition()
        {
            var collector = new TrackingCollector<string>(new CollectionDef());

            Toolkit.Collecting.Set("gamma", collector);

            var collectors = Toolkit.Collecting.GetAllCollectors();
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            Assert.True(collectors.ContainsKey("gamma"));
            Assert.Same(collector, collectors["gamma"]);
            Assert.True(definitions.ContainsKey("gamma"));
            Assert.Same(collector.Definition, definitions["gamma"]);
        }

        [Fact]
        public void Remove_WithCollector_StopsAndDisposesCollectorAndRemovesEntries()
        {
            var collector = new TrackingCollector(new CollectionDef());
            Toolkit.Collecting.Set("delta", collector, false);

            Toolkit.Collecting.Remove("delta");

            var collectors = Toolkit.Collecting.GetAllCollectors();
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            Assert.False(collectors.ContainsKey("delta"));
            Assert.False(definitions.ContainsKey("delta"));
            Assert.Equal(1, collector.StopCount);
            Assert.True(collector.IsDisposed);
        }

        [Fact]
        public void Build_WithoutCollectorFactory_AddsDefinitionOnly()
        {
            Toolkit.Collecting.Build("epsilon", b => b);

            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var collectors = Toolkit.Collecting.GetAllCollectors();
            Assert.True(definitions.ContainsKey("epsilon"));
            Assert.False(collectors.ContainsKey("epsilon"));
        }

        [Fact]
        public void Build_WithCollectorFactory_AddsCollectorAndDefinition()
        {
            Toolkit.Collecting.Build("zeta", b => b.CollectWith(def => new TrackingCollector(def)));

            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var collectors = Toolkit.Collecting.GetAllCollectors();
            Assert.True(definitions.ContainsKey("zeta"));
            Assert.True(collectors.ContainsKey("zeta"));
        }

        [Fact]
        public void StartCollection_WithCollectors_RestartsAllWithComparatorAndDefinitions()
        {
            var collectorA = new TrackingCollector(new CollectionDef());
            var collectorB = new TrackingCollector(new CollectionDef());
            Toolkit.Collecting.Set("A", collectorA, false);
            Toolkit.Collecting.Set("B", collectorB, false);

            var comparator = new TrackingComparator();
            Toolkit.Collecting.Comparator = comparator;

            Toolkit.Collecting.StartCollection();

            Assert.Equal(1, collectorA.StopCount);
            Assert.Equal(1, collectorB.StopCount);
            Assert.Equal(1, collectorA.StartCount);
            Assert.Equal(1, collectorB.StartCount);
            Assert.Same(comparator, collectorA.LastComparator);
            Assert.Same(comparator, collectorB.LastComparator);
            Assert.NotNull(collectorA.LastDefinitions);
            Assert.True(collectorA.LastDefinitions.ContainsKey("A"));
            Assert.True(collectorA.LastDefinitions.ContainsKey("B"));
        }

        [Fact]
        public void Comparator_Setter_ReplacesAndDisposesOldComparator()
        {
            var first = new DisposableTrackingComparator();
            var second = new DisposableTrackingComparator();

            Toolkit.Collecting.Comparator = first;
            Toolkit.Collecting.Comparator = second;

            Assert.True(first.IsDisposed);
            Assert.False(second.IsDisposed);
            Assert.Same(second, Toolkit.Collecting.Comparator);
        }

        [Fact]
        public void GetAllDefinitions_ReturnsCopy()
        {
            Toolkit.Collecting.Set("eta", new CollectionDef());
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            var mutable = (Dictionary<string, ICollectionDef>)definitions;
            mutable.Clear();

            var current = Toolkit.Collecting.GetAllDefinitions();
            Assert.True(current.ContainsKey("eta"));
        }

        [Fact]
        public void GetAllCollectors_ReturnsCopy()
        {
            Toolkit.Collecting.Set("theta", new TrackingCollector(new CollectionDef()));
            var collectors = Toolkit.Collecting.GetAllCollectors();

            var mutable = (Dictionary<string, ICollector>)collectors;
            mutable.Clear();

            var current = Toolkit.Collecting.GetAllCollectors();
            Assert.True(current.ContainsKey("theta"));
        }

        private static void ClearCollections()
        {
            var allKeys = Toolkit.Collecting.GetAllDefinitions().Keys
                .Concat(Toolkit.Collecting.GetAllCollectors().Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (var i = 0; i < allKeys.Length; i++)
            {
                Toolkit.Collecting.Remove(allKeys[i]);
            }
        }

        private sealed class TrackingComparator : ICollectionComparator
        {
            public bool Matches(ICollectionDef collection, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
                => true;

            public IEnumerable<(object Object, bool Matches)> Matches(ICollectionDef collection, IEnumerable<object> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class DisposableTrackingComparator : ICollectionComparator, IDisposable
        {
            public bool IsDisposed { get; private set; }

            public bool Matches(ICollectionDef collection, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
                => true;

            public void Dispose()
            {
                IsDisposed = true;
            }

            public IEnumerable<(object Object, bool Matches)> Matches(ICollectionDef collection, IEnumerable<object> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
            {
                throw new NotImplementedException();
            }
        }

        private class TrackingCollector : ICollector, IDisposable
        {
            private readonly List<object> _items = new List<object>();

            public TrackingCollector(ICollectionDef definition)
            {
                Definition = definition;
            }

            public ICollectionDef Definition { get; }
            public int Count => _items.Count;
            public int StartCount { get; private set; }
            public int StopCount { get; private set; }
            public bool IsDisposed { get; private set; }
            public ICollectionComparator LastComparator { get; private set; }
            public IReadOnlyDictionary<string, ICollectionDef> LastDefinitions { get; private set; }

            public void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections)
            {
                StartCount++;
                LastComparator = comparer;
                LastDefinitions = collections;
            }

            public void StopCollecting()
            {
                StopCount++;
            }

            public void Clear()
            {
                _items.Clear();
            }

            public IReadOnlyCollection<object> GetAll() => _items;

            public IEnumerator GetEnumerator() => _items.GetEnumerator();

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private sealed class TrackingCollector<T> : TrackingCollector, ICollector<T> where T : class
        {
            private readonly List<T> _typedItems = new List<T>();

            public event Action<T> OnCollected;
            public event Action<T> OnRemoved;
            public event Action<IReadOnlyCollection<T>> OnClear;

            public TrackingCollector(ICollectionDef definition) : base(definition)
            {
            }

            public bool Collect(T obj, IReadOnlyDictionary<string, object> context)
            {
                if (obj == null)
                {
                    return false;
                }

                _typedItems.Add(obj);
                return true;
            }

            public new IReadOnlyCollection<T> GetAll() => _typedItems;

            public bool Contains(T obj) => obj != null && _typedItems.Contains(obj);

            public bool CanCollect(T obj, IReadOnlyDictionary<string, object> context) => obj != null;

            public bool Remove(T obj)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<(T Obj, bool Collected)> Collect(IEnumerable<T> objects, IReadOnlyDictionary<string, object> context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
