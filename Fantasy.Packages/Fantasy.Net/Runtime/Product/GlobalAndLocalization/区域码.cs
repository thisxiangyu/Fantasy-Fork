using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#pragma warning disable CS8625
#pragma warning disable CS8632

namespace Fantasy.GlobalAndLocalization
{
    /// <summary>
    /// 发行区码
    /// </summary>
    public enum 区域码 : uint
    {
        Unknown = 0,

        [大区(东亚)]
        东亚 = 100000,

        [大区(东亚)]
        [语言码(nameof(语言映射.cn))]
        [简写("CN")]
        中国 = 86,
        [大区(港澳台澎金马)]
        [语言码(nameof(语言映射.cn_t))]
        [简写("HK")]
        香港CN = 852,
        [大区(港澳台澎金马)]
        [语言码(nameof(语言映射.cn_t))]
        [简写("MO")]
        澳门CN = 853,
        [大区(港澳台澎金马)]
        [语言码(nameof(语言映射.cn_t))]
        [简写("TW")]
        台湾CN = 886,
        [大区(东亚)]
        [语言码(nameof(语言映射.ja))]
        [简写("JP")]
        日本 = 81,
        [大区(东亚)]
        [语言码(nameof(语言映射.ko))]
        [简写("KR")]
        韩国 = 82,
        [大区(东亚)]
        [语言码(nameof(语言映射.ko))]
        [简写("KP")]
        朝鲜 = 850,
        [大区(东亚)]
        [语言码(nameof(语言映射.mn))]
        [语言码(nameof(语言映射.mn_t), 1)]
        [简写("MN")]
        蒙古 = 976,

        [大区(东南亚)]
        东南亚 = 100001,

        [大区(东南亚)]
        [语言码(nameof(语言映射.ms))]
        [语言码(nameof(语言映射.en), 1)]
        [语言码(nameof(语言映射.cn_ny), 2)]
        [简写("MY")]
        马来西亚 = 60,
        [大区(东南亚)]
        [语言码(nameof(语言映射.id))]
        [简写("ID")]
        印尼 = 62,
        [大区(东南亚)]
        [语言码(nameof(语言映射.vi))]
        [简写("VN")]
        越南 = 84,
        [大区(东南亚)]
        [语言码(nameof(语言映射.tl))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("PH")]
        菲律宾 = 63,
        [大区(东南亚)]
        [语言码(nameof(语言映射.th))]
        [简写("TH")]
        泰国 = 66,
        [大区(东南亚)]
        [语言码(nameof(语言映射.en_sgp))]
        [语言码(nameof(语言映射.cn_ny), 2)]
        [语言码(nameof(语言映射.ms), 3)]
        [语言码(nameof(语言映射.ta), 4)]
        [简写("SG")]
        新加坡 = 65,
        [大区(东南亚)]
        [语言码(nameof(语言映射.my))]
        [简写("MM")]
        缅甸 = 95,
        [大区(东南亚)]
        [语言码(nameof(语言映射.km))]
        [简写("KH")]
        柬埔寨 = 855,
        [大区(东南亚)]
        [语言码(nameof(语言映射.lo))]
        [简写("LA")]
        老挝 = 856,
        [大区(东南亚)]
        [语言码(nameof(语言映射.ms))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("BN")]
        文莱 = 673,
        [大区(东南亚)]
        [语言码(nameof(语言映射.pt))]
        [语言码(nameof(语言映射.id), 1)]
        [简写("TL")]
        东帝汶 = 670,

        [大区(西亚)]
        西亚 = 100002,

        [大区(西亚)]
        [语言码(nameof(语言映射.tr))]
        [简写("TR")]
        土耳其 = 90,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [简写("SA")]
        沙特 = 966,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar), 1)]
        [语言码(nameof(语言映射.en), 1)]
        [简写("AE")]
        阿联酋 = 971,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("EG")]
        埃及 = 20,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [简写("QA")]
        卡塔尔 = 974,
        [大区(西亚)]
        [语言码(nameof(语言映射.he))]
        [语言码(nameof(语言映射.ar), 1)]
        [语言码(nameof(语言映射.en), 2)]
        [简写("IL")]
        以色列 = 972,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.en), 2)]
        [简写("KW")]
        科威特 = 965,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [简写("BH")]
        巴林 = 973,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.fr), 2)]
        [简写("LB")]
        黎巴嫩 = 961,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [简写("OM")]
        阿曼 = 968,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [简写("JO")]
        约旦 = 962,
        [大区(西亚)]
        [语言码(nameof(语言映射.fa))]
        [简写("IR")]
        伊朗 = 98,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.ku), 2)]
        [简写("IQ")]
        伊拉克 = 964,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.ku), 2)]
        [简写("SY")]
        叙利亚 = 963,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [简写("YE")]
        也门 = 967,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [简写("PS")]
        巴勒斯坦 = 970,
        [大区(西亚)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.en), 2)]
        [简写("SD")]
        苏丹 = 249,

        [大区(高加索地区)]
        [语言码(nameof(语言映射.ka))]
        [简写("GE")]
        格鲁吉亚 = 995,
        [大区(高加索地区)]
        [语言码(nameof(语言映射.az))]
        [简写("AZ")]
        阿塞拜疆 = 994,
        [大区(高加索地区)]
        [语言码(nameof(语言映射.hy))]
        [简写("AM")]
        亚美尼亚 = 374,

        [大区(南亚)]
        南亚 = 100003,

        [大区(印度)]
        [语言码(nameof(语言映射.hi))]
        [语言码(nameof(语言映射.hi_ro))]
        [语言码(nameof(语言映射.en_in))]
        [语言码(nameof(语言映射.bn))]
        [语言码(nameof(语言映射.bn_ro))]
        [语言码(nameof(语言映射.te))]
        [语言码(nameof(语言映射.mr))]
        [语言码(nameof(语言映射.ta))]
        [语言码(nameof(语言映射.gu))]
        [语言码(nameof(语言映射.kn))]
        [语言码(nameof(语言映射.ml))]
        [语言码(nameof(语言映射.pa))]
        [语言码(nameof(语言映射.or))]
        [语言码(nameof(语言映射.asm))]
        [语言码(nameof(语言映射.ks))]
        [语言码(nameof(语言映射.mni))]
        [语言码(nameof(语言映射.sd))]
        [语言码(nameof(语言映射.kok))]
        [语言码(nameof(语言映射.ur))]
        [语言码(nameof(语言映射.ur_ro))]
        [简写("IN")]
        印度 = 91,
        [大区(南亚)]
        [语言码(nameof(语言映射.ur))]
        [语言码(nameof(语言映射.ur_ro))]
        [语言码(nameof(语言映射.en), 1)]
        [语言码(nameof(语言映射.pa), 2)]
        [简写("PK")]
        巴基斯坦 = 92,
        [大区(南亚)]
        [语言码(nameof(语言映射.fa))]
        [语言码(nameof(语言映射.ps), 1)]
        [简写("AF")]
        阿富汗 = 93,
        [大区(南亚)]
        [语言码(nameof(语言映射.bn))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("BD")]
        孟加拉国 = 880,
        [大区(南亚)]
        [语言码(nameof(语言映射.dz))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("BT")]
        不丹 = 975,
        [大区(南亚)]
        [语言码(nameof(语言映射.dv))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("MV")]
        马尔代夫 = 960,
        [大区(南亚)]
        [语言码(nameof(语言映射.ne))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("NP")]
        尼泊尔 = 977,
        [大区(南亚)]
        [语言码(nameof(语言映射.si))]
        [语言码(nameof(语言映射.ta), 1)]
        [语言码(nameof(语言映射.en), 2)]
        [简写("LK")]
        斯里兰卡 = 94,

        [大区(中亚)]
        中亚 = 100004,

        [大区(中亚)]
        [共用区码(俄罗斯)]
        [语言码(nameof(语言映射.kk))]
        [语言码(nameof(语言映射.ru_kz), 1)]
        [简写("KZ")]
        哈萨克斯坦 = 7,
        [大区(中亚)]
        [语言码(nameof(语言映射.ky))]
        [语言码(nameof(语言映射.ru_kg), 1)]
        [简写("KG")]
        吉尔吉斯斯坦 = 996,
        [大区(中亚)]
        [语言码(nameof(语言映射.tg))]
        [语言码(nameof(语言映射.ru_tj), 1)]
        [简写("TJ")]
        塔吉克斯坦 = 992,
        [大区(中亚)]
        [语言码(nameof(语言映射.tk))]
        [语言码(nameof(语言映射.ru_tm), 1)]
        [简写("TM")]
        土库曼斯坦 = 993,
        [大区(中亚)]
        [语言码(nameof(语言映射.uz))]
        [语言码(nameof(语言映射.ru_uz), 1)]
        [简写("UZ")]
        乌兹别克斯坦 = 998,

        [大区(非洲)]
        马格里布地区 = 100005,

        [大区(非洲)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.fr), 1)]
        [简写("DZ")]
        阿尔及利亚 = 213,
        [大区(非洲)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("LY")]
        利比亚 = 218,
        [大区(非洲)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.fr), 1)]
        [简写("MA")]
        摩洛哥 = 212,
        [大区(非洲)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.fr), 1)]
        [简写("TN")]
        突尼斯 = 216,
        [大区(非洲)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.fr), 1)]
        [简写("MR")]
        毛里塔尼亚 = 222,

        [大区(非洲)]
        撒哈拉以南 = 100006,

        [大区(非洲)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.af), 1)]
        [语言码(nameof(语言映射.zu), 2)]
        [简写("ZA")]
        南非 = 27,
        [大区(非洲)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.ha), 1)]
        [简写("NG")]
        尼日利亚 = 234,
        [大区(非洲)]
        [语言码(nameof(语言映射.sw))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("KE")]
        肯尼亚 = 254,
        [大区(非洲)]
        [语言码(nameof(语言映射.ar))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("SO")]
        索马里 = 252,
        [大区(非洲)]
        [语言码(nameof(语言映射.am))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("ET")]
        埃塞俄比亚 = 251,
        [大区(非洲)]
        [语言码(nameof(语言映射.en))]
        [简写("GH")]
        加纳 = 233,
        [大区(非洲)]
        [语言码(nameof(语言映射.fr))]
        [简写("CI")]
        科特迪瓦 = 225,
        [大区(非洲)]
        [语言码(nameof(语言映射.pt))]
        [简写("AO")]
        安哥拉 = 244,
        [大区(非洲)]
        [语言码(nameof(语言映射.sw))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("TZ")]
        坦桑尼亚 = 255,
        [大区(非洲)]
        [语言码(nameof(语言映射.fr))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("CM")]
        喀麦隆 = 237,
        [大区(非洲)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.sw), 1)]
        [简写("UG")]
        乌干达 = 256,
        [大区(非洲)]
        [语言码(nameof(语言映射.fr))]
        [简写("SN")]
        塞内加尔 = 221,
        [大区(非洲)]
        [语言码(nameof(语言映射.fr))]
        [语言码(nameof(语言映射.ln), 1)]
        [简写("CD")]
        刚果 = 243,
        [大区(非洲)]
        [语言码(nameof(语言映射.rw))]
        [语言码(nameof(语言映射.en), 1)]
        [语言码(nameof(语言映射.fr), 2)]
        [简写("RW")]
        卢旺达 = 250,
        [大区(非洲)]
        [语言码(nameof(语言映射.mg))]
        [语言码(nameof(语言映射.fr), 1)]
        [简写("MG")]
        马达加斯加 = 261,
        [大区(非洲)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.sn), 1)]
        [简写("ZW")]
        津巴布韦 = 263,
        [大区(非洲)]
        [语言码(nameof(语言映射.en))]
        [简写("ZM")]
        赞比亚 = 260,

        [大区(北美)]
        北美 = 100007,

        [大区(北美)]
        [语言码(nameof(语言映射.en_us))]
        [语言码(nameof(语言映射.es), 1)] // 美国有极大量的西班牙语人口
        [简写("US")]
        美国 = 1,
        [大区(北美)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en_ca))]
        [语言码(nameof(语言映射.fr), 1)] // 加拿大官方双语
        [简写("CA")]
        加拿大 = 1,

        [大区(太平洋大区)]
        太平洋大区 = 100008,

        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en_au))]
        [简写("AU")]
        澳大利亚 = 61,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.mi), 1)]
        [简写("NZ")]
        新西兰 = 64,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.fj), 1)]
        [语言码(nameof(语言映射.hi), 2)]
        [简写("FJ")]
        斐济 = 679,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.sm), 1)]
        [简写("WS")]
        萨摩亚 = 685,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.to), 1)]
        [简写("TO")]
        汤加 = 676,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.tpi), 1)]
        [简写("PG")]
        巴布亚新几内亚 = 675,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.fr), 1)]
        [语言码(nameof(语言映射.bi), 2)]
        [简写("VU")]
        瓦努阿图 = 678,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [简写("FM")]
        密克罗尼西亚联邦 = 691,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [简写("SB")]
        所罗门群岛 = 677,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.gil), 1)]
        [简写("KI")]
        基里巴斯 = 686,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.na), 1)]
        [简写("NR")]
        瑙鲁 = 674,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.tvl), 1)]
        [简写("TV")]
        图瓦卢 = 688,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.mh), 1)]
        [简写("MH")]
        马绍尔群岛 = 692,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.pau), 1)]
        [简写("PW")]
        帕劳 = 680,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.rar), 1)]
        [简写("CK")]
        库克群岛 = 682,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.niu), 1)]
        [简写("NU")]
        纽埃 = 683,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.fr))]
        [语言码(nameof(语言映射.ty), 1)]
        [简写("PF")]
        法属波利尼西亚 = 689,
        [大区(太平洋大区)]
        [语言码(nameof(语言映射.fr))]
        [简写("NC")]
        法属新喀里多尼亚 = 687,

        [大区(加勒比地区)]
        加勒比地区 = 10009,

        [大区(加勒比地区)]
        [语言码(nameof(语言映射.es_cu))]
        [简写("CU")]
        古巴 = 53,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("AG")]
        安提瓜和巴布达 = 1,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("BS")]
        巴哈马 = 1,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("BB")]
        巴巴多斯 = 1,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.es), 1)]
        [简写("BZ")]
        伯利兹 = 501,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.es))]
        [简写("CR")]
        哥斯达黎加 = 506,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("DM")]
        多米尼克 = 1,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.es))]
        [简写("DO")]
        多米尼加 = 1,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.es))]
        [简写("SV")]
        萨尔瓦多 = 503,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("GD")]
        格林纳达 = 1,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.es_gt))]
        [简写("GT")]
        危地马拉 = 502,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.en))]
        [简写("GY")]
        圭亚那 = 592,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.fr_ht))]
        [语言码(nameof(语言映射.ht), 1)]
        [简写("HT")]
        海地 = 509,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.es))]
        [简写("HN")]
        洪都拉斯 = 504,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en_jm))]
        [简写("JM")]
        牙买加 = 1,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.es))]
        [简写("NI")]
        尼加拉瓜 = 505,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.es))]
        [简写("PA")]
        巴拿马 = 507,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("KN")]
        圣基茨和尼维斯 = 1,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("VC")]
        圣文森特和格林纳丁斯 = 1,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en))]
        [简写("LC")]
        圣卢西亚 = 1,
        [大区(加勒比地区)]
        [语言码(nameof(语言映射.nl))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("SR")]
        苏里南 = 597,
        [大区(加勒比地区)]
        [共用区码(美国)]
        [语言码(nameof(语言映射.en_tt))]
        [简写("TT")]
        特立尼达和多巴哥双岛 = 1,

        [大区(拉丁美洲)]
        拉丁美洲 = 100010,

        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es_mx))]
        [简写("MX")]
        墨西哥 = 52,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es_co))]
        [简写("CO")]
        哥伦比亚 = 57,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.pt_br))]
        [简写("BR")]
        巴西 = 55,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es_ar))]
        [简写("AR")]
        阿根廷 = 54,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es_cl))]
        [简写("CL")]
        智利 = 56,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es_pe))]
        [简写("PE")]
        秘鲁 = 51,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es_ve))]
        [简写("VE")]
        委内瑞拉 = 58,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es_ec))]
        [简写("EC")]
        厄瓜多尔 = 593,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es))]
        [语言码(nameof(语言映射.ay), 1)]
        [语言码(nameof(语言映射.qu), 2)]
        [简写("BO")]
        玻利维亚 = 591,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es))]
        [语言码(nameof(语言映射.gn), 1)] // 瓜拉尼语 (巴拉圭官方双语)
        [简写("PY")]
        巴拉圭 = 595,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.es))]
        [简写("UY")]
        乌拉圭 = 598,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.fr))]
        [简写("GF")]
        法属圭亚那 = 594,
        [大区(拉丁美洲)]
        [语言码(nameof(语言映射.en))]
        [简写("FK")]
        福克兰群岛 = 500,

        [大区(欧洲)]
        北欧五国 = 100011,

        [大区(欧洲)]
        [语言码(nameof(语言映射.da))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("DK")]
        丹麦 = 45,

        [大区(欧洲)]
        [语言码(nameof(语言映射.fi))]
        [语言码(nameof(语言映射.sv), 1)]
        [简写("FI")]
        芬兰 = 358,

        [大区(欧洲)]
        [语言码(nameof(语言映射.Is))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("IS")]
        冰岛 = 354,

        [大区(欧洲)]
        [语言码(nameof(语言映射.no))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("NO")]
        挪威 = 47,

        [大区(欧洲)]
        [语言码(nameof(语言映射.sv))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("SE")]
        瑞典 = 46,

        [大区(欧洲)]
        俄罗斯地区 = 100012,

        [大区(欧洲)]
        [语言码(nameof(语言映射.ru))]
        [简写("RU")]
        俄罗斯 = 7,
        [大区(欧洲)]
        [语言码(nameof(语言映射.ru))]
        [语言码(nameof(语言映射.be), 1)]
        [简写("BY")]
        白俄罗斯 = 375,

        [大区(欧洲)]
        东欧 = 100013,

        [大区(欧洲)]
        [语言码(nameof(语言映射.uk))]
        [语言码(nameof(语言映射.ru), 1)]
        [简写("UA")]
        乌克兰 = 380,

        [大区(欧洲)]
        [语言码(nameof(语言映射.pl))]
        [简写("PL")]
        波兰 = 48,

        [大区(欧洲)]
        [语言码(nameof(语言映射.lt))]
        [语言码(nameof(语言映射.ru), 1)]
        [简写("LT")]
        立陶宛 = 370,

        [大区(欧洲)]
        [语言码(nameof(语言映射.et))]
        [语言码(nameof(语言映射.ru), 1)]
        [简写("EE")]
        爱沙尼亚 = 372,

        [大区(欧洲)]
        [语言码(nameof(语言映射.lv))]
        [语言码(nameof(语言映射.ru), 1)]
        [简写("LV")]
        拉脱维亚 = 371,

        [大区(欧洲)]
        [语言码(nameof(语言映射.cs))]
        [简写("CZ")]
        捷克 = 420,

        [大区(欧洲)]
        [语言码(nameof(语言映射.sk))]
        [语言码(nameof(语言映射.cs), 1)] // 斯洛伐克人普遍能无障碍听懂/阅读捷克语
        [简写("SK")]
        斯洛伐克 = 421,
        [大区(欧洲)]
        [语言码(nameof(语言映射.hu))]
        [简写("HU")]
        匈牙利 = 36,

        [大区(欧洲)]
        欧洲地中海地区 = 100014,

        [大区(欧洲)]
        [语言码(nameof(语言映射.it))]
        [简写("IT")]
        意大利 = 39,

        [大区(欧洲)]
        [语言码(nameof(语言映射.es))]
        [语言码(nameof(语言映射.ca), 1)] // 西班牙语为主，加泰罗尼亚语为重要区域语
        [简写("ES")]
        西班牙 = 34,

        [大区(欧洲)]
        [语言码(nameof(语言映射.pt))]
        [简写("PT")]
        葡萄牙 = 351,

        [大区(欧洲)]
        [语言码(nameof(语言映射.el))]
        [简写("GR")]
        希腊 = 30,

        [大区(欧洲)]
        [语言码(nameof(语言映射.mt))]
        [语言码(nameof(语言映射.en), 1)] // 马耳他语和英语均为官方语言
        [简写("MT")]
        马耳他 = 356,

        [大区(欧洲)]
        [语言码(nameof(语言映射.el))]
        [语言码(nameof(语言映射.tr), 1)] // 塞浦路斯包含希腊裔和土耳其裔
        [语言码(nameof(语言映射.en), 2)] // 英语作为前殖民地通用语
        [简写("CY")]
        塞浦路斯 = 357,

        [大区(欧洲)]
        巴尔干地区 = 100015,

        [大区(欧洲)]
        [语言码(nameof(语言映射.ca))]
        [语言码(nameof(语言映射.es), 1)]
        [语言码(nameof(语言映射.fr), 2)] // 安道尔官方语言为加泰语，但通晓西、法文
        [简写("AD")]
        安道尔 = 376,

        [大区(欧洲)]
        [共用区码(意大利)]
        [语言码(nameof(语言映射.it))]
        [语言码(nameof(语言映射.la), 1)]
        [简写("VA")]
        梵蒂冈 = 39,

        [大区(欧洲)]
        [语言码(nameof(语言映射.it))]
        [简写("SM")]
        圣马力诺 = 378,

        [大区(欧洲)]
        [语言码(nameof(语言映射.sl))]
        [简写("SI")]
        斯洛文尼亚 = 386,

        [大区(欧洲)]
        [语言码(nameof(语言映射.hr))]
        [简写("HR")]
        克罗地亚 = 385,

        [大区(欧洲)]
        [语言码(nameof(语言映射.sq))]
        [简写("AL")]
        阿尔巴尼亚 = 355,

        [大区(欧洲)]
        [语言码(nameof(语言映射.ro))]
        [简写("RO")]
        罗马尼亚 = 40,

        [大区(欧洲)]
        [语言码(nameof(语言映射.bg))]
        [简写("BG")]
        保加利亚 = 359,

        [大区(欧洲)]
        [语言码(nameof(语言映射.sr))]
        [简写("RS")]
        塞尔维亚 = 381,

        [大区(欧洲)]
        [语言码(nameof(语言映射.sr))]
        [语言码(nameof(语言映射.cnr), 1)] // 黑山官方语言现称为黑山语，但与塞尔维亚语高度一致
        [简写("ME")]
        黑山 = 382,

        [大区(欧洲)]
        [语言码(nameof(语言映射.mk))]
        [简写("MK")]
        北马其顿 = 389,

        [大区(欧洲)]
        [语言码(nameof(语言映射.bs))]
        [语言码(nameof(语言映射.hr), 1)]
        [语言码(nameof(语言映射.sr), 2)] // 波黑官方认定三种语言，实则互通
        [简写("BA")]
        波斯尼亚和黑塞哥维那 = 387,

        [大区(欧洲)]
        西欧 = 100016,

        [大区(欧洲)]
        [语言码(nameof(语言映射.fr))]
        [简写("FR")]
        法国 = 33,
        [大区(欧洲)]
        [语言码(nameof(语言映射.de))]
        [简写("DE")]
        德国 = 49,
        [大区(欧洲)]
        [语言码(nameof(语言映射.de))]
        [简写("AT")]
        奥地利 = 43,
        [大区(欧洲)]
        [语言码(nameof(语言映射.nl))]
        [语言码(nameof(语言映射.en), 1)]
        [简写("NL")]
        荷兰 = 31,

        [大区(欧洲)]
        [语言码(nameof(语言映射.de))] // 德语使用者约占 63%
        [语言码(nameof(语言映射.fr), 1)] // 法语使用者约占 23%
        [语言码(nameof(语言映射.it), 2)] // 意大利语使用者约占 8%
        [语言码(nameof(语言映射.rm), 3)] // 罗曼什语为第四官方语，比例极小
        [简写("CH")]
        瑞士 = 41,

        [大区(欧洲)]
        [语言码(nameof(语言映射.nl))] // 弗拉芒语（荷兰语变体）占比最高
        [语言码(nameof(语言映射.fr), 1)] // 瓦隆语（法语变体）次之
        [语言码(nameof(语言映射.de), 2)] // 德语为少数官方语
        [简写("BE")]
        比利时 = 32,

        [大区(欧洲)]
        [语言码(nameof(语言映射.lb))] // 卢森堡语
        [语言码(nameof(语言映射.fr), 1)] // 法语为行政办公主语言
        [语言码(nameof(语言映射.de), 2)] // 德语
        [简写("LU")]
        卢森堡 = 352,

        [大区(欧洲)]
        [语言码(nameof(语言映射.de))]
        [简写("LI")]
        列支敦士登 = 423,

        [大区(欧洲)]
        [语言码(nameof(语言映射.fr))]
        [简写("MC")]
        摩纳哥 = 377,

        [大区(欧洲)]
        英伦地区 = 100017,

        [大区(欧洲)]
        [语言码(nameof(语言映射.en))]
        [简写("GB")]
        英国 = 44,

        [大区(欧洲)]
        [语言码(nameof(语言映射.en))]
        [语言码(nameof(语言映射.ga), 1)] // 爱尔兰语为官方第一语言，但英语是事实上的通用语
        [简写("IE")]
        爱尔兰 = 353,

        // 这几个都分子大区 (比如亚洲包括东亚、西亚等大区), 所以单独拎出来
        亚洲 = 110000,
        非洲 = 120000,
        欧洲 = 130000,

        [特殊标记]
        高加索地区 = 999997, // 这里从文化归属上与西亚和中亚都不同, 文化较为独立
        [特殊标记]
        港澳台澎金马 = 999998,
        [特殊标记]
        国际 = 999999,
    }

    /// <summary>
    /// 这个类提供获取区域码信息的辅助方法
    /// </summary>
    public static class 区域码信息
    {
        static List<(string 区码简写, string 语言码)> _area_lang_tuples = null;
        static List<(区域码 大区, string 简写, string 枚举名)> _region_area_number_tuples_enumName = null;

        /// <summary>
        /// 生成 List<(string 简写, string 语言码)>, 
        /// 只处理带 [语言码] 的元素，支持一国对多语。
        /// </summary>
        public static List<(string 区码简写, string 语言码)> 获取所有简写With语言码元组List(bool update_cache = false)
        {
            if (_area_lang_tuples != null && !update_cache)
                return _area_lang_tuples;

            _area_lang_tuples = new List<(string 区码简写, string 语言码)>();
            var type = typeof(区域码);

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var langAttrs = field.GetCustomAttributes<语言码Attribute>().ToList();

                if (langAttrs.Count == 0)
                    continue;

                var shortNameAttr = field.GetCustomAttribute<简写Attribute>();

                if (shortNameAttr == null)
                {
                    Log.Error($"元素 {field.Name} 缺少 [简写] 特性，已跳过。请务必补充。");
                    continue;
                }

                foreach (var lang in langAttrs)
                {
                    _area_lang_tuples.Add((shortNameAttr.简写, lang.语言码));
                }
            }
            return _area_lang_tuples;
        }

        /// <summary>
        /// 生成 List<(区域码 大区, string 简写, 区域码 区码)>
        /// 注意：如果一个国家有多个语言码，该方法会根据 [语言码] 标记的顺序，为每个语言关联生成对应的元组条目
        /// </summary>
        public static List<(区域码 大区, string 区码简写, string 区码枚举名)> 获取大区With简写With区码元组List(bool update_cache = false)
        {
            if (_region_area_number_tuples_enumName != null && !update_cache)
                return _region_area_number_tuples_enumName;

            _region_area_number_tuples_enumName = new List<(区域码 大区, string 简写, string 枚举名)>();
            var type = typeof(区域码);

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                // 获取该字段上所有的语言码特性，并严格按照你在构造函数里传入的“顺序”字段排序
                var langAttrs = field.GetCustomAttributes<语言码Attribute>()
                                     .OrderBy(l => l.语言顺序)
                                     .ToList();

                if (langAttrs.Count == 0) continue;

                var shortNameAttr = field.GetCustomAttribute<简写Attribute>();
                var regionAttr = field.GetCustomAttribute<大区Attribute>();

                if (shortNameAttr == null || regionAttr == null)
                {
                    Log.Error($"元素 {field.Name} 缺少 [简写] 或 [大区] 特性，已跳过。");
                    continue;
                }

                // 枚举名
                var enumName = field.Name;

                // 如果一个国家有多个语言（如瑞士），
                // 按照排序后的语言顺序，将该国家信息存入列表
                foreach (var lang in langAttrs)
                {
                    _region_area_number_tuples_enumName.Add((regionAttr.大区, shortNameAttr.简写, enumName));
                }
            }
            return _region_area_number_tuples_enumName;
        }

        public static string? 根据区码枚举名获取为首的语言码(this string 区域枚举名)
        {
            var type = typeof(区域码);

            // 通过枚举名字获取 FieldInfo
            var field = type.GetField(区域枚举名, BindingFlags.Public | BindingFlags.Static);
            if (field == null)
                return null;

            // 获取该字段上的所有语言码特性
            var langAttrs = field.GetCustomAttributes<语言码Attribute>(false);
            if (!langAttrs.Any())
                return null;

            // 优先取顺序为 0 的语言码
            var first = langAttrs.FirstOrDefault(a => a.语言顺序 == 0)
                     ?? langAttrs.OrderBy(a => a.语言顺序).First();

            return first.语言码;
        }
    }
}