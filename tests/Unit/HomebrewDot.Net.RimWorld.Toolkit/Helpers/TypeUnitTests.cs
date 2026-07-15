using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Helpers
{
    public class TypeUnitTests
    {
        #region TryGetType
        [Theory()]
        [InlineData("System.String")]
        [InlineData(nameof(CompProperties_Explosive))]
        [InlineData(nameof(Def))]
        [InlineData("Verse.ThingDef")]
        [InlineData("RimWorld.CompProperties_AbilityAnimalRoar")]
        public void TryGetType_WithValidTypeName_ReturnsType(string typeName)
        {
            // Arrange

            // Act
            Type result = HomebrewDot.Net.Rimworld.Toolkit.Helpers.TryGetType(typeName);

            // Assert
            Assert.NotNull(result);
        }
        #endregion
    }
}
