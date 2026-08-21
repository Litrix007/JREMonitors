using System;

namespace JREMonitors.Core.Utils
{
    public static class StringHelper
    {
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;
            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static char ToFullWidth(this char c, bool convertMarks = true, bool convertNumbers = true,
            bool convertWords = true)
        {
            if (c == ' ') return !convertMarks ? c : '\u3000';
            if (c < 33 || c > 126) return c;
            var isNumber = c >= '0' && c <= '9';
            var isWord = IsAsciiLetter(c);
            var isMark = !isNumber && !isWord;
            if ((isNumber && convertNumbers) ||
                (isWord && convertWords) ||
                (isMark && convertMarks))
                return (char)(c + 0xFEE0);

            return c;
        }

        public static string ToFullWidth(this string text, bool convertMarks = true, bool convertNumbers = true,
            bool convertWords = true)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.Length <= 128)
            {
                Span<char> chars = stackalloc char[text.Length];
                for (var i = 0; i < chars.Length; i++)
                    chars[i] = text[i].ToFullWidth(convertMarks, convertNumbers, convertWords);

                return chars.ToString();
            }

            var array = new char[text.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = text[i].ToFullWidth(convertMarks, convertNumbers, convertWords);
            return new string(array);
        }

        public static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }
    }
}