# 上下文
文件名：WebSocket服务重构任务.md
创建于：2024-12-19
创建者：AI Assistant
关联协议：RIPER-5 + Multidimensional + Agent Protocol 

# 任务描述
用户反馈当前的WebSocket桥接SignalR的逻辑存在问题，需要创建一个完整的WebSocket服务，订阅方式与SignalR保持一致。

# 项目概述
这是一个工业通信网关项目，包含：
- 后端：ASP.NET Core + SignalR + WebSocket中间件
- 前端：Vue.js + SignalR客户端
- 数据采集：实时点位数据采集和推送
- 订阅管理：支持设备、点位组、点位的订阅管理

---
*以下部分由 AI 在协议执行过程中维护*
---

# 分析 (由 RESEARCH 模式填充)

## 当前架构分析

### 现有组件
1. **SignalR Hub** (`DataCollectionHub.cs`)
   - 路径：`/ws/datacollection`
   - 功能：完整的订阅管理和数据推送
   - 方法：`SubscribeDevice`, `SubscribeGroup`, `SubscribePoint`等
   - 消息：`ReceivePointUpdate`, `ReceiveBatchPointsUpdate`等

2. **WebSocket桥接中间件** (`WsBridgeMiddleware.cs`)
   - 路径：`/ws/bridge`
   - 功能：简单的WebSocket桥接，转发DataPushBus消息
   - 问题：订阅逻辑过于简化，与SignalR不一致

3. **订阅管理器** (`ISubscriptionManager.cs`)
   - 功能：管理连接与订阅项的映射关系
   - 实现：`SubscriptionManager`类

4. **数据推送总线** (`IDataPushBus.cs`)
   - 功能：消息发布订阅模式
   - 实现：`DataPushBus`类

### 发现的问题

1. **订阅方式不一致**
   - SignalR：`SubscribeDevice(deviceId)`, `SubscribeGroup(groupId)`, `SubscribePoint(pointId)`
   - WebSocket桥接：`{"action": "subscribe", "id": 123}` - 无法区分订阅类型

2. **消息格式不统一**
   - SignalR：结构化的消息对象
   - WebSocket桥接：简单的JSON序列化

3. **功能缺失**
   - WebSocket桥接缺少订阅确认、状态查询等功能
   - 缺少连接统计和监控

4. **架构冗余**
   - 两套独立的WebSocket服务
   - 数据流向复杂：DataPushBus → WebSocket桥接

### 技术约束
- 需要保持与现有SignalR客户端的兼容性
- 需要支持设备、点位组、点位的分层订阅
- 需要实现连接管理和重连机制
- 需要支持批量数据推送

# 提议的解决方案 (由 INNOVATE 模式填充)

# 实施计划 (由 PLAN 模式生成)

# 当前执行步骤 (由 EXECUTE 模式在开始执行某步骤时更新)

# 任务进度 (由 EXECUTE 模式在每步完成后追加)

# 最终审查 (由 REVIEW 模式填充) 

## 订阅行为说明

- 订阅设备（"target": "device"）时，服务端会自动将该设备下所有点位ID加入订阅，推送所有相关点位的数据。
- 订阅组（"target": "group"）时，服务端会自动将该组下所有点位ID加入订阅。
- 订阅点位（"target": "point"）时，仅订阅该点位的数据。
- 取消订阅设备/组时，会批量移除所有相关点位的订阅。
- 推送逻辑与SignalR一致，始终按点位ID分发数据。 

## 关键实现文件位置

- WebSocketService 唯一实现位置：
  ```
  Wombat.CommGateway.Application/Services/WebSocketService.cs
  ```
  Api 层不应有重复实现，所有依赖注入和业务逻辑均指向此处。 