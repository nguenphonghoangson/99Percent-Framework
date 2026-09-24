using System.Text;
using NinetyNine.Core;

namespace NinetyNine.Modules.Profile
{
    /// <summary>Display-name invariants, pure C#. Letters include Vietnamese diacritics (char.IsLetter).</summary>
    public static class ProfileRules
    {
        public const int MinNameLength = 3;
        public const int MaxNameLength = 16;

        /// <summary>Trims and collapses runs of whitespace to one space.</summary>
        public static string NormalizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var builder = new StringBuilder(raw.Length);
            var pendingSpace = false;
            foreach (var c in raw.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    pendingSpace = true;
                    continue;
                }

                if (pendingSpace) builder.Append(' ');
                pendingSpace = false;
                builder.Append(c);
            }

            return builder.ToString();
        }

        /// <summary>Validates an already normalised name.</summary>
        public static Result ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < MinNameLength) return Result.Fail(ProfileErrors.NameTooShort);
            if (name.Length > MaxNameLength) return Result.Fail(ProfileErrors.NameTooLong);

            foreach (var c in name)
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '_')
                    return Result.Fail(ProfileErrors.NameInvalidCharacters);

            return Result.Success;
        }
    }
}
