namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides localized description information for error codes and other enum values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InfoAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the InfoAttribute class.
        /// </summary>
        /// <param name="english">The English description.</param>
        /// <param name="chinese">The Chinese description.</param>
        public InfoAttribute(string english, string chinese)
        {
            English = english;
            Chinese  = chinese;
        }

        /// <summary>
        ///     Gets the English description.
        /// </summary>
        public string English { get; }

        /// <summary>
        ///     Gets the Chinese description.
        /// </summary>
        public string Chinese { get; }
    }
}

