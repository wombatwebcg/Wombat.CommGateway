namespace  Wombat.CommGateway.API
{
    /// <summary>
    /// Ajax请求结果
    /// </summary>
    public class AjaxResult<T> : AjaxResult
    {

        /// <summary>
        /// 返回数据
        /// </summary>
        public T Data { get; set; }
    }
}
