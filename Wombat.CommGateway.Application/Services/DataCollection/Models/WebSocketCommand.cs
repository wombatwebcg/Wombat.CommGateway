using System.Text.Json.Serialization;

namespace Wombat.CommGateway.Application.Services.DataCollection.Models
{
    /// <summary>
    /// WebSocket命令模型
    /// 定义客户端发送的命令结构，支持不同类型的订阅操作
    /// </summary>
    public record WebSocketCommand
    {
        /// <summary>
        /// 命令动作
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; init; } = string.Empty;

        /// <summary>
        /// 订阅目标类型：device, group, point
        /// </summary>
        [JsonPropertyName("target")]
        public string? Target { get; init; }

        /// <summary>
        /// 目标ID
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; init; }

        /// <summary>
        /// 连接ID（可选，用于服务器端验证）
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string? ConnectionId { get; init; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 创建订阅命令
        /// </summary>
        public static WebSocketCommand CreateSubscribe(string target, int id)
            => new()
            {
                Action = "subscribe",
                Target = target,
                Id = id,
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建取消订阅命令
        /// </summary>
        public static WebSocketCommand CreateUnsubscribe(string target, int id)
            => new()
            {
                Action = "unsubscribe",
                Target = target,
                Id = id,
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建心跳命令
        /// </summary>
        public static WebSocketCommand CreatePing()
            => new()
            {
                Action = "ping",
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建获取订阅状态命令
        /// </summary>
        public static WebSocketCommand CreateGetSubscriptionStatus()
            => new()
            {
                Action = "get_subscription_status",
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建获取连接统计命令
        /// </summary>
        public static WebSocketCommand CreateGetConnectionStatistics()
            => new()
            {
                Action = "get_connection_statistics",
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 验证命令是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Action) && 
                   (Action == "ping" || Action == "get_subscription_status" || Action == "get_connection_statistics" || 
                    (!string.IsNullOrEmpty(Target) && Id > 0));
        }

        /// <summary>
        /// 获取命令描述
        /// </summary>
        public string GetDescription()
        {
            return Action switch
            {
                "subscribe" => $"订阅{Target} {Id}",
                "unsubscribe" => $"取消订阅{Target} {Id}",
                "ping" => "心跳检测",
                "get_subscription_status" => "获取订阅状态",
                "get_connection_statistics" => "获取连接统计",
                _ => $"未知命令: {Action}"
            };
        }
    }

    /// <summary>
    /// 订阅目标类型常量
    /// </summary>
    public static class SubscriptionTargets
    {
        public const string Device = "device";
        public const string Group = "group";
        public const string Point = "point";
    }

    /// <summary>
    /// 命令动作类型常量
    /// </summary>
    public static class CommandActions
    {
        public const string Subscribe = "subscribe";
        public const string Unsubscribe = "unsubscribe";
        public const string Ping = "ping";
        public const string GetSubscriptionStatus = "get_subscription_status";
        public const string GetConnectionStatistics = "get_connection_statistics";
    }
} 