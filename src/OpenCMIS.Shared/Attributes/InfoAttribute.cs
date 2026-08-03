namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Provides metadata information for enum members.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InfoAttribute : Attribute
    {
        public InfoAttribute(string english, string chinese = "")
        {
            English = english;
            Chinese = chinese;
        }

        public string English { get; }
        public string Chinese { get; }
    }
}
