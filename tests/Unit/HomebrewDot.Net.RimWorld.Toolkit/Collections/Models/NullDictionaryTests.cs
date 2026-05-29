using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.RimWorld.Generic.Models;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Collections.Models
{
    public class NullDictionaryTests
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        [Fact]
        public void Instance_ReturnsSameObjectEveryTime()
        {
            var a = NullDictionary<string, int>.Instance;
            var b = NullDictionary<string, int>.Instance;

            Assert.Same(a, b);
        }

        // ── Count / IsReadOnly ────────────────────────────────────────────────

        [Fact]
        public void Count_ReturnsZero()
        {
            Assert.Empty(NullDictionary<string, int>.Instance);
        }

        [Fact]
        public void IsReadOnly_ReturnsTrue()
        {
            Assert.True(NullDictionary<string, int>.Instance.IsReadOnly);
        }

        // ── Keys / Values ─────────────────────────────────────────────────────

        [Fact]
        public void Keys_ReturnsEmptyCollection()
        {
            Assert.Empty(NullDictionary<string, int>.Instance.Keys);
        }

        [Fact]
        public void Values_ReturnsEmptyCollection()
        {
            Assert.Empty(NullDictionary<string, int>.Instance.Values);
        }

        // ── Indexer ───────────────────────────────────────────────────────────

        [Fact]
        public void Indexer_Get_ReturnsDefaultValue()
        {
            var result = NullDictionary<string, int>.Instance["anyKey"];

            Assert.Equal(default(int), result);
        }

        [Fact]
        public void Indexer_Get_ReferenceType_ReturnsNull()
        {
            var result = NullDictionary<string, object>.Instance["anyKey"];

            Assert.Null(result);
        }

        [Fact]
        public void Indexer_Set_DoesNothing()
        {
            NullDictionary<string, int>.Instance["anyKey"] = 42;

            Assert.Empty(NullDictionary<string, int>.Instance);
        }

        // ── ContainsKey / TryGetValue ─────────────────────────────────────────

        [Fact]
        public void ContainsKey_AlwaysReturnsFalse()
        {
            Assert.False(NullDictionary<string, int>.Instance.ContainsKey("key"));
        }

        [Fact]
        public void TryGetValue_ReturnsFalseAndDefaultValue()
        {
            var result = NullDictionary<string, int>.Instance.TryGetValue("key", out var value);

            Assert.False(result);
            Assert.Equal(default(int), value);
        }

        // ── Add / Remove / Clear ─────────────────────────────────────────────

        [Fact]
        public void Add_KeyValue_DoesNothing()
        {
            NullDictionary<string, int>.Instance.Add("key", 1);
            Assert.Empty(NullDictionary<string, int>.Instance);
        }

        [Fact]
        public void Add_KeyValuePair_DoesNothing()
        {
            NullDictionary<string, int>.Instance.Add(new KeyValuePair<string, int>("key", 1));
            Assert.Empty(NullDictionary<string, int>.Instance);
        }

        [Fact]
        public void Remove_ByKey_ReturnsFalse()
        {
            var result = NullDictionary<string, int>.Instance.Remove("key");

            Assert.False(result);
        }

        [Fact]
        public void Remove_ByKeyValuePair_ReturnsFalse()
        {
            var result = NullDictionary<string, int>.Instance.Remove(new KeyValuePair<string, int>("key", 1));

            Assert.False(result);
        }

        [Fact]
        public void Clear_DoesNothing()
        {
            NullDictionary<string, int>.Instance.Clear();
            Assert.Empty(NullDictionary<string, int>.Instance);
        }

        // ── Contains ─────────────────────────────────────────────────────────

        [Fact]
        public void Contains_AlwaysReturnsFalse()
        {
            var result = NullDictionary<string, int>.Instance.Contains(new KeyValuePair<string, int>("key", 1));

            Assert.False(result);
        }

        // ── GetEnumerator ─────────────────────────────────────────────────────

        [Fact]
        public void GetEnumerator_Generic_YieldsNoElements()
        {
            var items = NullDictionary<string, int>.Instance.ToList();

            Assert.Empty(items);
        }

        [Fact]
        public void GetEnumerator_NonGeneric_YieldsNoElements()
        {
            var dict = (System.Collections.IEnumerable)NullDictionary<string, int>.Instance;
            var count = 0;
            foreach (var _ in dict) count++;

            Assert.Equal(0, count); // using int comparison, not collection size check
        }

        // ── CopyTo ────────────────────────────────────────────────────────────

        [Fact]
        public void CopyTo_DoesNotThrow()
        {
            var arr = new KeyValuePair<string, int>[5];
            NullDictionary<string, int>.Instance.CopyTo(arr, 0);
        }

        // ── IReadOnlyDictionary interface ─────────────────────────────────────

        [Fact]
        public void IReadOnlyDictionary_Keys_ReturnsEmpty()
        {
            var roDict = (IReadOnlyDictionary<string, int>)NullDictionary<string, int>.Instance;
            Assert.Empty(roDict.Keys);
        }

        [Fact]
        public void IReadOnlyDictionary_Values_ReturnsEmpty()
        {
            var roDict = (IReadOnlyDictionary<string, int>)NullDictionary<string, int>.Instance;
            Assert.Empty(roDict.Values);
        }
    }
}
