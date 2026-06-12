using System;
using HomebrewDot.Net.Rimworld.Extensions;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Extensions
{
    public class TypeExtensionsTests
    {
        #region Test hierarchy

        private class Base { }
        private class Child : Base { }
        private class Grandchild : Child { }
        private class Unrelated { }

        #endregion

        // ── Null argument guards ──────────────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ((Type)null).GetInheritanceDistance(typeof(Base)));
        }

        [Fact]
        public void GetInheritanceDistance_NullBaseType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                typeof(Child).GetInheritanceDistance(null));
        }

        // ── Same type ─────────────────────────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_SameType_ReturnsZero()
        {
            var result = typeof(Base).GetInheritanceDistance(typeof(Base));

            Assert.Equal(0, result);
        }

        // ── Direct subclass ───────────────────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_DirectSubclass_ReturnsOne()
        {
            var result = typeof(Child).GetInheritanceDistance(typeof(Base));

            Assert.Equal(1, result);
        }

        // ── Two levels deep ───────────────────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_TwoLevels_ReturnsTwo()
        {
            var result = typeof(Grandchild).GetInheritanceDistance(typeof(Base));

            Assert.Equal(2, result);
        }

        // ── Grandchild to immediate parent ────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_GrandchildToDirectParent_ReturnsOne()
        {
            var result = typeof(Grandchild).GetInheritanceDistance(typeof(Child));

            Assert.Equal(1, result);
        }

        // ── Unrelated types ───────────────────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_UnrelatedTypes_ReturnsNegativeOne()
        {
            var result = typeof(Unrelated).GetInheritanceDistance(typeof(Base));

            Assert.Equal(-1, result);
        }

        // ── System types ──────────────────────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_ObjectAsBaseType_ReturnsCorrectDepth()
        {
            // Child → Base → object = distance 2 from Child to object
            var result = typeof(Child).GetInheritanceDistance(typeof(object));

            Assert.Equal(2, result);
        }

        [Fact]
        public void GetInheritanceDistance_TypeToItself_WorksForSystemTypes()
        {
            var result = typeof(string).GetInheritanceDistance(typeof(string));

            Assert.Equal(0, result);
        }

        // ── Base searching up ─────────────────────────────────────────────────

        [Fact]
        public void GetInheritanceDistance_BaseToChild_ReturnsNegativeOne()
        {
            // Base does not inherit from Child
            var result = typeof(Base).GetInheritanceDistance(typeof(Child));

            Assert.Equal(-1, result);
        }
    }
}
