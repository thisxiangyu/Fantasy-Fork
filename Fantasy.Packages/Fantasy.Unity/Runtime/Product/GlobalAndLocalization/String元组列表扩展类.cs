using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

#pragma warning disable CS8603 // 允许返回Null
#pragma warning disable CS8625 

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
            foreach (var x in self)
            {
                if (x.Item1 == item1)
                    return x.Item2;
            }
            return null;
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

        // CompareInfo 缓存
        static readonly CompareInfo _invariantCompare = CultureInfo.InvariantCulture.CompareInfo;

        // 跨语种首字母排序方案。
        // sorting_logic 是一个额外的string字典, 用于修正特定的排序元素。
        // 该字典的 key 是原字符串, value 是用于修正的字符串,
        // 比如 key 为 English (US), value 为 EnglishA: 
        // 意味着在排序的时候一旦遇到 English(US) 就使用 EnglishA 来作为"排序替身"完成排序, 
        // 从而达到局部修改顺序的效果。
        // 注意: 该方法返回的依然是原字符串的内容, 只是顺序受到"排序替身"影响, 而产生了微调。
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
                    if (s1 != null && sorting_logic.TryGetValue(s1, out var alias1))
                    {
                        s1 = alias1;
                    }

                    if (s2 != null && sorting_logic.TryGetValue(s2, out var alias2))
                    {
                        s2 = alias2;
                    }
                }

                return _invariantCompare.Compare(s1, s2, CompareOptions.IgnoreCase);
            });

            return self;
        }

        /// <summary>
        /// 根据 Key(item1) 的首字母排序
        /// </summary>
        public static List<(string, string)> RankByFirstCharOfKey(this List<(string, string)> self, Dictionary<string,string> sorting_logic = null)
            => InternalRankByFirstChar(self, true, sorting_logic);

        /// <summary>
        /// 根据 Value(item2) 的首字母排序
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