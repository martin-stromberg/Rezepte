namespace Rezepte.Web.Extensions;

public static class StringExt   
{
    public static int ToInt32(this string str, int detaultValue = default(int))
    {
        if (!int.TryParse(str, out int value))
            value = detaultValue;
        return value;
    }
}