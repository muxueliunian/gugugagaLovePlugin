# gugugagaLovePlugin

一个用于 URA (UmamusumeResponseAnalyzer) 的插件，自动记录每日宝石获取量并可选采集社团粉丝数据。

**语言版本 / Language**:[中文](README.md) | [日本語](README_JP.md)

## 功能

- **💎 自动胡萝卜统计**: 按东八区日期统计每日获取的胡萝卜数量
- **📊 粉丝数据采集**: 可选功能，采集社团成员粉丝数据并按日期存储
- **📁 灵活存储**: 支持自定义输出目录，数据文件独立管理
- **🔧 配置简单**: 通过URA设置界面轻松配置，无需修改代码

## 特性

### 胡萝卜统计 (v1.0+)
- 自动识别和统计胡萝卜获取（item_type: 90, item_id: 43）
- 按东八区日期生成 `gugugagaMagicCarrot.json` 文件
- 实时控制台显示今日累计获取量
- 跨session累计统计，避免重复计算

### 粉丝数据采集 (v1.1+) 
- **默认关闭**，需手动启用避免影响原有功能
- 采集 `summary_user_info_array` 中的关键信息
- 按日期分文件存储为 `yyyymmdd.json` 格式
- 同日数据智能去重，相同 viewer_id 保留最新记录
- 优化字段顺序，时间戳简化为 `yyyymmdd` 格式
- 详细字段说明请参考 [数据映射文档](src/SummaryUserInfoMapping.md)

## 安装

1. 确保已安装 [UmamusumeResponseAnalyzer](https://github.com/UmamusumeResponseAnalyzer/UmamusumeResponseAnalyzer)
2. 下载最新的 `Plugin.cs` 文件到 URA 的插件目录
3. 重启 URA 或重新加载插件

## 输出示例

### 胡萝卜统计文件 (`gugugagaMagicCarrot.json`)
```json
{
  "20240125": 850,
  "20240126": 1200,
  "20240127": 750
}
```

### 粉丝数据文件 (`20240128.json`)
```json
{
  "12345678901234567": {
    "name": "テイオー",
    "fan": 1508234,
    "circle_name": "ウマ娘愛好会",
    "ts": "20240128",
    "viewer_id": 12345678901234567,
    "comment": "よろしくお願いします",
    "rank_score": 85230,
    "circle_id": 987654321
  }
}
```

## 配置说明

通过 URA 设置界面可配置以下选项：

### 胡萝卜统计配置
- **JSON输出目录**: 胡萝卜统计文件的保存位置（留空使用默认位置）

### 粉丝采集配置  
- **启用粉丝统计**: 是否开启粉丝数据采集功能（默认关闭）
- **粉丝数据输出目录**: 粉丝数据文件的保存位置（留空使用默认位置）

### 默认输出位置
- 工作目录下的 `PluginData/gugugagaLovePlugin/` 文件夹
- 支持相对路径和绝对路径自定义

## 从源码构建

### 前置条件
- .NET 8.0 SDK
- UmamusumeResponseAnalyzer 引用

### 构建步骤
```bash
dotnet build -c Release
```

构建完成后，将生成的 DLL 文件复制到 URA 插件目录。

## 跨平台注意事项

- **Windows**: 完全支持，无额外配置
- **Linux/macOS**: 需要安装对应的 .NET 8.0 运行时
- **时区处理**: 自动使用 "China Standard Time"，非中国地区用户可能需要调整

## 开发文档

### 插件结构
- `Plugin.cs`: 主插件代码，包含胡萝卜统计和粉丝采集逻辑
- `SummaryUserInfoMapping.md`: 详细的数据字段映射文档

### 扩展开发
插件采用模块化设计，可轻松扩展：
- 胡萝卜ID配置：修改 `KnownGemKeys` 集合
- 数据格式：调整 JSON 序列化逻辑  
- 时区设置：修改 `TzChina` 时区配置

## 版本历史

### v1.1.0 (2024-01-28)
- 🆕 新增粉丝数据采集功能（可选）
- 🆕 支持 summary_user_info_array 数据采集
- 🆕 按日期分文件存储，智能去重更新
- 🆕 优化字段顺序和时间格式
- 🆕 添加详细的数据映射文档
- 🔧 保持向后兼容，默认不影响原有功能

### v1.0.3 (2024-01-25)
- 🐛 修复时区转换问题
- 🔧 优化错误处理逻辑

### v1.0.2 (2024-01-20)
- 🔧 改进文件锁机制
- 🔧 优化JSON格式化

### v1.0.1 (2024-01-15)
- 🐛 修复路径处理问题
- 🔧 添加防御性编程

### v1.0.0 (2024-01-10)
- 🎉 初始版本发布
- ✨ 基础胡萝卜统计功能

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。

## 免责声明

本插件仅用于个人学习和研究目的。使用本插件产生的任何后果由用户自行承担。请确保遵守相关游戏的使用条款和当地法律法规。

## 相关链接

- [UmamusumeResponseAnalyzer](https://github.com/UmamusumeResponseAnalyzer/UmamusumeResponseAnalyzer)
- [.NET 8.0 下载](https://dotnet.microsoft.com/download/dotnet/8.0)

---

🐧 gugugaga!!! 🐧
