using System.Text.Json.Serialization;

namespace Wombat.CommGateway.Application.Services.DataCollection.Models
{
    /// <summary>
    /// WebSocket消息协议模型
    /// 定义统一的WebSocket消息格式，确保与SignalR保持一致的API
    /// </summary>
    public record WebSocketMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// 消息数据
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; init; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 连接ID（用于定向推送）
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string? ConnectionId { get; init; }

        /// <summary>
        /// 错误信息（当type为error时）
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; init; }

        /// <summary>
        /// 创建订阅确认消息
        /// </summary>
        public static WebSocketMessage CreateSubscriptionConfirmed(string target, int id, string connectionId)
            => new()
            {
                Type = "subscription_confirmed",
                Data = new { target, id },
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建取消订阅确认消息
        /// </summary>
        public static WebSocketMessage CreateUnsubscriptionConfirmed(string target, int id, string connectionId)
            => new()
            {
                Type = "unsubscription_confirmed",
                Data = new { target, id },
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建错误消息
        /// </summary>
        public static WebSocketMessage CreateError(string error, string? connectionId = null)
            => new()
            {
                Type = "error",
                Error = error,
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建心跳响应消息
        /// </summary>
        public static WebSocketMessage CreatePong()
            => new()
            {
                Type = "pong",
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建连接状态消息
        /// </summary>
        public static WebSocketMessage CreateConnectionStatus(string connectionId, object status)
            => new()
            {
                Type = "connection_status",
                Data = status,
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// 创建订阅状态消息
        /// </summary>
        public static WebSocketMessage CreateSubscriptionStatus(string connectionId, object subscriptions)
            => new()
            {
                Type = "subscription_status",
                Data = subscriptions,
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };
    }

    /// <summary>
    /// 点位更新数据模型
    /// </summary>
    public record PointUpdateData
    {
        [JsonPropertyName("pointId")]
        public int PointId { get; init; }

        [JsonPropertyName("value")]
        public string Value { get; init; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("updateTime")]
        public DateTime UpdateTime { get; init; }
    }

    /// <summary>
    /// 批量点位更新数据模型
    /// </summary>
    public record BatchPointsUpdateData
    {
        [JsonPropertyName("updates")]
        public List<PointUpdateData> Updates { get; init; } = new();

        [JsonPropertyName("updateTime")]
        public DateTime UpdateTime { get; init; }
    }

    /// <summary>
    /// 点位状态变更数据模型
    /// </summary>
    public record PointStatusChangeData
    {
        [JsonPropertyName("pointId")]
        public int PointId { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("updateTime")]
        public DateTime UpdateTime { get; init; }
    }

    /// <summary>
    /// 点位移除数据模型
    /// </summary>
    public record PointRemovedData
    {
        [JsonPropertyName("pointId")]
        public int PointId { get; init; }

        [JsonPropertyName("updateTime")]
        public DateTime UpdateTime { get; init; }
    }
} 