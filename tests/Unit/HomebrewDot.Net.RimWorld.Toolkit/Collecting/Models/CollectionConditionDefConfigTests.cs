using System;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Collecting.Models
{
    [Trait("Category", "Unit")]
    public class CollectionConditionDefConfigTests
    {
        [Fact]
        public void CopyConstructor_WithPopulatedConfig_CopiesAllFields()
        {
            var original = new CollectionConditionDefConfig
            {
                Name = "Snipers",
                IsOr = true,
                Inverted = true,
                By = "weapon",
            };

            var copy = new CollectionConditionDefConfig(original);

            Assert.Equal(original.Name, copy.Name);
            Assert.Equal(original.IsOr, copy.IsOr);
            Assert.Equal(original.Inverted, copy.Inverted);
            Assert.Equal(original.By, copy.By);
        }

        [Fact]
        public void CopyConstructor_WithNullOther_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CollectionConditionDefConfig(null));
        }

        [Fact]
        public void CopyConstructor_ModifyingCopy_DoesNotAffectOriginal()
        {
            var original = new CollectionConditionDefConfig { Name = "Snipers", Inverted = true };
            var copy = new CollectionConditionDefConfig(original);

            copy.Name = "ShortRange";
            copy.Inverted = false;

            Assert.Equal("Snipers", original.Name);
            Assert.True(original.Inverted);
        }
    }
}
