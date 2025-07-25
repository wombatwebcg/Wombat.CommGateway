

### 1.1 概述

```
You are developing an industrial soft‑gateway based on .NET 8 WebAPI (后端) + Vue 3 (前端).
The gateway supports multiple physical interfaces (serial, Ethernet, etc.) and protocols (Siemens S7, Mitsubishi, Omron, Modbus, etc.).
You must provide point‑tag (点位) management, rule‑engine (数据转换、转发), and full logging (采集结果、运行日志、RPC调用结果).
```

### 1.2 网关配置模块

```
Prompt: 实现多接口与多协议的统一配置中心。
```

* **接口类型**：

  * 串口（RS232/RS485）
  * 网口（TCP/UDP）
  * CAN、PROFINET（可扩展）
* **协议支持**：

  * 西门子 S7
  * 三菱 MC 协议
  * 欧姆龙 FINS
  * Modbus RTU/TCP
* **采集点位管理**：

  * 点位属性：地址、数据类型、周期、备注
  * 分组管理：分区、设备、通道
  * 启停控制：启用/禁用

### 1.3 规则引擎模块

```
Prompt: 提供灵活的点位数据流规则编辑器。
```

* **规则类型**：

  * 数据映射（按比例、偏移）
  * 单点/多点联动（条件触发）
  * 转发目标：MQTT、HTTP POST、数据库
* **规则设计器**：

  * 可视化拖拽或 JSON 定义
  * 流程调试与实时预览
  * 版本管理与回滚

### 1.4 日志与监控模块

```
Prompt: 收集并展示实时与历史日志，支持多级检索。
```

* **日志类型**：

  * 采集数据日志（原始/转换后）
  * 系统事件日志（启动/关闭/异常）
  * RPC 调用日志（入参/出参/耗时）
* **存储**：

  * 本地文件（按天归档）
  * Elasticsearch（可选）
* **查询与告警**：

  * 智能过滤：按级别、模块、时间
  * 告警推送：邮件、短信、企业微信

---
## 2. 界面需求文档

### 2.1 总体布局

```
Prompt: 使用 Vue 3 + Pinia + Vue Router 构建 SPA。整体采用侧边栏导航+内容区域布局。
```

* **导航栏**（左侧）：
  * 宽度：200px（展开）/ 80px（折叠）
  * 背景色：#001529
  * 文字颜色：#FFFFFF
  * 菜单项：配置中心、规则引擎、日志监控、系统管理
  * 折叠/展开按钮：位于底部

* **顶部栏**：
  * 高度：64px
  * 背景色：#FFFFFF
  * 阴影：0 1px 4px rgba(0,21,41,.08)
  * 内容布局：
    * 左侧：当前项目名称（24px，主标题）
    * 右侧：用户信息与设置、全局搜索框
  * 搜索框：
    * 宽度：300px
    * 圆角：4px
    * 边框色：#D9D9D9

* **主内容区**：
  * 背景色：#F0F2F5
  * 内边距：24px
  * 标签页：
    * 高度：40px
    * 背景色：#FFFFFF
  * 面包屑：
    * 字体：14px
    * 颜色：#8C8C8C
  * 自适应布局：
    * 响应式断点：
      * xs: <576px
      * sm: ≥576px
      * md: ≥768px
      * lg: ≥992px
      * xl: ≥1200px

### 2.2 配置中心页面

```
Prompt: 提供设备与点位统一配置视图。
```

* **设备列表**：
  * 表格样式：
    * 表头背景色：#FAFAFA
    * 行高：48px
    * 斑马纹：隔行变色
    * 边框色：#D9D9D9
  * 列配置：
    * 设备名称（200px）
    * 类型（120px）
    * 状态（100px）
    * 操作（150px）
  * 状态标签：
    * 启用：#52C41A
    * 禁用：#F5222D
    * 离线：#FAAD14

* **新建设备**：
  * 弹窗尺寸：600px × 500px
  * 表单样式：
    * 标签宽度：100px
    * 输入框高度：32px
    * 必填项标记：红色星号
  * 按钮样式：
    * 主要按钮：实心背景 #1890FF
    * 次要按钮：描边样式
    * 按钮高度：32px

* **点位管理**：
  * 树结构：
    * 节点间距：8px
    * 展开图标：12px
  * 表格视图：
    * 分页器：每页20条
    * 批量操作按钮组
  * 导入/导出：
    * 按钮样式：文字按钮
    * 文件格式：CSV
  * 编辑弹窗：
    * 宽度：500px
    * 表单布局：两列

### 2.3 规则引擎页面

```
Prompt: 可视化/JSON 双向编辑的规则列表。
```

* **规则列表**：
  * 表格样式：
    * 固定表头
    * 可排序
    * 可筛选
  * 状态标签：
    * 运行中：#52C41A
    * 已停止：#F5222D
    * 编辑中：#1890FF

* **编辑器**：
  * 画布区域：
    * 背景色：#FFFFFF
    * 网格线：#F0F0F0
    * 节点样式：
      * 宽度：180px
      * 圆角：4px
      * 阴影：0 2px 8px rgba(0,0,0,0.15)
  * JSON编辑器：
    * 主题：VS Code Dark
    * 字体：Consolas
    * 字号：14px
  * 预览面板：
    * 高度：300px
    * 背景色：#F5F5F5
    * 实时数据更新

### 2.4 日志监控页面

```
Prompt: 实时日志流+历史查询分析。
```

* **实时日志面板**：
  * 高度：400px
  * 背景色：#000000
  * 字体：Consolas
  * 字号：12px
  * 行高：20px
  * 日志级别颜色：
    * INFO：#1890FF
    * WARN：#FAAD14
    * ERROR：#F5222D
    * DEBUG：#8C8C8C

* **历史日志查询**：
  * 查询表单：
    * 布局：行内表单
    * 间距：16px
  * 时间选择器：
    * 宽度：300px
    * 快捷选项
  * 关键字搜索：
    * 宽度：200px
    * 支持模糊匹配
  * 导出按钮：
    * 位置：右上角
    * 格式：TXT/JSON
  * 分页器：
    * 每页：50条
    * 快速跳转

### 2.5 系统管理页面

```
Prompt: 用户、权限、全局配置管理。
```

* **用户权限**：
  * 表格样式：
    * 固定操作列
    * 可批量操作
  * 角色管理：
    * 树形结构
    * 权限分配
  * 状态切换：
    * 开关组件
    * 即时生效

* **全局参数**：
  * 表单布局：
    * 两列布局
    * 标签右对齐
  * 输入控件：
    * 数字输入框
    * 下拉选择器
    * 开关组件
  * 保存按钮：
    * 位置：底部
    * 样式：主要按钮

* **版本与升级**：
  * 版本信息：
    * 卡片样式
    * 突出显示
  * 升级按钮：
    * 样式：主要按钮
    * 确认弹窗
  * 更新日志：
    * 时间线样式
    * 可折叠

---

*以上文档可直接作为项目启动的需求模板，并可在开发过程中持续迭代。*

## 运行时配置API/SignalR后端地址（第三方部署指南）

前端打包后，API和SignalR后端地址可通过 `public/config.json` 文件动态配置，无需重新打包：

```json
{
  "API_BASE_URL": "http://your-api-server:5000"
}
```

- 第三方只需修改 `config.json` 中的 `API_BASE_URL`，即可切换后端服务地址和端口。
- SignalR和所有API请求会自动读取此配置。
- `config.json` 路径为 `/gateway/config.json`（与前端部署路径一致）。

### 配置网页名称和icon（推荐）

将你的logo.png放在public目录下，打包后路径为/gateway/logo.png。

在 `public/config.json` 中添加如下字段：

```json
{
  "WEB_TITLE": "自定义网页名称",
  "WEB_ICON": "/gateway/logo.png"
}
```
- `WEB_TITLE`：网页标题（浏览器tab显示的名称）
- `WEB_ICON`：网页icon路径（推荐/gateway/logo.png）

> 注意：vite.svg已废弃，不再作为默认icon。

### 最佳实践：自定义favicon和logo

- favicon（网页icon）：将你的favicon.ico放在public目录，打包后路径为/gateway/favicon.ico。
- logo（页面logo）：将你的logo.png放在public目录，打包后路径为/gateway/logo.png。
- 所有页面和配置文件引用时都用`/logo.png`或`/favicon.ico`，无需加base前缀。
- 用户只需替换public/logo.png和public/favicon.ico，无需改动代码或重新打包。

在 `public/config.json` 中可选配置如下字段：

```json
{
  "WEB_TITLE": "自定义网页名称",
  "WEB_ICON": "/favicon.ico"
}
```
- `WEB_TITLE`：网页标题（浏览器tab显示的名称）
- `WEB_ICON`：网页icon路径（推荐/favicon.ico）
