namespace Fantasy.Product.Solutions
{
    public static class LongTime
    {
        // 基础
        public const long one_second = 1000;
        public const long one_minute = 60 * one_second;
        public const long one_hour = 60 * one_minute;
        public const long one_day = 24 * one_hour;

        // 分钟
        public const long five_mins = 5 * one_minute;
        public const long ten_mins = 10 * one_minute;
        public const long fifteen_mins = 15 * one_minute;
        public const long thirty_mins = 30 * one_minute;

        // 小时
        public const long two_hours = 2 * one_hour;
        public const long three_hours = 3 * one_hour;
        public const long six_hours = 6 * one_hour;
        public const long twelve_hours = 12 * one_hour;

        // 天
        public const long two_days = 2 * one_day;
        public const long three_days = 3 * one_day;
        public const long five_days = 5 * one_day;
        public const long one_week = 7 * one_day;
        public const long one_month = 30 * one_day;   // 近似值
    }

}
