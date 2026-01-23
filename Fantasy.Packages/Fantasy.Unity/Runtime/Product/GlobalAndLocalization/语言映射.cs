using System;
using Fantasy.Database.Attributes;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

#if FANTASY_NET
using System.ComponentModel.DataAnnotations;
#endif

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8603 // 允许返回Null
#pragma warning disable CS8625
#pragma warning disable CS8600

namespace Fantasy.GlobalAndLocalization
{
    /// <summary>
    /// 这是一个翻译主题枚举类的例子, 其它翻译主题枚举类可以模仿这个扩展, 
    /// 关键在于打上<see cref="翻译主题枚举类Attribute"/>标签, 以及每个枚举值不能重复(通过设置首个枚举值)。
    /// </summary>
    [翻译主题枚举类]
    public enum 翻译主题 : uint
    {
        Unknown = 0,

        中文映射,
        自我映射,

        中国,
        中_台湾,
        中_香港,

    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class 翻译主题枚举类Attribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class 未受良好支持Attribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class 大语种Attribute : Attribute
    {

    }

    /// <summary>
    /// 元数据在反射取内容时默认忽视掉
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class 元数据Attribute : Attribute
    {

    }

    /// <summary>
    /// 默认排序是按照首字母+次级字母顺序依次排序的。
    /// 如果要订正排序, 比如 English (United States) 希望它排在仅次于 English 之后, 
    /// 可以打上标签给一个 "EnglishA" 的标记, 就可以识别为新的排序。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class 排序订正符文Attribute : Attribute
    {
        public string 排序符文 { get; private set; } = default;
        public 排序订正符文Attribute(string 排序符文)
        {
            this.排序符文 = 排序符文;
        }
    }

    /// <summary>
    /// 不同的项目, 需要分别继承这个类, 打上<see cref="DbSetAttribute"/>之后作为表存入数据库 
    /// </summary>
    public class 语言映射
    {
#if FANTASY_NET
        [Key]
#endif
        public uint theme_id { get; set; } = (uint)翻译主题.Unknown;
        [元数据] public string theme { get; set; } = 翻译主题.Unknown.ToString();

        // 中文系
        [大语种]
        [排序订正符文("中文")] public virtual string cn { get; set; } = string.Empty;
        [排序订正符文("中文B")] public virtual string cn_t { get; set; } = string.Empty;

        // 昂撒英语系
        [大语种]
        [排序订正符文("English")] public virtual string en { get; set; } = string.Empty; // 默认en为英式英语和泛用性英语的结合
        [排序订正符文("English !")] public virtual string en_us { get; set; } = string.Empty;
        public virtual string en_au { get; set; } = string.Empty;
        public virtual string en_ca { get; set; } = string.Empty;

        // 东亚系
        [大语种]
        public virtual string ja { get; set; } = string.Empty;
        [大语种]
        public virtual string ko { get; set; } = string.Empty;
        public virtual string mn { get; set; } = string.Empty;
        public virtual string mn_t { get; set; } = string.Empty;
        public virtual string my { get; set; } = string.Empty;
        public virtual string km { get; set; } = string.Empty;
        public virtual string lo { get; set; } = string.Empty;

        // 欧洲语言
        [大语种]
        public virtual string de { get; set; } = string.Empty;
        [大语种]
        public virtual string fr { get; set; } = string.Empty;
        [大语种]
        public virtual string es { get; set; } = string.Empty;
        public virtual string ca { get; set; } = string.Empty; // 西班牙法律要求必须给予加泰语特别支持
        [大语种]
        public virtual string it { get; set; } = string.Empty;
        [大语种]
        [排序订正符文("Português")] public virtual string pt { get; set; } = string.Empty;
        [大语种]
        public virtual string ru { get; set; } = string.Empty;
        public virtual string nl { get; set; } = string.Empty;
        public virtual string sv { get; set; } = string.Empty;
        public virtual string no { get; set; } = string.Empty;
        public virtual string da { get; set; } = string.Empty;
        public virtual string fi { get; set; } = string.Empty;
        public virtual string Is { get; set; } = string.Empty;  // 冰岛语, 用大I是因为is已经是C#的关键字了
        public virtual string pl { get; set; } = string.Empty;
        public virtual string cs { get; set; } = string.Empty;
        public virtual string hu { get; set; } = string.Empty;
        public virtual string el { get; set; } = string.Empty;
        public virtual string uk { get; set; } = string.Empty;
        public virtual string ro { get; set; } = string.Empty;
        public virtual string bg { get; set; } = string.Empty;
        public virtual string sr { get; set; } = string.Empty;
        public virtual string hr { get; set; } = string.Empty;
        public virtual string be { get; set; } = string.Empty;
        public virtual string lt { get; set; } = string.Empty;
        public virtual string et { get; set; } = string.Empty;
        public virtual string lv { get; set; } = string.Empty;
        public virtual string sk { get; set; } = string.Empty;
        public virtual string mt { get; set; } = string.Empty;
        public virtual string la { get; set; } = string.Empty;   // 拉丁语 (目前仅在梵蒂冈使用)
        public virtual string sl { get; set; } = string.Empty;
        public virtual string sq { get; set; } = string.Empty;
        public virtual string mk { get; set; } = string.Empty;
        public virtual string bs { get; set; } = string.Empty;
        public virtual string cnr { get; set; } = string.Empty;
        public virtual string rm { get; set; } = string.Empty;
        public virtual string lb { get; set; } = string.Empty;
        public virtual string ga { get; set; } = string.Empty;
        public virtual string tr { get; set; } = string.Empty; // 土耳其虽然主要在西亚, 但他们自认为属于欧洲

        // 西亚(中东)
        [大语种]
        public virtual string ar { get; set; } = string.Empty;
        public virtual string fa { get; set; } = string.Empty;
        public virtual string dr { get; set; } = string.Empty;
        public virtual string ps { get; set; } = string.Empty;
        public virtual string he { get; set; } = string.Empty;
        public virtual string ku { get; set; } = string.Empty;      // 伊拉克、叙利亚部分
        public virtual string hy { get; set; } = string.Empty;    // 亚美尼亚
        public virtual string az { get; set; } = string.Empty;    // 阿塞拜疆

        // 南亚
        public virtual string en_in { get; set; } = string.Empty;
        public virtual string en_pk { get; set; } = string.Empty;
        public virtual string ur { get; set; } = string.Empty;  // 巴基斯坦
        public virtual string dz { get; set; } = string.Empty;
        public virtual string dv { get; set; } = string.Empty;
        public virtual string si { get; set; } = string.Empty;
        public virtual string hi { get; set; } = string.Empty;
        public virtual string bn { get; set; } = string.Empty;
        public virtual string ne { get; set; } = string.Empty;
        public virtual string ta { get; set; } = string.Empty;

        // 印度特色
        public virtual string ml { get; set; } = string.Empty;
        public virtual string kn { get; set; } = string.Empty;
        public virtual string te { get; set; } = string.Empty;
        public virtual string mr { get; set; } = string.Empty;
        public virtual string gu { get; set; } = string.Empty;
        public virtual string or { get; set; } = string.Empty;
        public virtual string pa { get; set; } = string.Empty;
        public virtual string asm { get; set; } = string.Empty;
        public virtual string ks { get; set; } = string.Empty;
        public virtual string mni { get; set; } = string.Empty;
        public virtual string sd { get; set; } = string.Empty;
        public virtual string bh { get; set; } = string.Empty;
        public virtual string kok { get; set; } = string.Empty;

        // 东南亚
        public virtual string th { get; set; } = string.Empty;
        public virtual string vi { get; set; } = string.Empty;
        public virtual string id { get; set; } = string.Empty;
        public virtual string ms { get; set; } = string.Empty;
        public virtual string tl { get; set; } = string.Empty;
        public virtual string en_sgp { get; set; } = string.Empty;
        [排序订正符文("中文C")] public virtual string cn_ny { get; set; } = string.Empty;

        // 中亚
        public virtual string kk { get; set; } = string.Empty;
        public virtual string ru_kz { get; set; } = string.Empty;
        public virtual string uz { get; set; } = string.Empty;
        public virtual string ru_uz { get; set; } = string.Empty;
        public virtual string tk { get; set; } = string.Empty;
        public virtual string ru_tm { get; set; } = string.Empty;
        public virtual string ky { get; set; } = string.Empty;
        public virtual string ru_kg { get; set; } = string.Empty;
        public virtual string tg { get; set; } = string.Empty;
        public virtual string ru_tj { get; set; } = string.Empty;

        // 拉美 - 西班牙语系
        public virtual string es_mx { get; set; } = string.Empty;
        public virtual string es_ar { get; set; } = string.Empty;
        public virtual string es_co { get; set; } = string.Empty;
        public virtual string es_cl { get; set; } = string.Empty;
        public virtual string es_pe { get; set; } = string.Empty;
        public virtual string es_ve { get; set; } = string.Empty;
        public virtual string es_ec { get; set; } = string.Empty;
        public virtual string es_gt { get; set; } = string.Empty;
        public virtual string es_cu { get; set; } = string.Empty;

        // 拉美 - 葡萄牙语

        [排序订正符文("PortuguêA")] public virtual string pt_br { get; set; } = string.Empty;

        // 拉美 - 英语 / 法语（加勒比部分）

        public virtual string en_jm { get; set; } = string.Empty;
        public virtual string en_tt { get; set; } = string.Empty;
        public virtual string fr_ht { get; set; } = string.Empty;

        // 拉美土著语
        public virtual string ht { get; set; } = string.Empty; // 海地 克里奥尔语
        public virtual string ay { get; set; } = string.Empty; // 艾马拉语 (Aymara)
        public virtual string qu { get; set; } = string.Empty; // 克丘亚语 (Quechua)
        public virtual string gn { get; set; } = string.Empty; // 瓜拉尼语 (Guarani, 巴拉圭主流语言之一)

        // 非洲
        public virtual string af { get; set; } = string.Empty; // 南非荷语 (Afrikaans)
        public virtual string zu { get; set; } = string.Empty; // 祖鲁语 (Zulu)

        public virtual string ha { get; set; } = string.Empty; // 豪萨语 (西非大语种)
        public virtual string sw { get; set; } = string.Empty; // 斯瓦希里语 (东非通用)
        public virtual string am { get; set; } = string.Empty; // 阿姆哈拉语 (埃塞俄比亚官方)
        public virtual string ln { get; set; } = string.Empty; // 林加拉语 (刚果通用)
        public virtual string rw { get; set; } = string.Empty; // 卢旺达语
        public virtual string mg { get; set; } = string.Empty; // 马达加斯加语
        public virtual string sn { get; set; } = string.Empty; // 修纳语

        // 太平洋系
        public virtual string mi { get; set; } = string.Empty;   // 毛利语
        public virtual string fj { get; set; } = string.Empty;   // 斐济语
        public virtual string sm { get; set; } = string.Empty;   // 萨摩亚语
        public virtual string to { get; set; } = string.Empty;   // 汤加语
        public virtual string tpi { get; set; } = string.Empty;  // 托克皮辛语 (巴新通用语)
        public virtual string bi { get; set; } = string.Empty;   // 比斯拉马语
        public virtual string gil { get; set; } = string.Empty;  // 基里巴斯语
        public virtual string na { get; set; } = string.Empty;   // 瑙鲁语
        public virtual string tvl { get; set; } = string.Empty;  // 图瓦卢语
        public virtual string mh { get; set; } = string.Empty;   // 马绍尔语
        public virtual string pau { get; set; } = string.Empty;  // 帕劳语
        public virtual string rar { get; set; } = string.Empty;  // 库克群岛语
        public virtual string niu { get; set; } = string.Empty;  // 纽埃语
        public virtual string ty { get; set; } = string.Empty;   // 塔希提语
    }

    public static partial class 语言映射扩展类
    {
        /// <summary>
        /// 返回「属性名 + Text」
        /// </summary>
        public static List<(string, string)> 获取string元组列表(this 语言映射 self)
        {
            return self.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(string))
                .Select(p => (
                    p.Name,
                    (string)p.GetValue(self)!
                ))
                .ToList();
        }

        /// <summary>
        /// 根据<see cref="Attribute"/>, 返回「属性名 + Text」
        /// </summary>
        public static List<(string, string)> 获取string元组列表<T>(this 语言映射 self, bool 忽视元数据 = true) where T : Attribute
        {
            var properties_info = self.GetType()
              .GetProperties(BindingFlags.Instance | BindingFlags.Public);

            IEnumerable<PropertyInfo> enumerable = null;

            if (忽视元数据)
            {
                enumerable = properties_info.Where(p => p.PropertyType == typeof(string)
                        && p.GetCustomAttribute<T>() != null
                        && p.GetCustomAttribute<元数据Attribute>() == null);
            }
            else
            {
                enumerable = properties_info.Where(p => p.PropertyType == typeof(string) && p.GetCustomAttribute<T>() != null);

            }

            return enumerable.Select(p => (
                p.Name,
                (string)p.GetValue(self)!
            ))
            .ToList();
        }

        /// <summary>
        /// 根据<see cref="Attribute"/>排除之后, 再返回「属性名 + Text」,
        /// 可以用来排除类似于<see cref="未受良好支持Attribute"/>或其它自定义标签的语言。
        /// </summary>
        public static List<(string, string)> 获取string元组列表_排除<T>(this 语言映射 self, bool 忽视元数据 = true) where T : Attribute
        {
            var properties_info = self.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);

            IEnumerable<PropertyInfo> enumerable = null;
            if (忽视元数据)
            {
                enumerable = properties_info.Where(p => p.PropertyType == typeof(string)
                                   && p.GetCustomAttribute<T>() == null
                                   && p.GetCustomAttribute<元数据Attribute>() == null);
            }
            else
            {
                enumerable = properties_info.Where(p => p.PropertyType == typeof(string)
                            && p.GetCustomAttribute<T>() == null);
            }
            return enumerable.Select(p => (
                           p.Name,
                           (string)p.GetValue(self)!
                            ))
                            .ToList();
        }


        public static Dictionary<string, string> 获取非空string键值对(this 语言映射 self)
        {
            var dict = new Dictionary<string, string>();
            if (self == null)
                return dict;

            foreach (var p in self.GetType()
                                  .GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (p.PropertyType != typeof(string))
                    continue;

                var value = (string?)p.GetValue(self);
                if (!string.IsNullOrEmpty(value))
                {
                    dict[p.Name] = value;
                }
            }
            return dict;
        }

        /// <summary>
        /// 获取的该字典可以传入
        /// <see cref="String元组列表扩展类.RankByFirstCharOfKey"/> 或
        /// <see cref="String元组列表扩展类.RankByFirstCharOfValue"/>
        /// 从而对个别字符串元素的排序次序产生微调作用。
        /// 返回字典的 key 代表原内容, value代表 "排序替身"。
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> 获取排序订正符文映射字典(this 语言映射 self)
        {
            var dictionary = new Dictionary<string, string>();
            if (self == null) return dictionary;

            PropertyInfo[] properties = typeof(语言映射).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<排序订正符文Attribute>();
                if (attr == null)
                    continue;

                var originalValue = prop.GetValue(self)?.ToString();
                if (!string.IsNullOrEmpty(originalValue))
                {
                    if (!dictionary.ContainsKey(originalValue))
                    {
                        dictionary.Add(originalValue, attr.排序符文);
                    }
                }
            }
            return dictionary;
        }

        /// <summary>
        /// 返回各个语种的自我映射。比如 cn 对应简体中文, en 对应English。
        /// </summary>
        public static 语言映射 生成自我映射(this 语言映射 self)
        {

            self.theme = 翻译主题.自我映射.ToString();
            self.theme_id = (uint)翻译主题.自我映射;

            self.cn = "中文";
            self.cn_t = "華語";

            // 英语系
            self.en = "English";
            self.en_us = "English (United States)";
            self.en_au = "English (Australia)";
            self.en_ca = "English (Canada)";

            // 东亚
            self.ja = "日本語";
            self.ko = "한국어";
            self.mn = "монгол хэл";
            self.mn_t = "ᠮᠣᠩᠭᠣᠯ ᠬᠡᠯᠡ";

            // 欧洲
            self.de = "Deutsch";
            self.fr = "Français";
            self.es = "Español";
            self.it = "Italiano";
            self.pt = "Português";
            self.ru = "Русский";
            self.nl = "Nederlands";
            self.sv = "Svenska";
            self.no = "Norsk";
            self.da = "Dansk";
            self.fi = "Suomi";
            self.Is = "Íslenska";
            self.pl = "Polski";
            self.cs = "Čeština";
            self.hu = "Magyar";
            self.el = "Ελληνικά";
            self.uk = "Українська";
            self.ro = "Română";
            self.bg = "Български";
            self.sr = "Српски";
            self.hr = "Hrvatski";
            self.be = "Беларуская";
            self.lt = "Lietuvių";
            self.et = "Eesti";
            self.lv = "Latviešu";
            self.sk = "Slovenčina";
            self.mt = "Malti";
            self.ca = "Català";
            self.la = "Latina";
            self.sl = "Slovenščina";
            self.sq = "Shqip";
            self.mk = "Македонски";
            self.bs = "Bosanski";
            self.cnr = "Crnogorski";
            self.rm = "Rumantsch";
            self.lb = "Lëtzebuergesch";
            self.ga = "Gaeilge";
            self.tr = "Türkçe";

            // 西亚 / 中东
            self.ar = "العربية";
            self.fa = "فارسی";
            self.dr = "دری";
            self.ps = "پښتو";
            self.he = "עברית";
            self.ku = "Kurdî";
            self.hy = "Հայերեն";
            self.az = "Azərbaycan dili";

            // 南亚
            self.en_in = "English (India)";
            self.en_pk = "English (Pakistan)";
            self.ur = "اردو";
            self.hi = "हिन्दी";
            self.bn = "বাংলা";
            self.ne = "नेपाली";
            self.ta = "தமிழ்";
            self.dz = "རྫོང་ཁ";
            self.dv = "ދިވެހި";
            self.si = "සිංහල";


            // 印度特色
            self.ml = "മലയാളം";
            self.kn = "ಕನ್ನಡ";
            self.te = "తెలుగు";
            self.mr = "मराठी";
            self.gu = "ગુજરાતી";
            self.or = "ଓଡ଼ିଆ";
            self.pa = "ਪੰਜਾਬੀ";
            self.asm = "অসমীয়া";
            self.ks = "कॉशुर";
            self.mni = "মৈতৈলোন";
            self.sd = "سنڌي";
            self.bh = "भोजपुरी";
            self.kok = "कोंकणी";

            // 东南亚
            self.th = "ไทย";
            self.vi = "Tiếng Việt";
            self.id = "Bahasa Indonesia";
            self.ms = "Bahasa Melayu";
            self.tl = "Filipino";
            self.en_sgp = "English (Singapore)";
            self.cn_ny = "华语 (南洋)";
            self.my = "မြန်မာဘာသာ";
            self.km = "ភាសាខ្មែរ";
            self.lo = "ພາສາລາວ";

            // 中亚
            self.kk = "Қазақша";
            self.ru_kz = "Русский (Казахстан)";
            self.uz = "O‘zbek";
            self.ru_uz = "Русский (Узбекистан)";
            self.tk = "Türkmen";
            self.ru_tm = "Русский (Туркменистан)";
            self.ky = "Кыргызча";
            self.ru_kg = "Русский (Кыргызстан)";
            self.tg = "Тоҷикӣ";
            self.ru_tj = "Русский (Таджикистан)";

            // 拉美 - 西班牙语
            self.es_mx = "Español (México)";
            self.es_ar = "Español (Argentina)";
            self.es_co = "Español (Colombia)";
            self.es_cl = "Español (Chile)";
            self.es_pe = "Español (Perú)";
            self.es_ve = "Español (Venezuela)";
            self.es_ec = "Español (Ecuador)";
            self.es_gt = "Español (Guatemala)";
            self.es_cu = "Español (Cuba)";

            // 拉美 - 葡萄牙语
            self.pt_br = "Português (Brasil)";

            // 拉美 - 英语 / 法语
            self.en_jm = "English (Jamaica)";
            self.en_tt = "English (Trinidad and Tobago)";
            self.fr_ht = "Français (Haïti)";

            // 拉美特色
            self.ht = "Kreyòl Ayisyen";
            self.ay = "Aymar aru";
            self.qu = "Runa Simi";
            self.gn = "Avañe'ẽ";

            // 非洲
            self.af = "Afrikaans";
            self.zu = "isiZulu";
            self.ha = "Hausa";
            self.sw = "Kiswahili";
            self.am = "አማርኛ";
            self.ln = "Lingála";
            self.rw = "Ikinyarwanda";
            self.mg = "Malagasy";
            self.sn = "chiShona";

            // 太平洋系
            self.mi = "Māori";
            self.fj = "Vosa Vakaviti";
            self.sm = "Gagana Samoa";
            self.to = "lea fakatonga";
            self.tpi = "Tok Pisin";
            self.bi = "Bislama";
            self.gil = "Te taetae ni Kiribati";
            self.na = "dorerin Naoero";
            self.tvl = "Te Ggana Tuuvalu";
            self.mh = "Kajin M̧ajeļ";
            self.pau = "a tekoi er a Belau";
            self.rar = "Te Reo Māori Kūki 'Āirani";
            self.niu = "ko e vagahau Niuē";
            self.ty = "Reo Tahiti";

            return self;
        }

        public static 语言映射 生成中文映射(this 语言映射 self)
        {

            self.theme_id = (uint)翻译主题.中文映射;
            self.theme = 翻译主题.中文映射.ToString();

            // 中文系
            self.cn = "中文";
            self.cn_t = "華語";

            // 昂撒英语系       
            self.en = "英语";
            self.en_us = "英语(美国)";
            self.en_au = "英语(澳大利亚)";
            self.en_ca = "英语(加拿大)";

            // 东亚系      
            self.ja = "日语";
            self.ko = "韩语";
            self.mn = "蒙古语";
            self.mn_t = "传统蒙古语";

            // 欧洲语言        
            self.de = "德语";
            self.fr = "法语";
            self.es = "西班牙语";
            self.it = "意大利语";
            self.pt = "葡萄牙语";
            self.ru = "俄语";
            self.nl = "荷兰语";
            self.sv = "瑞典语";
            self.no = "挪威语";
            self.da = "丹麦语";
            self.fi = "芬兰语";
            self.Is = "冰岛语";
            self.pl = "波兰语";
            self.cs = "捷克语";
            self.hu = "匈牙利语";
            self.el = "希腊语";
            self.uk = "乌克兰语";
            self.ro = "罗马尼亚语";
            self.bg = "保加利亚语";
            self.sr = "塞尔维亚语";
            self.hr = "克罗地亚语";
            self.be = "白俄罗斯语";
            self.lt = "立陶宛语";
            self.et = "爱沙尼亚语";
            self.lv = "拉脱维亚语";
            self.sk = "斯洛伐克语";
            self.mt = "马耳他语";
            self.ca = "加泰罗尼亚语";
            self.la = "拉丁语";
            self.sl = "斯洛文尼亚语";
            self.sq = "阿尔巴尼亚语";
            self.mk = "马其顿语";
            self.bs = "波斯尼亚语";
            self.cnr = "黑山语";
            self.rm = "罗曼什语";
            self.lb = "卢森堡语";
            self.ga = "爱尔兰语";
            self.tr = "土耳其语"; // 土耳其虽然主要在西亚, 但他们自认为属于欧洲

            // 西亚(中东)        
            self.ar = "阿拉伯语";
            self.fa = "波斯语";
            self.dr = "达里波斯语";
            self.ps = "普什图语";
            self.he = "希伯来语";
            self.ku = "库尔德语";      // 伊拉克、叙利亚部分
            self.hy = "亚美尼亚语";    // 亚美尼亚
            self.az = "阿塞拜疆语";    // 阿塞拜疆

            // 南亚
            self.en_in = "英语(印度)";
            self.en_pk = "英语(巴基斯坦)";
            self.ur = "乌尔都语";  // 巴基斯坦
            self.hi = "印地语";
            self.bn = "孟加拉语";
            self.ne = "尼泊尔语";
            self.ta = "泰米尔语";
            self.dz = "宗卡语";   // 不丹
            self.dv = "迪维希语";   // 马尔代夫
            self.si = "僧伽罗语";  // 斯里兰卡

            // 印度特色    
            self.ml = "马拉雅拉姆语";
            self.kn = "卡纳达语";
            self.te = "泰卢固语";
            self.mr = "马拉地语";
            self.gu = "古吉拉特语";
            self.or = "奥里亚语";
            self.pa = "旁遮普语";
            self.asm = "阿萨姆语";
            self.ks = "卡什米尔语";
            self.mni = "马尼普尔语";
            self.sd = "信德语";
            self.bh = "博杰普尔语";
            self.kok = "康卡尼语";

            // 东南亚
            self.th = "泰语";
            self.vi = "越南语";
            self.id = "印尼语";
            self.ms = "马来语";
            self.tl = "菲律宾语";
            self.en_sgp = "英语 (新加坡)";
            self.cn_ny = "华语 (南洋)";
            self.my = "缅甸语";
            self.km = "高棉语";  //（柬埔寨语）
            self.lo = "老挝语";

            // 中亚
            self.kk = "哈萨克语";
            self.ru_kz = "俄语(哈萨克斯坦)";
            self.uz = "乌兹别克语";
            self.ru_uz = "俄语(乌兹别克斯坦)";
            self.tk = "土库曼语";
            self.ru_tm = "俄语(土库曼斯坦)";
            self.ky = "吉尔吉斯语";
            self.ru_kg = "俄语(吉尔吉斯斯坦)";
            self.tg = "塔吉克语";
            self.ru_tj = "俄语(塔吉克斯坦)";

            // 拉美 - 西班牙语系

            self.es_mx = "西班牙语(墨西哥)";
            self.es_ar = "西班牙语(阿根廷)";
            self.es_co = "西班牙语(哥伦比亚)";
            self.es_cl = "西班牙语(智利)";
            self.es_pe = "西班牙语(秘鲁)";
            self.es_ve = "西班牙语(委内瑞拉)";
            self.es_ec = "西班牙语(厄瓜多尔)";
            self.es_gt = "西班牙语(危地马拉)";
            self.es_cu = "西班牙语(古巴)";

            // 拉美 - 葡萄牙语     
            self.pt_br = "葡萄牙语(巴西)";

            // 拉美 - 英语 / 法语（加勒比部分）      
            self.en_jm = "英语(牙买加)";
            self.en_tt = "英语(特立尼达和多巴哥)";
            self.fr_ht = "法语(海地)";

            // 拉美特色
            self.ht = "海地克里奥尔语";
            self.ay = "艾马拉语";
            self.qu = "克丘亚语";
            self.gn = "瓜拉尼语";

            // 非洲
            self.af = "南非荷语";
            self.zu = "祖鲁语";
            self.ha = "豪萨语";
            self.sw = "斯瓦希里语";
            self.am = "阿姆哈拉语";
            self.ln = "林加拉语";
            self.rw = "卢旺达语";
            self.mg = "马达加斯加语";
            self.sn = "修纳语";

            // 太平洋系
            self.mi = "毛利语";
            self.fj = "斐济语";
            self.sm = "萨摩亚语";
            self.to = "汤加语";
            self.tpi = "托克皮辛语";
            self.bi = "比斯拉马语";
            self.gil = "基里巴斯语";
            self.na = "瑙鲁语";
            self.tvl = "图瓦卢语";
            self.mh = "马绍尔语";
            self.pau = "帕劳语";
            self.rar = "库克群岛语";
            self.niu = "纽埃语";
            self.ty = "塔希提语";

            return self;
        }
    }
}