# 数据库日志装饰器使用说明

## 概述

数据库日志装饰器是一个优雅的解决方案，用于在最小改动的情况下，将现有服务的日志记录同时写入数据库。

## 核心特性

- **装饰器模式**：包装现有的 `ILogger<T>`，保持兼容性
- **最小改动**：现有服务只需要改变注入的接口类型
- **智能分类**：根据服务类型和日志内容自动分类
- **异步记录**：不影响业务性能
- **灵活配置**：可选择性记录日志级别

## 使用方法

### 1. 注册服务

在 `Program.cs` 或服务注册处添加：

```csharp
// 注册基础服务
builder.Services.AddDatabaseLoggerServices();
```

### 2. 修改现有服务

将现有服务中的 `ILogger<T>` 替换为 `IApplicationLogger<T>`：

```csharp
// 之前
private readonly ILogger<MyService> _logger;

public MyService(ILogger<MyService> logger)
{
    _logger = logger;
}

// 之后
private readonly IApplicationLogger<MyService> _logger;

public MyService(IApplicationLogger<MyService> logger)
{
    _logger = logger;
}
```

### 3. 添加业务日志

在需要记录业务日志的地方添加：

```csharp
// 操作日志
await _logger.LogOperationAsync(
    "创建设备组成功", 
    "Create", 
    "DeviceGroup", 
    resourceId: deviceGroup.Id);

// 通信日志
await _logger.LogCommunicationAsync(
    "设备通信成功", 
    "Send", 
    "Modbus", 
    "01 03 00 00 00 02 C4 0B", 
    channelId: 1, 
    deviceId: 1, 
    responseTime: 100);

// 系统日志
await _logger.LogSystemAsync(
    "系统初始化完成", 
    LogCategory.System);
```

## 配置选项

### DatabaseLoggerOptions

```csharp
services.AddDatabaseLogger(options =>
{
    // 启用数据库日志记录
    options.EnableDatabaseLogging = true;
    
    // 配置需要记录的日志级别
    options.DatabaseLogLevels = new HashSet<LogLevel>
    {
        LogLevel.Information,
        LogLevel.Warning,
        LogLevel.Error,
        LogLevel.Critical
    };
    
    // 自定义服务类型映射
    options.ServiceLogCategoryMapping["MyService"] = LogCategory.Custom;
});
```

### 日志级别映射

- `LogLevel.Information` → `DomainLogLevel.Information`
- `LogLevel.Warning` → `DomainLogLevel.Warning`
- `LogLevel.Error` → `DomainLogLevel.Error`
- `LogLevel.Critical` → `DomainLogLevel.Critical`

### 服务类型映射

| 服务类型 | 日志类别 |
|---------|---------|
| DataCollectionService | DataCollection |
| WebSocketService | Communication |
| DeviceService | System |
| RuleEngineService | System |

## 自动分类规则

### 通信日志关键字
- 中文：连接、断开、发送、接收、通信、协议、数据、响应、超时、重连
- 英文：connection、disconnect、send、receive、communication、protocol、data、response、timeout、reconnect

### 操作日志关键字
- 中文：创建、更新、删除、启用、禁用、执行、处理、配置、设置、添加、移除
- 英文：create、update、delete、enable、disable、execute、process、configure、set、add、remove

## 示例集成

### 完整服务示例

```csharp
[AutoInject(typeof(IDeviceGroupService), ServiceLifetime = ServiceLifetime.Scoped)]
public class DeviceGroupService : IDeviceGroupService
{
    private readonly IDeviceGroupRepository _deviceGroupRepository;
    private readonly IApplicationLogger<DeviceGroupService> _logger;
    private readonly IMapper _mapper;

    public DeviceGroupService(
        IDeviceGroupRepository deviceGroupRepository,
        IApplicationLogger<DeviceGroupService> logger,
        IMapper mapper)
    {
        _deviceGroupRepository = deviceGroupRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<DeviceGroupDto> CreateDeviceGroupAsync(CreateDeviceGroupDto dto)
    {
        var deviceGroup = new DeviceGroup(dto.Name, dto.Description);
        await _deviceGroupRepository.InsertAsync(deviceGroup);
        
        // 记录操作日志到数据库
        await _logger.LogOperationAsync(
            $"创建设备组成功: {dto.Name}", 
            "Create", 
            "DeviceGroup", 
            resourceId: deviceGroup.Id);
        
        return _mapper.Map<DeviceGroupDto>(deviceGroup);
    }
}
```

## 注册选项

### 方案1：精确控制（推荐）

```csharp
services.AddDatabaseLoggerFor<DeviceGroupService>()
       .AddDatabaseLoggerFor<DeviceService>()
       .AddDatabaseLoggerFor<DataCollectionService>();
```

### 方案2：批量注册

```csharp
services.AddDatabaseLoggerForTypes(
    typeof(DeviceGroupService),
    typeof(DeviceService),
    typeof(DataCollectionService)
);
```

### 方案3：全局注册（慎用）

```csharp
services.AddDatabaseLoggerForAll();
```

## 性能考虑

1. **异步记录**：数据库日志记录是异步的，不会阻塞业务流程
2. **错误隔离**：数据库日志记录失败不会影响原始日志记录
3. **可配置**：可以通过配置控制哪些日志级别需要记录到数据库
4. **智能分类**：减少不必要的日志记录

## 兼容性

- 完全兼容现有的 `ILogger<T>` 接口
- 不影响现有的日志记录功能
- 可以与 Serilog、NLog 等日志框架共存

## 故障排除

### 常见问题

1. **服务未注册**：确保在 DI 容器中注册了装饰器服务
2. **循环依赖**：避免在 LogService 中使用装饰器
3. **性能问题**：检查数据库日志级别配置
4. **日志重复**：确保没有重复注册装饰器

### 调试技巧

1. 启用详细日志记录
2. 检查数据库连接
3. 验证服务注册
4. 测试异步操作 