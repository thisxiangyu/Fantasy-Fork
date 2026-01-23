namespace Fantasy.Product.Solutions
{
    public class PhoneHelper
    {
#if FANTASY_NET
        /// <summary>
        /// 请求发送手机号验证码 (Server端发出请求)
        /// </summary>
        /// <param name="regionNumber">地区码</param>
        /// <param name="phoneNumber">手机号</param>
        public static uint RequestVerificationCode(uint regionNumber, uint phoneNumber)
        {
            return 0;
        }
#endif
    }
}