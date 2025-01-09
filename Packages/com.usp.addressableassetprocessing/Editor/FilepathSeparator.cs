public class FilepathSeparator : StringSeparator
{
    #region Constants
    private static readonly char[] s_delimiters = new[] { '\\', '/' };
    #endregion

    #region Methods
    public FilepathSeparator(string prefix)
    {
        RemovablePrefix = prefix;
        Delimiters = s_delimiters;
    }
    #endregion
}
