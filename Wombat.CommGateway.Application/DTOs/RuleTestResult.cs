using System;
using System.Collections.Generic;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 规则测试结果
    /// </summary>
    public class RuleTestResult
    {
        /// <summary>
        /// 测试是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestTime { get; set; }

        /// <summary>
        /// 输入数据
        /// </summary>
        public RuleTestData InputData { get; set; }

        /// <summary>
        /// 输出数据
        /// </summary>
        public Dictionary<string, object> OutputData { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public int ExecutionTimeMs { get; set; }

        /// <summary>
        /// 规则ID
        /// </summary>
        public int RuleId { get; set; }

        /// <summary>
        /// 规则名称
        /// </summary>
        public string RuleName { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static RuleTestResult CreateSuccess(
            RuleTestData inputData,
            Dictionary<string, object> outputData,
            int executionTimeMs,
            int ruleId,
            string ruleName)
        {
            return new RuleTestResult
            {
                Success = true,
                TestTime = DateTime.UtcNow,
                InputData = inputData,
                OutputData = outputData,
                ExecutionTimeMs = executionTimeMs,
                RuleId = ruleId,
                RuleName = ruleName
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static RuleTestResult CreateFailure(
            RuleTestData inputData,
            string errorMessage,
            int ruleId,
            string ruleName)
        {
            return new RuleTestResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                TestTime = DateTime.UtcNow,
                InputData = inputData,
                RuleId = ruleId,
                RuleName = ruleName
            };
        }
    }
} 