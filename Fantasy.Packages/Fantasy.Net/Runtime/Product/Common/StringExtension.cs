namespace Fantasy.Product
{
    public static class StringExtension
    {
        /// <summary>
        /// 用_替换字符串中的空格 以确保安全
        /// </summary>
        public static string ReplaceSpaceWith_(this string s)
        {
            if (s.IndexOf(' ') < 0)
                return s;

            return string.Create(s.Length, s, (chars, source) =>
            {
                for (int i = 0; i < source.Length; i++)
                {
                    chars[i] = source[i] == ' ' ? '_' : source[i];
                }
            });
        }

        /// <summary>
        /// 用_替换字符串中的. 以确保安全
        /// </summary>
        public static string ReplaceDotWith_(this string s)
        {
            if (s.IndexOf('.') < 0)
                return s;

            return string.Create(s.Length, s, (chars, source) =>
            {
                for (int i = 0; i < source.Length; i++)
                {
                    chars[i] = source[i] == '.' ? '_' : source[i];
                }
            });
        }

        /// <summary>
        /// 判断字符串是否全为数字字符
        /// </summary>
        /// <param name="input">字符</param>
        /// <param name="different_culture_digit">为false意味着仅阿拉伯数字, 为true意味着任何Unicode数字</param>
        /// <returns></returns>
        public static bool IsAllDigitChars(this string input ,bool different_culture_digit = false)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }

            foreach (char c in input)
            {
                bool isDigitValid;
                if (different_culture_digit)
                {
                    // 支持Unicode数字：用char.IsDigit判断
                    isDigitValid = char.IsDigit(c);
                }
                else
                {
                    // 仅支持0-9阿拉伯数字：判断ASCII范围
                    isDigitValid = c >= '0' && c <= '9';
                }
                // 只要有一个字符不符合，立即返回false
                if (!isDigitValid)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
