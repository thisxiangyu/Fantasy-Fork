using System.Threading;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;
using ZXing.Datamatrix.Encoder;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Rendering;

namespace Fantasy.Product.Authentication
{
    /// <summary>
    /// 二维码生成帮助类
    /// </summary>
    public class QRCodeHelper
    {
        /// <summary>
        ///  异步生成二维码。
        /// </summary>
        /// <param name="contents">用来生成二维码的字符串内容</param>
        /// <param name="cancelToken">异步取消令牌</param>
        /// <param name="verision">二维码版本， 可选 1~40</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <returns></returns>
        public static async Task<PixelData> GenerateQRCodeAsync(string contents, CancellationToken cancelToken,int verision = 5, int width = 256,int height = 256)
        {
            cancelToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
            {
                cancelToken.ThrowIfCancellationRequested();
                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Width = width,
                        Height = height,
                        Margin = 1,
                        QrVersion = verision,
                        ErrorCorrection = ErrorCorrectionLevel.M // 纠错等级
                    }
                };
                return writer.Write(contents); 
            }, cancelToken);
        }
    }
}
