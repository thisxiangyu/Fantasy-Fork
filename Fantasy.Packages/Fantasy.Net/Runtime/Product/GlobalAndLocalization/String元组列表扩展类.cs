using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

#pragma warning disable CS8603 // 允许返回Null
#pragma warning disable CS8625 
#pragma warning disable CS8604 

namespace Fantasy.GlobalAndLocalization
{
    public static class String元组列表扩展类
    {
        /// <summary>
        /// 返回一个可以打印的文字字符串。
        /// 可以只显示与 item1 匹配的项，也可全部打印。
        /// </summary>
        public static string IntoString(this List<(string, string)> self, string item1 = null)
        {
            if (self == null || self.Count == 0)
                return "<empty>";

            // 根据 item1 筛选，如果为 null 或空则显示全部
            IEnumerable<(string, string)> listToPrint = self;
            if (!string.IsNullOrEmpty(item1))
            {
                listToPrint = self.Where(t => t.Item1 == item1);
            }

            // 构造每行输出
            var lines = listToPrint.Select(t => $"{t.Item1}: {t.Item2}");
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// 根据key返回首个value
        /// </summary>
        public static string GetFirstValueByKey(this List<(string, string)> self, string item1)
        {
            for (int i = 0; i < self.Count; i++)
            {
                if (self[i].Item1 == item1)
                    return self[i].Item2;
            }
            return null;
        }

        /// <summary>
        /// 根据key返回value列表
        /// </summary>
        public static List<string> GetValueListByKey(this List<(string, string)> self, string item1)
        {
            List<string> res = new();
            foreach (var x in self)
            {
                if (x.Item1 == item1)
                    res.Add(x.Item2);
            }
            return res;
        }

        /// <summary>
        /// 根据value返回首个key
        /// </summary>
        public static string GetFirstKeyByValue(this List<(string, string)> self, string item2)
        {
            foreach (var x in self)
            {
                if (x.Item2 == item2)
                    return x.Item1;
            }
            return null;
        }

        /// <summary>
        /// 根据value返回key列表
        /// </summary>
        public static List<string> GetKeyListByValue(this List<(string, string)> self, string item2)
        {
            List<string> res = new();
            foreach (var x in self)
            {
                if (x.Item2 == item2)
                    res.Add(x.Item1);
            }
            return res;
        }

        /// <summary>
        /// 是否存在Value
        /// </summary>
        public static bool ContainsValue(this List<(string, string)> self, string item2)
        {
            foreach (var x in self)
            {
                if (x.Item2 == item2)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 是否存在Key
        /// </summary>
        public static bool ContainsKey(this List<(string, string)> self, string item1)
        {
            foreach (var x in self)
            {
                if (x.Item1 == item1)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 是否存在Value包含
        /// </summary>
        public static string? AnyValueContains(this List<(string, string)> self, string item2)
        {
            if (item2 == null)
                return null;

            foreach (var x in self)
            {
                if (x.Item2.Contains(item2))
                    return x.Item2;
            }
            return null;
        }

        /// <summary>
        /// 是否存在Key包含
        /// </summary>
        public static string? AnyKeyContains(this List<(string, string)> self, string item1)
        {
            if (item1 == null)
                return null;

            foreach (var x in self)
            {
                if (x.Item1.Contains(item1))
                    return x.Item1;
            }
            return null;
        }

        /// <summary>
        /// 是否存在元素包含
        /// </summary>
        public static string? AnyItemContains(this List<string> self, string item)
        {
            if (item == null)
                return null;

            foreach (var x in self)
            {
                if (x.Contains(item))
                    return x;
            }
            return null;
        }

        // CompareInfo 缓存
        static readonly CompareInfo _invariantCompare = CultureInfo.InvariantCulture.CompareInfo;

        // 跨语种首字母排序方案。
        static List<(string, string)> InternalRankByFirstChar(
            List<(string, string)> self,
            bool sort_by_key,
            Dictionary<string, string> sorting_logic = null)
        {
            if (self == null || self.Count <= 1) return self;

            // 直接使用 List.Sort 进行原地排序
            self.Sort((x, y) =>
            {
                // 提取原始待排字符串
                string s1 = sort_by_key ? x.Item1 : x.Item2;
                string s2 = sort_by_key ? y.Item1 : y.Item2;

                // 局部微调逻辑：尝试从字典中获取“排序替身”
                // 如果字典不为空且包含该 key，则使用 value 参与比较，否则保留原样
                if (sorting_logic != null)
                {
                    if (s1 != null && sorting_logic.TryGetValue(s1, out var alias1)) s1 = alias1;
                    if (s2 != null && sorting_logic.TryGetValue(s2, out var alias2)) s2 = alias2;
                }

                // --- 新增：脚本优先级判定逻辑 ---
                int p1 = GetCharPriority(s1);
                int p2 = GetCharPriority(s2);

                if (p1 != p2)
                    return p1.CompareTo(p2); // 如果优先级不同，直接按优先级排

                return _invariantCompare.Compare(s1, s2, CompareOptions.IgnoreCase);
            });

            return self;
        }

        /// <summary>
        /// 字符的排序优先级分类。(数值越小，排位越靠前。)
        /// </summary>
        static int GetCharPriority(string s)
        {
            if (string.IsNullOrEmpty(s)) return 100; // 空白排最后

            char c = s[0];

            // 1. 真正的控制字符（回车、Tab等）
            if (c < 32) return 0;

            // 2. 显式的置顶符号（如果你使用了 \x01 这种控制字符作为 alias 前缀）
            if (c == '\x01') return 1;

            // 3. 空格
            if (c == 32) return 2;

            // 4. 普通标点与符号
            if (char.IsPunctuation(c) || char.IsSymbol(c)) return 3;

            // 5. 数字
            if (char.IsDigit(c)) return 4;
            // 6. 字母与文字 (包含拉丁语、中文、以及阿姆哈拉语等文字)
            // 将吉兹字母等统一归为此类，它们将遵循 InvariantCompare 在字母桶内的内部排序
            return 5;
        }

        public static class 排序符号
        {
            public const char Top = '\x01';
        }

        /// <summary>
        /// 根据 Key(item1) 的首字母排序。
        /// sorting_logic 是一个额外的string字典, 用于修正特定的排序元素。
        /// 该字典的 key 是原字符串, value 是用于修正的字符串,
        /// 比如 key 为 English (US), value 为 EnglishA: 
        /// 意味着在排序的时候一旦遇到 English(US) 就使用 EnglishA 来作为"排序替身"完成排序, 
        /// 从而达到局部修改顺序的效果。
        /// 注意: 该方法返回的依然是原字符串的内容, 只是顺序受到"排序替身"影响, 而产生了微调。
        /// </summary>
        public static List<(string, string)> RankByFirstCharOfKey(this List<(string, string)> self, Dictionary<string, string> sorting_logic = null)
            => InternalRankByFirstChar(self, true, sorting_logic);

        /// <summary>
        /// 根据 Value(item2) 的首字母排序。
        /// sorting_logic 是一个额外的string字典, 用于修正特定的排序元素。
        /// 该字典的 key 是原字符串, value 是用于修正的字符串,
        /// 比如 key 为 English (US), value 为 EnglishA: 
        /// 意味着在排序的时候一旦遇到 English(US) 就使用 EnglishA 来作为"排序替身"完成排序, 
        /// 从而达到局部修改顺序的效果。
        /// 注意: 该方法返回的依然是原字符串的内容, 只是顺序受到"排序替身"影响, 而产生了微调。
        /// </summary>
        public static List<(string, string)> RankByFirstCharOfValue(this List<(string, string)> self, Dictionary<string, string> sorting_logic = null)
            => InternalRankByFirstChar(self, false, sorting_logic);


        ///// <summary>
        ///// 根据 Key(item1) 的首字母排序
        ///// </summary>
        //public static List<(string, string)> RankByFirstCharOfKey(this List<(string, string)> self)
        //{
        //    if (self == null || self.Count <= 1) return self;

        //    // StringHelper.Compare 在底层针对全语言有非常深度的优化
        //    self.Sort((x, y) =>
        //        _invariantCompare.Compare(x.Item1, y.Item1, CompareOptions.None));

        //    return self;
        //}

        ///// <summary>
        ///// 根据 Value(item2) 的首字母排序
        ///// </summary>
        //public static List<(string, string)> RankByFirstCharOfValue(this List<(string, string)> self)
        //{
        //    if (self == null || self.Count <= 1) return self;

        //    self.Sort((x, y) =>
        //        _invariantCompare.Compare(x.Item2, y.Item2, CompareOptions.None));

        //    return self;
        //}
    }
}