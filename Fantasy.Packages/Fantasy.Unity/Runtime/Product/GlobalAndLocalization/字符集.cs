using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Fantasy.GlobalAndLocalization
{
    public class 字符集
    {
        /// <summary>
        /// 包括拉丁字母和西里尔字母,、所有大小写、特殊字母
        /// </summary>
        /// <param name="outputPath"></param>
        public static void 输出欧洲全部字母(string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // 1. 基本拉丁字母 (ASCII) + 拉丁-1 增补 (西欧常用如德语 ß, 法语 é)
            AppendRange(sb, 0x0020, 0x007F); // 基础 ASCII
            AppendRange(sb, 0x00A0, 0x00FF); // 西欧增补

            // 2. 拉丁扩展 A & B (东欧、中欧、北欧特有字母如捷克语 ř, 波兰语 ł)
            AppendRange(sb, 0x0100, 0x017F);
            AppendRange(sb, 0x0180, 0x024F);

            // 3. 西里尔字母 (俄语、乌克兰语、塞尔维亚语等)
            AppendRange(sb, 0x0400, 0x04FF);

            // 4. 希腊字母及扩展
            AppendRange(sb, 0x0370, 0x03FF);

            // 5. 补充
            sb.Append("ẞ"); // 德语大写 Esszet (1E9E)，较新标准

            WriteToFile(outputPath, sb.ToString());
        }

        public static void 输出常用符号(string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // 1. 基础 ASCII 标点 (键盘能直接按出来的)
            // 范围：空格到斜杠，以及数字间的符号
            AppendRange(sb, 0x0020, 0x002F); //  !"#$%&'()*+,-./
            AppendRange(sb, 0x003A, 0x0040); // :;<=>?@
            AppendRange(sb, 0x005B, 0x0060); // [\]^_`
            AppendRange(sb, 0x007B, 0x007E); // {|}~

            // 2. 广义标点 (General Punctuation)
            // 包含：特殊引号 “” ‘’、破折号 —、省略号 …、以及 Bullet 点 •
            AppendRange(sb, 0x2000, 0x206F);

            // 3. 货币符号 (Currency Symbols)
            // 包含：€ (欧元), ￡ (英镑), ￥ (日元/人民币), ₩ (韩元), ₽ (卢布), ฿ (泰铢) 等
            AppendRange(sb, 0x20A0, 0x20CF);

            // 4. 箭头与几何图形 (UI 常用)
            // 包含：← ↑ → ↓ ◀ ▶ ▲ ▼ ◁ ▷ △ ▽ 等
            AppendRange(sb, 0x2190, 0x21FF); // 各种箭头
            AppendRange(sb, 0x25A0, 0x25FF); // 各种方块、三角、圆圈 (包含你需要的左三角 ◀)

            // 5. 数学运算符
            // 包含：± × ÷ √ ∞ ≈ ≠ ≤ ≥
            AppendRange(sb, 0x2200, 0x22FF);

            // 6. 额外补充：中文/日韩常用全角标点 (如果你的项目涉及东亚语言)
            // 包含：，。！？（）《》【】
            sb.Append("，。！？（）《》【】“”‘’；：￥…—·");

            // 去重
            string result = new string(sb.ToString().Distinct().ToArray());

            WriteToFile(outputPath, result);
        }

        /// <summary>
        /// 输出 Unicode 标准中定义的所有韩语相关字符（共计 11,172 个完整音节 + 基础字母）
        /// </summary>
        /// <param name="outputPath"></param>
        public static void 输出全部韩语文字(string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // 1. 韩文字母 (Hangul Jamo)
            // 包含初声、中声、终声的基础组合构件
            AppendRange(sb, 0x1100, 0x11FF);

            // 2. 韩文兼容字母 (Hangul Compatibility Jamo)
            // 类似于键盘直接输入的单个辅音和元音 (ㄱ, ㄴ, ㅏ, ㅑ...)
            AppendRange(sb, 0x3130, 0x318F);

            // 3. 韩文音节 (Hangul Syllables) - 核心部分
            // 这是韩文最完整、最庞大的区块，包含从 '가' 到 '힣' 的所有 11,172 个组合音节
            // 在制作 TMP 字体时，如果 TTF 字体支持全量，这里能保证覆盖所有现代韩语输入
            AppendRange(sb, 0xAC00, 0xD7A3);

            // 去重并输出
            string result = new string(sb.ToString().Distinct().ToArray());
            WriteToFile(outputPath, result);
        }

        /// <summary>
        /// 输出西亚语种（阿拉伯语、波斯语、乌尔都语等）的所有基础字母。
        /// 注意：该类语种需要 RTL（从右往左）渲染支持和特殊的文法支持。
        /// </summary>
        /// <param name="outputPath"></param>
        public static void 输出西亚全部字母(string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // 1. 标准阿拉伯语 (Arabic)
            // 范围：0x0600 - 0x06FF
            // 包含基础字母、阿拉伯数字、以及常见的音符标记（Harakat）
            AppendRange(sb, 0x0600, 0x06FF);

            // 2. 阿拉伯语增补 (Arabic Supplement)
            // 范围：0x0750 - 0x077F
            // 主要用于非洲和亚洲的一些少数民族语言的阿拉伯字母扩展
            AppendRange(sb, 0x0750, 0x077F);

            // 3. 阿拉伯语扩展-A (Arabic Extended-A)
            // 范围：0x08A0 - 0x08FF
            // 包含波斯语、乌尔都语等特有的额外字母
            AppendRange(sb, 0x08A0, 0x08FF);

            // 4. 阿拉伯字母变体演示格式 (Arabic Presentation Forms)
            // 这些是字母在开头、中间、结尾时的形态，虽然 TMP 通常能动态处理，
            // 但为了保险，生成静态字库时有时会包含这些区域。
            AppendRange(sb, 0xFB50, 0xFDFF); // Forms-A
            AppendRange(sb, 0xFE70, 0xFEFF); // Forms-B

            // 去重
            string result = new string(sb.ToString().Distinct().ToArray());

            WriteToFile(outputPath, result);
        }

        /// <summary>
        /// 南亚文字及小众语种字母
        /// </summary>
        public static void 输出南亚文字字母(string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // 天城体 (Devanagari): 印度语(Hindi)、尼泊尔语、马拉地语等最常用的文字
            AppendRange(sb, 0x0900, 0x097F);

            // 孟加拉文 (Bengali): 孟加拉国官方语言
            AppendRange(sb, 0x0980, 0x09FF);

            // 古吉拉特文 (Gujarati)
            AppendRange(sb, 0x0A80, 0x0AFF);

            // 泰米尔文 (Tamil): 印度南部及斯里兰卡、新加坡常用
            AppendRange(sb, 0x0B80, 0x0BFF);

            // 泰卢固文 (Telugu)
            AppendRange(sb, 0x0C00, 0x0C7F);

            // 卡纳达语 (Kannada): 对应班加罗尔(印度硅谷)
            AppendRange(sb, 0x0C80, 0x0CFF);

            // 马拉雅拉姆语 (Malayalam): 对应印度喀拉拉邦
            AppendRange(sb, 0x0D00, 0x0D7F);

            // 旁遮普语 (Gurmukhi): 对应印度旁遮普邦
            AppendRange(sb, 0x0A00, 0x0A7F);

            //----------- 一些小众的 -----------

            // 它拿文 (Thaana): 马尔代夫官方语言
            AppendRange(sb, 0x0780, 0x07BF);
            // 奥里亚文 (Oriya): 印度奥里萨邦
            AppendRange(sb, 0x0B00, 0x0B7F);
            // 僧伽罗文 (Sinhala): 斯里兰卡主要语言
            AppendRange(sb, 0x0D80, 0x0DFF);

            // 去重并输出
            string result = new string(sb.ToString().Distinct().ToArray());
            WriteToFile(outputPath, result);
        }

        /// <summary>
        /// 东南亚婆罗米系文字及小众语种字母
        /// </summary>
        public static void 输出东南亚文字字母(string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // 泰文 (Thai): 泰国官方语言，笔画带有大量上下堆叠的元音符号
            AppendRange(sb, 0x0E00, 0x0E7F);

            // 老挝文 (Lao)
            AppendRange(sb, 0x0E80, 0x0EFF);

            // 缅甸文 (Myanmar): 包含基础字符及扩展
            AppendRange(sb, 0x1000, 0x109F);

            // 高棉文 (Khmer): 柬埔寨
            AppendRange(sb, 0x1780, 0x17FF);

            // 越南语额外字符 (Latin Extended Additional)
            // 越南语虽用拉丁字母，但声调符号非常多，需要此扩展区才能显示正确
            AppendRange(sb, 0x1E00, 0x1EFF);

            //----------- 一些小众的 -----------

            // 巽他字母 (Sundanese): 印尼爪哇岛部分地区
            AppendRange(sb, 0x1B80, 0x1BBF);
            // 爪哇文 (Javanese): 印尼传统文学常用
            AppendRange(sb, 0xA980, 0xA9DF);
            // 查姆文 (Cham): 越南/柬埔寨少数民族
            AppendRange(sb, 0xAA00, 0xAA5F);
            // 泰语/老挝语的数字与特殊标点 (有些字体不包含在基础区)
            AppendRange(sb, 0x0E50, 0x0E59); // 泰语数字 0-9
            // 菲律宾贝贝因文字 (Baybayin/Tagalog): 菲律宾古文字
            AppendRange(sb, 0x1700, 0x171F);
            // 布吉文 (Buginese): 印度尼西亚苏拉威西
            AppendRange(sb, 0x1A00, 0x1A1F);

            // 去重并输出
            string result = new string(sb.ToString().Distinct().ToArray());
            WriteToFile(outputPath, result);
        }

        public static void 输出全部阿姆哈拉语文字(string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // 1. 基础吉兹字母 (Ethiopic)
            // 这是核心部分，包含阿姆哈拉语、提格雷语等最常用的音节和标点
            // 覆盖范围：1200 - 137F
            AppendRange(sb, 0x1200, 0x137F);

            // 2. 埃塞俄比亚补充区 (Ethiopic Supplement)
            // 包含一些额外的音节，主要用于某些特定方言或罕见词汇
            // 覆盖范围：1380 - 139F
            AppendRange(sb, 0x1380, 0x139F);

            // 3. 埃塞俄比亚扩展区 (Ethiopic Extended)
            // 包含一些用于其他当地语言（如奥罗莫语的部分书写）的额外字符
            // 覆盖范围：2D80 - 2DDF
            AppendRange(sb, 0x2D80, 0x2DDF);

            // 4. 埃塞俄比亚扩展-A (Ethiopic Extended-A)
            // 较新的 Unicode 标准补充
            // 覆盖范围：AB00 - AB2F
            AppendRange(sb, 0xAB00, 0xAB2F);

            // 去重并输出
            // 注意：阿姆哈拉语字符间没有类似韩语 Jamo 的逻辑组合，全部是独立码位
            string result = new string(sb.ToString().Distinct().ToArray());
            WriteToFile(outputPath, result);
        }

        private static void AppendRange(StringBuilder sb, int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                sb.Append((char)i);
            }
        }

        private static void WriteToFile(string path, string content)
        {
            try
            {
                // 使用 UTF-8 带 BOM 编码
                File.WriteAllText(path, content, Encoding.UTF8);
                Console.WriteLine($"<color=green>成功导出字符集至: {path}</color>");
            }
            catch (Exception e)
            {
                Console.WriteLine($"导出失败! {e.Message}");
            }
        }
    }
}