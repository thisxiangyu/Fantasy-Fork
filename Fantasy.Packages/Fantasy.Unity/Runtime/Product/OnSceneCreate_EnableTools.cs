#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Product.Authentication;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fantasy.Product
{
    /// <summary>
    /// Scene创建后根据配置启用各种效率工具
    /// </summary>
    public sealed class OnSceneCreate_EnableTools : AsyncEventSystem<OnCreateScene>, IAsyncActionFilter
    {
        /// <summary>
        /// 
        /// </summary>
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 执行各种启用
        /// </summary> 
        protected override async FTask Handler(OnCreateScene self)
        {
            await JWT.Instance.Enable();
        }
    }
}
#endif