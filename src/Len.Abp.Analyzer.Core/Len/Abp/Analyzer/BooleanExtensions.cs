namespace Len.Abp.Analyzer;

public static class BooleanExtensions
{
    public static string ToBooleanKeyword(this bool value) => value ? "true" : "false";
}