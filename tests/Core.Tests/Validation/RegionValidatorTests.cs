using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Core.Validation.Tests
{
    public class RegionValidatorTests
    {
        private readonly RegionValidator _validator;

        public RegionValidatorTests()
        {
            _validator = new RegionValidator();
        }

        [Theory]
        [InlineData("EU", true)]  // Europe
        [InlineData("NA", true)]  // North America
        [InlineData("SA", true)]  // South America
        [InlineData("AS", true)]  // Asia
        [InlineData("AF", true)]  // Africa
        [InlineData("OC", true)]  // Oceania
        [InlineData("AN", true)]  // Antarctica
        [InlineData("eu", true)]  // Should be case insensitive
        [InlineData("XX", false)] // Invalid code
        [InlineData("USA", false)] // Too long
        [InlineData("", false)]   // Empty
        [InlineData(null, false)] // Null
        public void IsValidContinentCode_ReturnsExpectedResult(string code, bool expected)
        {
            // Act
            var result = _validator.IsValidContinentCode(code);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("US", true)]  // United States
        [InlineData("FR", true)]  // France
        [InlineData("JP", true)]  // Japan
        [InlineData("fr", true)]  // Should be case insensitive
        [InlineData("USA", false)] // Not ISO 3166-1 alpha-2
        [InlineData("XX", false)] // Invalid code
        [InlineData("", false)]   // Empty
        [InlineData(null, false)] // Null
        public void IsValidCountryCode_ReturnsExpectedResult(string code, bool expected)
        {
            // Act
            var result = _validator.IsValidCountryCode(code);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("JFK", true)]  // New York JFK
        [InlineData("LHR", true)]  // London Heathrow
        [InlineData("CDG", true)]  // Paris Charles de Gaulle
        [InlineData("lax", false)] // Not uppercase
        [InlineData("NY", false)]  // Too short
        [InlineData("NYCT", false)] // Too long
        [InlineData("JF1", false)] // Contains non-letter
        [InlineData("", false)]    // Empty
        [InlineData(null, false)]  // Null
        public void IsValidCityCode_ReturnsExpectedResult(string code, bool expected)
        {
            // Act
            var result = _validator.IsValidCityCode(code);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ValidateRegionCodes_WithValidContinentCodes_ReturnsTrue()
        {
            // Arrange
            var codes = new List<string> { "EU", "NA", "AS" };

            // Act
            var result = _validator.ValidateRegionCodes(RegionType.Continent, codes, out var invalidCodes);

            // Assert
            Assert.True(result);
            Assert.Empty(invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithInvalidContinentCodes_ReturnsFalse()
        {
            // Arrange
            var codes = new List<string> { "EU", "XX", "ZZ" };

            // Act
            var result = _validator.ValidateRegionCodes(RegionType.Continent, codes, out var invalidCodes);

            // Assert
            Assert.False(result);
            Assert.Equal(2, invalidCodes.Count);
            Assert.Contains("XX", invalidCodes);
            Assert.Contains("ZZ", invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithValidCountryCodes_ReturnsTrue()
        {
            // Arrange
            var codes = new List<string> { "US", "FR", "JP" };

            // Act
            var result = _validator.ValidateRegionCodes(RegionType.Country, codes, out var invalidCodes);

            // Assert
            Assert.True(result);
            Assert.Empty(invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithInvalidCountryCodes_ReturnsFalse()
        {
            // Arrange
            var codes = new List<string> { "US", "XX", "ZZ" };

            // Act
            var result = _validator.ValidateRegionCodes(RegionType.Country, codes, out var invalidCodes);

            // Assert
            Assert.False(result);
            Assert.Equal(2, invalidCodes.Count);
            Assert.Contains("XX", invalidCodes);
            Assert.Contains("ZZ", invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithValidCityCodes_ReturnsTrue()
        {
            // Arrange
            var codes = new List<string> { "JFK", "LHR", "CDG" };

            // Act
            var result = _validator.ValidateRegionCodes(RegionType.City, codes, out var invalidCodes);

            // Assert
            Assert.True(result);
            Assert.Empty(invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithInvalidCityCodes_ReturnsFalse()
        {
            // Arrange
            var codes = new List<string> { "JFK", "abc", "NY" };

            // Act
            var result = _validator.ValidateRegionCodes(RegionType.City, codes, out var invalidCodes);

            // Assert
            Assert.False(result);
            Assert.Equal(2, invalidCodes.Count);
            Assert.Contains("abc", invalidCodes);
            Assert.Contains("NY", invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithEmptyList_ReturnsFalse()
        {
            // Arrange
            var codes = new List<string>();

            // Act
            var result = _validator.ValidateRegionCodes(RegionType.Continent, codes, out var invalidCodes);

            // Assert
            Assert.False(result);
            Assert.Empty(invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithNullList_ReturnsFalse()
        {
            // Act
            var result = _validator.ValidateRegionCodes(RegionType.Continent, null, out var invalidCodes);

            // Assert
            Assert.False(result);
            Assert.Empty(invalidCodes);
        }

        [Fact]
        public void ValidateRegionCodes_WithInvalidRegionType_ReturnsFalse()
        {
            // Arrange
            var codes = new List<string> { "US", "FR", "JP" };

            // Act
            var result = _validator.ValidateRegionCodes((RegionType)99, codes, out var invalidCodes);

            // Assert
            Assert.False(result);
            Assert.Equal(3, invalidCodes.Count);
        }
    }
}
