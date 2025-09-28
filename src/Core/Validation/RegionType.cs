namespace Core.Validation
{
    /// <summary>
    /// Defines the types of regions that can be validated.
    /// </summary>
    public enum RegionType
    {
        /// <summary>
        /// Continent code (e.g., EU, NA, SA)
        /// </summary>
        Continent,

        /// <summary>
        /// ISO 3166-1 alpha-2 country code (e.g., US, FR, JP)
        /// </summary>
        Country,

        /// <summary>
        /// IATA airport/city code (e.g., JFK, LHR, CDG)
        /// </summary>
        City
    }
}

