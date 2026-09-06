namespace Rezepte.Web.Extensions;

/// <summary>
/// Represents the string ext class.
/// </summary>
public static class StringExt
{
    /// <summary>
    /// tos the int32.
    /// </summary>
    /// <param name="str">The str parameter.</param>
    /// <param name="detaultValue">The detault value parameter.</param>
    /// <returns>The result.</returns>
    public static int ToInt32(this string str, int detaultValue = default(int))
    {
        if (!int.TryParse(str, out int value))
            value = detaultValue;
        return value;
    }
}
