namespace ItemsStore.Utility.Mappers
{
    public static class StringUtils
    {
        public static string ToKebabCase(this string value)
        {
            return string.Join("-", value.ToLower().Split());
        }

        public static string ToSnakeCase(this string value)
        {
            return string.Join("_", value.ToLower().Split());
        }
    }
}
