using System.Text.RegularExpressions;

namespace UsersDirectory.Utility
{
    public static class PasswordUtils
    {
        private static readonly Regex _upper = new("[A-Z]", RegexOptions.Compiled);
        private static readonly Regex _lower = new("[a-z]", RegexOptions.Compiled);
        private static readonly Regex _digit = new("[0-9]", RegexOptions.Compiled);
        private static readonly Regex _special = new(@"[!@#$%^&*()_\-+=\[\]{}|;:'"",.<>/?`~\\]", RegexOptions.Compiled);

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8 || password.Length > 256) return false;

            return _upper.IsMatch(password)
                && _lower.IsMatch(password)
                && _digit.IsMatch(password)
                && _special.IsMatch(password);
        }
    }
}
