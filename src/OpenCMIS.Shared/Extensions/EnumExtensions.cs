using System.Reflection;

namespace OpenCMIS.Shared.Extensions
{
    /// <summary>
    ///     Extension methods for enum types.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        ///     Gets the localized description for the enum value.
        ///     Returns English description by default, falls back to enum name if no attribute is found.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The localized description string.</returns>
        public static string GetLocalizedDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null)
                return value.ToString();

            var attribute = field.GetCustomAttribute<InfoAttribute>();
            if (attribute == null)
                return value.ToString();

            // For now, return English description
            // TODO: Implement localization based on current culture
            return attribute.English;
        }
    }
}
