
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace Wombat.CommGateway.API.Filters
{
    /// <summary>
    /// 参数校验
    /// </summary>
    public class ValidFilterAttribute : BaseActionFilterAsync
    {
        public override async Task OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var msgList = context.ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);

                context.Result = Error(string.Join(",", msgList));
            }

            await Task.CompletedTask;
        }
    }
}
