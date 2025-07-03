namespace  Wombat.CommGateway.Infrastructure
{
    /// <summary>
    /// Ajax请求结果
    /// </summary>
    public class AjaxResult
    {
        /// <summary>
        /// 返回消息
        /// </summary>
        public string Message { get; set; } = "success";

        /// <summary>
        /// 错误代码
        /// </summary>
        public int Code { get; set; } = 200;

    }
}
