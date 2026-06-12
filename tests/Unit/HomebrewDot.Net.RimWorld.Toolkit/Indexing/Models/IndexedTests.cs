using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Models
{
    public class IndexedTests
    {
        #region Test helpers

        private enum TestKind
        {
            Unknown = 0,
            Alpha = 1,
            Beta = 2
        }

        private class PrimitiveSubject
        {
            public bool IsEnabled { get; set; }
            public long CountLong { get; set; }
            public double Ratio { get; set; }
            public decimal Price { get; set; }
            public char Grade { get; set; }
            public DateTime CreatedOn { get; set; }
            public Guid UniqueId { get; set; }
            public TestKind Kind { get; set; }
            public int? OptionalInt { get; set; }

            public float PublicFloatField;
            public byte PublicByteField;
        }

        private class TestSubject
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string InternalField;

            private string _hidden { get; set; } = "hidden";
        }

        private static readonly IReadOnlyDictionary<string, object> EmptyMetadata =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Constructor

        [Fact]
        public void Constructor_WithNullValue_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Indexed<TestSubject>(null, EmptyMetadata));
        }

        [Fact]
        public void Constructor_WithNullMetadata_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Indexed<TestSubject>(new TestSubject(), null));
        }

        [Fact]
        public void Constructor_WithValidArgs_SetsValue()
        {
            // Arrange
            var subject = new TestSubject { Name = "Alice" };

            // Act
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Assert
            Assert.Same(subject, indexed.Value);
        }

        [Fact]
        public void Constructor_WithValidArgs_SetsMetadata()
        {
            // Arrange
            var subject = new TestSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Key"] = "value" };

            // Act
            var indexed = new Indexed<TestSubject>(subject, metadata);

            // Assert
            Assert.Same(metadata, indexed.Metadata);
        }

        #endregion

        #region GetValue - primitive/scalar property and field coverage

        [Fact]
        public void GetValue_WithExistingBoolProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { IsEnabled = true };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            bool result = indexed.GetValue<bool>("IsEnabled");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetValue_WithExistingBoolProperty_ReturnsPropertyValueAsObject()
        {
            // Arrange
            var subject = new PrimitiveSubject { IsEnabled = true };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            object result = indexed.GetValue<object>("IsEnabled");

            // Assert
            Assert.True((bool)result);
        }

        [Fact]
        public void GetValue_WithExistingLongProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { CountLong = 922337203685477000L };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            long result = indexed.GetValue<long>("CountLong");

            // Assert
            Assert.Equal(922337203685477000L, result);
        }

        [Fact]
        public void GetValue_WithExistingDoubleProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { Ratio = 3.14159d };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            double result = indexed.GetValue<double>("Ratio");

            // Assert
            Assert.Equal(3.14159d, result, 5);
        }

        [Fact]
        public void GetValue_WithExistingDecimalProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { Price = 1234.56m };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            decimal result = indexed.GetValue<decimal>("Price");

            // Assert
            Assert.Equal(1234.56m, result);
        }

        [Fact]
        public void GetValue_WithExistingCharProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { Grade = 'A' };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            char result = indexed.GetValue<char>("Grade");

            // Assert
            Assert.Equal('A', result);
        }

        [Fact]
        public void GetValue_WithExistingDateTimeProperty_ReturnsPropertyValue()
        {
            // Arrange
            DateTime expected = new DateTime(2026, 05, 08, 10, 11, 12, DateTimeKind.Utc);
            var subject = new PrimitiveSubject { CreatedOn = expected };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            DateTime result = indexed.GetValue<DateTime>("CreatedOn");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetValue_WithExistingGuidProperty_ReturnsPropertyValue()
        {
            // Arrange
            Guid expected = Guid.NewGuid();
            var subject = new PrimitiveSubject { UniqueId = expected };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            Guid result = indexed.GetValue<Guid>("UniqueId");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetValue_WithExistingEnumProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { Kind = TestKind.Beta };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            TestKind result = indexed.GetValue<TestKind>("Kind");

            // Assert
            Assert.Equal(TestKind.Beta, result);
        }

        [Fact]
        public void GetValue_WithExistingNullableIntPropertyWithValue_ReturnsValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { OptionalInt = 42 };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            int? result = indexed.GetValue<int?>("OptionalInt");

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void GetValue_WithExistingNullableIntPropertyWithNull_ReturnsNull()
        {
            // Arrange
            var subject = new PrimitiveSubject { OptionalInt = null };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            int? result = indexed.GetValue<int?>("OptionalInt");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetValue_WithExistingFloatField_ReturnsFieldValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { PublicFloatField = 1.25f };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            float result = indexed.GetValue<float>("PublicFloatField");

            // Assert
            Assert.True(Math.Abs(result - 1.25f) < 0.001f);
        }

        [Fact]
        public void GetValue_WithExistingByteField_ReturnsFieldValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { PublicByteField = 200 };
            var indexed = new Indexed<PrimitiveSubject>(subject, EmptyMetadata);

            // Act
            byte result = indexed.GetValue<byte>("PublicByteField");

            // Assert
            Assert.Equal((byte)200, result);
        }

        #endregion

        #region GetValue - primitive/scalar metadata conversion coverage

        [Fact]
        public void GetValue_WithMetadataBoolString_ConvertsAndReturns()
        {
            // Arrange
            var subject = new PrimitiveSubject { IsEnabled = false };
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["IsEnabled"] = "true"
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            bool result = indexed.GetValue<bool>("IsEnabled");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetValue_WithMetadataNumericStringForLong_ConvertsAndReturns()
        {
            // Arrange
            var subject = new PrimitiveSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["CountLong"] = "123456789"
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            long result = indexed.GetValue<long>("CountLong");

            // Assert
            Assert.Equal(123456789L, result);
        }

        [Fact]
        public void GetValue_WithMetadataNumericForDouble_ConvertsAndReturns()
        {
            // Arrange
            var subject = new PrimitiveSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ratio"] = 10
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            double result = indexed.GetValue<double>("Ratio");

            // Assert
            Assert.Equal(10d, result);
        }

        [Fact]
        public void GetValue_WithMetadataNumericStringForDecimal_ConvertsAndReturns()
        {
            // Arrange
            var subject = new PrimitiveSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Price"] = "789.01"
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            decimal result = indexed.GetValue<decimal>("Price");

            // Assert
            Assert.Equal(789.01m, result);
        }

        [Fact]
        public void GetValue_WithMetadataNumericForChar_ConvertsAndReturns()
        {
            // Arrange
            var subject = new PrimitiveSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Grade"] = 65
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            char result = indexed.GetValue<char>("Grade");

            // Assert
            Assert.Equal('A', result);
        }

        [Fact]
        public void GetValue_WithMetadataDateTimeString_ConvertsAndReturns()
        {
            // Arrange
            var subject = new PrimitiveSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["CreatedOn"] = "2026-05-08T12:34:56Z"
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            DateTime result = indexed.GetValue<DateTime>("CreatedOn");

            // Assert
            Assert.Equal(DateTime.Parse("2026-05-08T12:34:56Z"), result);
        }

        [Fact]
        public void GetValue_WithMetadataGuidValue_ReturnsGuid()
        {
            // Arrange
            Guid expected = Guid.NewGuid();
            var subject = new PrimitiveSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["UniqueId"] = expected
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            Guid result = indexed.GetValue<Guid>("UniqueId");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetValue_WithMetadataNullForValueType_ReturnsDefaultValue()
        {
            // Arrange
            var subject = new PrimitiveSubject { CountLong = 9L };
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["CountLong"] = null
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            long result = indexed.GetValue<long>("CountLong");

            // Assert
            Assert.Equal(0L, result);
        }

        [Fact]
        public void GetValue_WithMetadataNullForNullableType_ReturnsNull()
        {
            // Arrange
            var subject = new PrimitiveSubject { OptionalInt = 12 };
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["OptionalInt"] = null
            };
            var indexed = new Indexed<PrimitiveSubject>(subject, metadata);

            // Act
            int? result = indexed.GetValue<int?>("OptionalInt");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetValue – from object property

        [Fact]
        public void GetValue_WithExistingStringProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new TestSubject { Name = "Alice" };
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Act
            string result = indexed.GetValue<string>("Name");

            // Assert
            Assert.Equal("Alice", result);
        }

        [Fact]
        public void GetValue_WithExistingIntProperty_ReturnsPropertyValue()
        {
            // Arrange
            var subject = new TestSubject { Age = 30 };
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Act
            int result = indexed.GetValue<int>("Age");

            // Assert
            Assert.Equal(30, result);
        }

        [Fact]
        public void GetValue_WithPropertyName_IsCaseInsensitive()
        {
            // Arrange
            var subject = new TestSubject { Name = "Bob" };
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Act
            string result = indexed.GetValue<string>("name");

            // Assert
            Assert.Equal("Bob", result);
        }

        [Fact]
        public void GetValue_WithNullPropertyValue_ReturnsDefault()
        {
            // Arrange
            var subject = new TestSubject { Name = null };
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Act
            string result = indexed.GetValue<string>("Name");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetValue – from public field

        [Fact]
        public void GetValue_WithExistingPublicField_ReturnsFieldValue()
        {
            // Arrange
            var subject = new TestSubject { InternalField = "fieldValue" };
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Act
            string result = indexed.GetValue<string>("InternalField");

            // Assert
            Assert.Equal("fieldValue", result);
        }

        #endregion

        #region GetValue – unknown property

        [Fact]
        public void GetValue_WithUnknownPropertyName_ReturnsDefault()
        {
            // Arrange
            var subject = new TestSubject { Name = "Alice" };
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Act
            string result = indexed.GetValue<string>("DoesNotExist");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetValue_WithUnknownPropertyNameForValueType_ReturnsDefaultValue()
        {
            // Arrange
            var subject = new TestSubject();
            var indexed = new Indexed<TestSubject>(subject, EmptyMetadata);

            // Act
            int result = indexed.GetValue<int>("DoesNotExist");

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region GetValue – from metadata (takes precedence over property)

        [Fact]
        public void GetValue_WithMetadataKeyMatchingProperty_ReturnsMetadataValue()
        {
            // Arrange
            var subject = new TestSubject { Name = "Alice" };
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = "MetadataAlice"
            };
            var indexed = new Indexed<TestSubject>(subject, metadata);

            // Act
            string result = indexed.GetValue<string>("Name");

            // Assert
            Assert.Equal("MetadataAlice", result);
        }

        [Fact]
        public void GetValue_WithMetadataOnlyKey_ReturnsMetadataValue()
        {
            // Arrange
            var subject = new TestSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["CustomKey"] = "customValue"
            };
            var indexed = new Indexed<TestSubject>(subject, metadata);

            // Act
            string result = indexed.GetValue<string>("CustomKey");

            // Assert
            Assert.Equal("customValue", result);
        }

        [Fact]
        public void GetValue_WithMetadataValueRequiringConversion_ConvertsAndReturns()
        {
            // Arrange
            var subject = new TestSubject();
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Age"] = "25"   // stored as string, requested as int
            };
            var indexed = new Indexed<TestSubject>(subject, metadata);

            // Act
            int result = indexed.GetValue<int>("Age");

            // Assert
            Assert.Equal(25, result);
        }

        [Fact]
        public void GetValue_WithNullOrWhitespacePropertyName_ThrowsArgumentException()
        {
            // Arrange
            var indexed = new Indexed<TestSubject>(new TestSubject(), EmptyMetadata);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => indexed.GetValue<string>(null));
            Assert.Throws<ArgumentException>(() => indexed.GetValue<string>(string.Empty));
            Assert.Throws<ArgumentException>(() => indexed.GetValue<string>("   "));
        }

        #endregion
    }
}
