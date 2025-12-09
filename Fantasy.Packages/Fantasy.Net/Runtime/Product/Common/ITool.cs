
using Fantasy.Async;

namespace Fantasy.Product
{
    /// <summary>
    /// 效率工具接口
    /// </summary>
    public interface ITool
    {
        ///是否启用了
        bool IsEnabled { get; }
        /// <summary>
        /// 启用工具
        /// </summary>
        FTask Enable();
    }
}
