namespace UsersDirectory.Utility
{
    public static class EmailUtils
    {
        // Allowed special characters in an unquoted local-part (RFC 5321 §2.3.11)
        private static readonly HashSet<char> UnquotedSpecial =
            new("!#$%&'*+-/=?^_`{|}~".ToCharArray());

        /// <summary>
        /// Validates an email address following RFC 5321 / RFC 5322 rules.
        /// Supports:
        ///   - Display names:   Jane Smith &lt;jane@example.com&gt;
        ///   - Quoted locals:   "john..doe"@example.com
        ///   - Comments:        (comment)jane@example.com  /  jane@example.com(comment)
        ///   - IP literals:     jsmith@[192.168.2.1]
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            if (email.Length > 254) return false;

            // Strip display-name form:  Display Name <local@domain>
            var bracket = email.LastIndexOf('<');
            if (bracket >= 0)
            {
                if (!email.EndsWith('>')) return false;
                email = email[(bracket + 1)..^1].Trim();
            }

            // Find the @ that separates local-part from domain
            // (ignore @ inside a quoted local-part)
            var atIndex = FindAtSign(email);
            if (atIndex < 0) return false;

            var local  = email[..atIndex];
            var domain = email[(atIndex + 1)..];

            // Strip RFC-allowed comments at the boundaries
            local  = StripComments(local);
            domain = StripComments(domain);

            return IsValidLocalPart(local) && IsValidDomain(domain);
        }

        // ── Local-part ────────────────────────────────────────────────────────

        private static bool IsValidLocalPart(string local)
        {
            if (string.IsNullOrEmpty(local) || local.Length > 64) return false;

            return local.StartsWith('"')
                ? IsValidQuotedLocal(local)
                : IsValidUnquotedLocal(local);
        }

        private static bool IsValidQuotedLocal(string local)
        {
            // Must be surrounded by exactly one pair of double-quotes
            if (!local.EndsWith('"') || local.Length < 2) return false;
            var inner   = local[1..^1];
            var escaped = false;

            foreach (var c in inner)
            {
                if (escaped)
                {
                    // After \: HT, Space, or any printable ASCII (32-126)
                    if (c != '\t' && (c < 32 || c > 126)) return false;
                    escaped = false;
                    continue;
                }
                if (c == '\\') { escaped = true; continue; }
                if (c == '"')  return false;           // unescaped quote inside
                if (c == '\t' || c == ' ') continue;   // HT and Space are allowed
                if (c < 32 || c > 126) return false;   // non-ASCII / control chars
            }
            return !escaped; // trailing backslash is invalid
        }

        private static bool IsValidUnquotedLocal(string local)
        {
            if (local.StartsWith('.') || local.EndsWith('.')) return false;
            if (local.Contains("..")) return false;

            foreach (var c in local)
            {
                if (char.IsAsciiLetterOrDigit(c) || c == '.' || UnquotedSpecial.Contains(c))
                    continue;
                return false;
            }
            return true;
        }

        // ── Domain ────────────────────────────────────────────────────────────

        private static bool IsValidDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain) || domain.Length > 255) return false;

            // IP address literal: [192.0.2.1] or [IPv6:2001:db8::1]
            if (domain.StartsWith('[') && domain.EndsWith(']'))
                return true;

            // Hostname: one or more dot-separated labels
            var labels = domain.Split('.');
            if (labels.Length < 2) return false;

            foreach (var label in labels)
            {
                if (!IsValidDomainLabel(label)) return false;
            }

            // TLD must not be all-numeric (RFC 5321)
            return !labels[^1].All(char.IsDigit);
        }

        private static bool IsValidDomainLabel(string label)
        {
            if (string.IsNullOrEmpty(label) || label.Length > 63) return false;
            if (label.StartsWith('-') || label.EndsWith('-'))      return false;

            foreach (var c in label)
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '-') return false;
            }
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the index of the last unquoted @ sign in the string,
        /// or -1 if none is found.
        /// </summary>
        private static int FindAtSign(string email)
        {
            var inQuotes = false;
            var escaped  = false;
            var lastAt   = -1;

            for (var i = 0; i < email.Length; i++)
            {
                var c = email[i];
                if (escaped)  { escaped  = false; continue; }
                if (c == '\\' && inQuotes) { escaped = true; continue; }
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == '@' && !inQuotes) lastAt = i;
            }
            return lastAt;
        }

        /// <summary>
        /// Strips a leading and/or trailing comment of the form (…) from a string.
        /// </summary>
        private static string StripComments(string s)
        {
            // Leading comment
            if (s.StartsWith('('))
            {
                var depth = 0;
                for (var i = 0; i < s.Length; i++)
                {
                    if      (s[i] == '(') depth++;
                    else if (s[i] == ')') { if (--depth == 0) { s = s[(i + 1)..]; break; } }
                }
            }
            // Trailing comment
            if (s.EndsWith(')'))
            {
                var depth = 0;
                for (var i = s.Length - 1; i >= 0; i--)
                {
                    if      (s[i] == ')') depth++;
                    else if (s[i] == '(') { if (--depth == 0) { s = s[..i]; break; } }
                }
            }
            return s;
        }
    }
}

