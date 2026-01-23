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
    }
}
