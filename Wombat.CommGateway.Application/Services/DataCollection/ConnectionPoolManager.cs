using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wombat.CommGateway.Domain.Entities;
using Wombat.IndustrialCommunication;
using Wombat.Extensions.AutoGenerator.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using Wombat.IndustrialCommunication.Modbus;
using Wombat.IndustrialCommunication.PLC;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 连接池管理器
    /// 负责管理通信连接的创建、复用和健康检查
    /// </summary>
    [AutoInject<ConnectionPoolManager>(ServiceLifetime.Singleton)]
    public class ConnectionPoolManager : IDisposable
    {
        private readonly ILogger<ConnectionPoolManager> _logger;
        private readonly ConcurrentDictionary<int, ConnectionPool> _channelPools;
        private readonly Timer _healthCheckTimer;
        private readonly int _maxPoolSize = 10;
        private readonly int _connectionTimeoutMs = 30000;
        private readonly int _healthCheckIntervalMs = 60000; // 1分钟

        public ConnectionPoolManager(ILogger<ConnectionPoolManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channelPools = new ConcurrentDictionary<int, ConnectionPool>();
            _healthCheckTimer = new Timer(PerformHealthCheck, null, _healthCheckIntervalMs, _healthCheckIntervalMs);
        }

        /// <summary>
        /// 获取连接
        /// </summary>
        /// <param name="channel">通信通道</param>
        /// <returns>设备客户端连接</returns>
        public async Task<IDeviceClient> GetConnectionAsync(Channel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            var pool = GetOrCreatePool(channel.Id);
            return await pool.GetConnectionAsync(channel);
        }

        /// <summary>
        /// 释放连接
        /// </summary>
        /// <param name="channelId">通道ID</param>
        /// <param name="client">设备客户端</param>
        public void ReleaseConnection(int channelId, IDeviceClient client)
        {
            if (_channelPools.TryGetValue(channelId, out var pool))
            {
                pool.ReleaseConnection(client);
            }
        }

        private ConnectionPool GetOrCreatePool(int channelId)
        {
            return _channelPools.GetOrAdd(channelId, id => new ConnectionPool(id, _maxPoolSize, _connectionTimeoutMs, _logger));
        }

        private void PerformHealthCheck(object state)
        {
            try
            {
                foreach (var pool in _channelPools.Values)
                {
                    pool.PerformHealthCheck();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行连接池健康检查时发生错误");
            }
        }

        /// <summary>
        /// 获取连接池统计信息
        /// </summary>
        /// <returns>连接池统计信息</returns>
        public Dictionary<int, ConnectionPoolStatistics> GetPoolStatistics()
        {
            var statistics = new Dictionary<int, ConnectionPoolStatistics>();
            foreach (var kvp in _channelPools)
            {
                statistics[kvp.Key] = kvp.Value.GetStatistics();
            }
            return statistics;
        }

        /// <summary>
        /// 获取指定通道的连接池统计信息
        /// </summary>
        /// <param name="channelId">通道ID</param>
        /// <returns>连接池统计信息</returns>
        public ConnectionPoolStatistics GetChannelPoolStatistics(int channelId)
        {
            if (_channelPools.TryGetValue(channelId, out var pool))
            {
                return pool.GetStatistics();
            }
            return null;
        }

        /// <summary>
        /// 清理指定通道的连接池
        /// </summary>
        /// <param name="channelId">通道ID</param>
        public void ClearChannelPool(int channelId)
        {
            if (_channelPools.TryRemove(channelId, out var pool))
            {
                pool.Dispose();
                _logger.LogInformation($"通道 {channelId} 连接池已清理");
            }
        }

        /// <summary>
        /// 清理所有连接池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _channelPools.Values)
            {
                pool.Dispose();
            }
            _channelPools.Clear();
            _logger.LogInformation("所有连接池已清理");
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            foreach (var pool in _channelPools.Values)
            {
                pool.Dispose();
            }
            _channelPools.Clear();
        }
    }

    /// <summary>
    /// 单个通道的连接池
    /// </summary>
    public class ConnectionPool : IDisposable
    {
        private readonly int _channelId;
        private readonly int _maxPoolSize;
        private readonly int _connectionTimeoutMs;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<PooledConnection> _availableConnections;
        private readonly ConcurrentDictionary<IDeviceClient, PooledConnection> _activeConnections;

        public ConnectionPool(int channelId, int maxPoolSize, int connectionTimeoutMs, ILogger logger)
        {
            _channelId = channelId;
            _maxPoolSize = maxPoolSize;
            _connectionTimeoutMs = connectionTimeoutMs;
            _logger = logger;
            _availableConnections = new ConcurrentQueue<PooledConnection>();
            _activeConnections = new ConcurrentDictionary<IDeviceClient, PooledConnection>();
        }

        public async Task<IDeviceClient> GetConnectionAsync(Channel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            _logger.LogDebug($"通道 {_channelId} 请求获取连接");

            // 首先尝试从可用连接池中获取连接
            if (_availableConnections.TryDequeue(out var pooledConnection))
            {
                // 检查连接是否健康
                if (pooledConnection.IsHealthy && await pooledConnection.PerformHealthCheckAsync())
                {
                    pooledConnection.MarkAsInUse();
                    _activeConnections[pooledConnection.Client] = pooledConnection;
                    _logger.LogDebug($"通道 {_channelId} 从池中获取到健康连接");
                    return pooledConnection.Client;
                }
                else
                {
                    // 连接不健康，销毁并创建新连接
                    _logger.LogWarning($"通道 {_channelId} 池中连接不健康，销毁并创建新连接");
                    await DestroyConnectionAsync(pooledConnection);
                }
            }

            // 检查是否达到最大连接数
            if (_activeConnections.Count >= _maxPoolSize)
            {
                _logger.LogWarning($"通道 {_channelId} 连接池已满，等待可用连接");
                
                // 等待可用连接，最多等待30秒
                var waitStartTime = DateTime.UtcNow;
                while (_activeConnections.Count >= _maxPoolSize && 
                       DateTime.UtcNow.Subtract(waitStartTime).TotalSeconds < 30)
                {
                    await Task.Delay(100); // 等待100ms后重试
                }

                // 如果仍然没有可用连接，抛出异常
                if (_activeConnections.Count >= _maxPoolSize)
                {
                    throw new InvalidOperationException($"通道 {_channelId} 连接池已满，无法获取连接");
                }
            }

            // 创建新连接
            var client = CreatClientByChannel(channel);
            if (client == null)
            {
                throw new InvalidOperationException($"通道 {_channelId} 创建连接失败");
            }

            try
            {
                // 尝试连接
                var connectResult = await client.ConnectAsync();
                if (!connectResult.IsSuccess)
                {
                    throw new InvalidOperationException($"通道 {_channelId} 连接失败: {connectResult.Message}");
                }

                // 创建池化连接对象
                var newPooledConnection = new PooledConnection(client);
                newPooledConnection.MarkAsInUse();
                _activeConnections[client] = newPooledConnection;

                _logger.LogInformation($"通道 {_channelId} 创建新连接成功");
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"通道 {_channelId} 创建连接时发生错误");
                throw;
            }
        }

        public void ReleaseConnection(IDeviceClient client)
        {
            if (client == null)
                return;

            _logger.LogDebug($"通道 {_channelId} 释放连接");

            if (_activeConnections.TryRemove(client, out var pooledConnection))
            {
                try
                {
                    // 检查连接是否仍然健康
                    if (pooledConnection.IsHealthy && pooledConnection.Client.Connected)
                    {
                        // 连接健康，归还到池中
                        pooledConnection.MarkAsIdle();
                        _availableConnections.Enqueue(pooledConnection);
                        _logger.LogDebug($"通道 {_channelId} 连接已归还到池中");
                    }
                    else
                    {
                        // 连接不健康，销毁连接
                        _logger.LogWarning($"通道 {_channelId} 连接不健康，销毁连接");
                        _ = Task.Run(async () => await DestroyConnectionAsync(pooledConnection));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通道 {_channelId} 释放连接时发生错误");
                    // 发生异常时销毁连接
                    _ = Task.Run(async () => await DestroyConnectionAsync(pooledConnection));
                }
            }
            else
            {
                _logger.LogWarning($"通道 {_channelId} 尝试释放不存在的连接");
            }
        }

        /// <summary>
        /// 销毁连接
        /// </summary>
        /// <param name="pooledConnection">池化连接</param>
        private async Task DestroyConnectionAsync(PooledConnection pooledConnection)
        {
            try
            {
                if (pooledConnection?.Client != null)
                {
                    await pooledConnection.Client.DisconnectAsync();
                    _logger.LogDebug($"通道 {_channelId} 连接已销毁");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"通道 {_channelId} 销毁连接时发生错误");
            }
        }

        public void PerformHealthCheck()
        {
            _logger.LogDebug($"通道 {_channelId} 开始执行健康检查");

            var healthCheckIntervalMinutes = 5; // 5分钟健康检查间隔
            var connectionTimeoutMinutes = 30; // 30分钟连接超时

            // 检查可用连接池中的连接
            var availableConnections = new List<PooledConnection>();
            while (_availableConnections.TryDequeue(out var connection))
            {
                availableConnections.Add(connection);
            }

            foreach (var connection in availableConnections)
            {
                try
                {
                    // 检查连接是否超时
                    if (connection.IsExpired(connectionTimeoutMinutes))
                    {
                        _logger.LogInformation($"通道 {_channelId} 连接超时，销毁连接");
                        _ = Task.Run(async () => await DestroyConnectionAsync(connection));
                        continue;
                    }

                    // 检查是否需要健康检查
                    if (connection.NeedsHealthCheck(healthCheckIntervalMinutes))
                    {
                        var isHealthy = connection.PerformHealthCheckAsync().Result;
                        if (!isHealthy)
                        {
                            _logger.LogWarning($"通道 {_channelId} 连接健康检查失败，销毁连接: {connection.LastErrorMessage}");
                            _ = Task.Run(async () => await DestroyConnectionAsync(connection));
                            continue;
                        }
                    }

                    // 连接健康，重新放回池中
                    _availableConnections.Enqueue(connection);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通道 {_channelId} 健康检查连接时发生错误");
                    _ = Task.Run(async () => await DestroyConnectionAsync(connection));
                }
            }

            // 检查活跃连接
            var activeConnectionsToRemove = new List<IDeviceClient>();
            foreach (var kvp in _activeConnections)
            {
                var client = kvp.Key;
                var connection = kvp.Value;

                try
                {
                    // 检查连接是否超时（活跃连接的超时时间可以更长）
                    if (connection.IsExpired(connectionTimeoutMinutes * 2))
                    {
                        _logger.LogWarning($"通道 {_channelId} 活跃连接超时，标记为错误");
                        connection.MarkAsError("连接超时");
                        activeConnectionsToRemove.Add(client);
                        continue;
                    }

                    // 检查连接状态
                    if (!client.Connected)
                    {
                        _logger.LogWarning($"通道 {_channelId} 活跃连接已断开，标记为错误");
                        connection.MarkAsError("连接已断开");
                        activeConnectionsToRemove.Add(client);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通道 {_channelId} 检查活跃连接时发生错误");
                    connection.MarkAsError(ex.Message);
                    activeConnectionsToRemove.Add(client);
                }
            }

            // 移除有问题的活跃连接
            foreach (var client in activeConnectionsToRemove)
            {
                if (_activeConnections.TryRemove(client, out var connection))
                {
                    _ = Task.Run(async () => await DestroyConnectionAsync(connection));
                }
            }

            _logger.LogDebug($"通道 {_channelId} 健康检查完成，可用连接: {_availableConnections.Count}, 活跃连接: {_activeConnections.Count}");
        }

        /// <summary>
        /// 获取连接池统计信息
        /// </summary>
        /// <returns>连接池统计信息</returns>
        public ConnectionPoolStatistics GetStatistics()
        {
            return new ConnectionPoolStatistics
            {
                ChannelId = _channelId,
                MaxPoolSize = _maxPoolSize,
                AvailableConnections = _availableConnections.Count,
                ActiveConnections = _activeConnections.Count,
                TotalConnections = _availableConnections.Count + _activeConnections.Count,
                PoolUtilization = _maxPoolSize > 0 ? (double)(_availableConnections.Count + _activeConnections.Count) / _maxPoolSize : 0,
                LastHealthCheckTime = DateTime.UtcNow
            };
        }


        public IDeviceClient CreatClientByChannel(Channel channel)
        {
            if (channel == null || channel.Configuration == null)
                return null;

            try
            {
                switch (channel.Protocol)
                {
                    case ProtocolType.SiemensS7:
                        // 提取参数
                        var ip = channel.Configuration.ContainsKey("ipAddress") ? channel.Configuration["ipAddress"] : null;
                        var portStr = channel.Configuration.ContainsKey("port") ? channel.Configuration["port"] : "102";
                        var cpuType = channel.Configuration.ContainsKey("cpuType") ? channel.Configuration["cpuType"] : "S7-1200";
                        var rackStr = channel.Configuration.ContainsKey("rack") ? channel.Configuration["rack"] : "0";
                        var slotStr = channel.Configuration.ContainsKey("slot") ? channel.Configuration["slot"] : "0";

                        if (string.IsNullOrWhiteSpace(ip))
                            throw new ArgumentException("SiemensS7配置缺少ipAddress参数");

                        if (!int.TryParse(portStr, out int port))
                            port = 102;
                        if (!byte.TryParse(rackStr, out byte rack))
                            rack = 0;
                        if (!byte.TryParse(slotStr, out byte slot))
                            slot = 0;

                        if (!CpuTypeToVersion.TryGetValue(cpuType, out SiemensVersion version))
                            throw new ArgumentException($"未知的cpuType: {cpuType}");

                        // 实例化SiemensClient
                        return new SiemensClient(ip, port, version, rack, slot);

                    case ProtocolType.ModbusTCP:
                        // 提取参数
                        var modbusIp = channel.Configuration.ContainsKey("ipAddress") ? channel.Configuration["ipAddress"] : null;
                        var modbusPortStr = channel.Configuration.ContainsKey("port") ? channel.Configuration["port"] : "502";
                        if (string.IsNullOrWhiteSpace(modbusIp))
                            throw new ArgumentException("ModbusTCP配置缺少ipAddress参数");
                        if (!int.TryParse(modbusPortStr, out int modbusPort))
                            modbusPort = 502;
                        // 实例化ModbusTcpClient
                        return new ModbusTcpClient(modbusIp, modbusPort);

                    default:
                        // 其他协议暂未实现
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"CreatClientByChannel参数解析或实例化失败，协议: {channel.Protocol}");
                return null;
            }
        }


        private static readonly Dictionary<string, SiemensVersion> CpuTypeToVersion = new()
        {
            { "S7-200Smart", SiemensVersion.S7_200Smart },
            { "S7-200", SiemensVersion.S7_200 },
            { "S7-300", SiemensVersion.S7_300 },
            { "S7-400", SiemensVersion.S7_400 },
            { "S7-1200", SiemensVersion.S7_1200 },
            { "S7-1500", SiemensVersion.S7_1500 }
        };
        public void Dispose()
        {
            // 清理所有连接
            foreach (var kvp in _activeConnections)
            {
                try
                {
                    kvp.Value?.Client?.DisconnectAsync().Wait(5000);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"销毁连接时发生错误");
                }
            }
            _activeConnections.Clear();

            while (_availableConnections.TryDequeue(out var connection))
            {
                try
                {
                    connection?.Client?.DisconnectAsync().Wait(5000);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"销毁连接时发生错误");
                }
            }
        }
    }

    /// <summary>
    /// 连接状态枚举
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Idle,
        /// <summary>
        /// 使用中
        /// </summary>
        InUse,
        /// <summary>
        /// 连接中
        /// </summary>
        Connecting,
        /// <summary>
        /// 已连接
        /// </summary>
        Connected,
        /// <summary>
        /// 断开连接
        /// </summary>
        Disconnected,
        /// <summary>
        /// 错误状态
        /// </summary>
        Error
    }

    /// <summary>
    /// 池化连接
    /// </summary>
    public class PooledConnection
    {
        public IDeviceClient Client { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUsedTime { get; set; }
        public DateTime LastHealthCheckTime { get; set; }
        public bool IsHealthy { get; set; }
        public ConnectionStatus Status { get; set; }
        public int UseCount { get; set; }
        public int ErrorCount { get; set; }
        public string LastErrorMessage { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PooledConnection(IDeviceClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            CreatedTime = DateTime.UtcNow;
            LastUsedTime = DateTime.UtcNow;
            LastHealthCheckTime = DateTime.UtcNow;
            IsHealthy = true;
            Status = ConnectionStatus.Idle;
            UseCount = 0;
            ErrorCount = 0;
            LastErrorMessage = string.Empty;
        }

        /// <summary>
        /// 检查连接是否超时
        /// </summary>
        /// <param name="timeoutMinutes">超时时间（分钟）</param>
        /// <returns>是否超时</returns>
        public bool IsExpired(int timeoutMinutes)
        {
            return DateTime.UtcNow.Subtract(LastUsedTime).TotalMinutes > timeoutMinutes;
        }

        /// <summary>
        /// 检查是否需要健康检查
        /// </summary>
        /// <param name="healthCheckIntervalMinutes">健康检查间隔（分钟）</param>
        /// <returns>是否需要健康检查</returns>
        public bool NeedsHealthCheck(int healthCheckIntervalMinutes)
        {
            return DateTime.UtcNow.Subtract(LastHealthCheckTime).TotalMinutes > healthCheckIntervalMinutes;
        }

        /// <summary>
        /// 执行健康检查
        /// </summary>
        /// <returns>健康检查结果</returns>
        public async Task<bool> PerformHealthCheckAsync()
        {
            try
            {
                LastHealthCheckTime = DateTime.UtcNow;

                if (Client == null)
                {
                    IsHealthy = false;
                    Status = ConnectionStatus.Error;
                    LastErrorMessage = "客户端为空";
                    return false;
                }

                // 检查连接状态
                if (!Client.Connected)
                {
                    IsHealthy = false;
                    Status = ConnectionStatus.Disconnected;
                    LastErrorMessage = "连接已断开";
                    return false;
                }

                // 尝试执行一个简单的操作来验证连接
                // 这里可以根据具体协议实现不同的健康检查方式
                // 暂时使用连接状态作为健康检查依据
                IsHealthy = true;
                Status = ConnectionStatus.Connected;
                LastErrorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                IsHealthy = false;
                Status = ConnectionStatus.Error;
                LastErrorMessage = ex.Message;
                ErrorCount++;
                return false;
            }
        }

        /// <summary>
        /// 标记为使用中
        /// </summary>
        public void MarkAsInUse()
        {
            LastUsedTime = DateTime.UtcNow;
            UseCount++;
            Status = ConnectionStatus.InUse;
        }

        /// <summary>
        /// 标记为空闲
        /// </summary>
        public void MarkAsIdle()
        {
            Status = ConnectionStatus.Idle;
        }

        /// <summary>
        /// 标记为错误状态
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public void MarkAsError(string errorMessage)
        {
            IsHealthy = false;
            Status = ConnectionStatus.Error;
            LastErrorMessage = errorMessage;
            ErrorCount++;
        }

        /// <summary>
        /// 重置连接状态
        /// </summary>
        public void Reset()
        {
            IsHealthy = true;
            Status = ConnectionStatus.Idle;
            ErrorCount = 0;
            LastErrorMessage = string.Empty;
        }

        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            var age = DateTime.UtcNow.Subtract(CreatedTime);
            var idleTime = DateTime.UtcNow.Subtract(LastUsedTime);
            
            return $"状态: {Status}, 健康: {IsHealthy}, 使用次数: {UseCount}, 错误次数: {ErrorCount}, " +
                   $"创建时间: {CreatedTime:yyyy-MM-dd HH:mm:ss}, 最后使用: {LastUsedTime:yyyy-MM-dd HH:mm:ss}, " +
                   $"连接时长: {age.TotalMinutes:F1}分钟, 空闲时长: {idleTime.TotalMinutes:F1}分钟";
        }


    }

} 