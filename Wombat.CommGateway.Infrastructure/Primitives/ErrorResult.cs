namespace  Wombat.CommGateway.Infrastructure
{
    public class ErrorResult : AjaxResult
    {
        public ErrorResult(string message = "fail", int code = 401)
        {
            base.Message = message;
            base.Code = code;
        }
    }
}