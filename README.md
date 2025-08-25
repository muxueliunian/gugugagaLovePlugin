# gugugagaLovePlugin

一个用于 [UmamusumeResponseAnalyzer](https://github.com/UmamusumeResponseAnalyzer/UmamusumeResponseAnalyzer) 的插件，按东八区日期统计"每日获得的宝石数量"，并写入单一 JSON 文件。每次比赛结算后会在控制台打印当日累计宝石数。

## ✨ 特性

- 📦 **单一文件累计**：以 `yyyyMMdd` 为键累计每日宝石数，所有历史数据存储在一个 JSON 文件中
- 🌏 **东八区基准**：使用 China Standard Time 计算日期，确保跨时区一致性
- 📁 **自定义目录**：可通过 `JsonOutputDirectory` 指定输出目录（留空则使用默认路径）
- ⚡ **实时统计**：比赛结束后立即更新并显示当日累计数量
- 🔧 **极简配置**：无需复杂设置，开箱即用

## 🚀 快速开始

### 前置要求

- Windows 10/11 (推荐)
- .NET 8.0 Runtime
- [UmamusumeResponseAnalyzer](https://github.com/UmamusumeResponseAnalyzer/UmamusumeResponseAnalyzer) 主程序

### 安装方法

1. 从 [Releases](../../releases) 下载最新版本的 `gugugagaLovePlugin.dll`
2. 将 DLL 文件放入 UmamusumeResponseAnalyzer 的插件目录：
   ```
   %LOCALAPPDATA%\UmamusumeResponseAnalyzer\Plugins\gugugagaLovePlugin\
   ```
3. 重启 UmamusumeResponseAnalyzer
4. 开始游戏并进行比赛，插件会自动工作

## 📋 输出示例

### 控制台输出
```
今日已获得胡萝卜数量:37(37为实际数量) 🐧gugugaga!!!🐧
```

### JSON 文件结构
文件位置：`<输出目录>/gugugagaMagicCarrot.json`
```json
{
  "20250824": 37,
  "20250825": 12,
  "20250826": 25
}
```

## ⚙️ 配置说明

| 配置项 | 说明 | 默认值 | 示例 |
|--------|------|--------|------|
| `JsonOutputDirectory` | 每日宝石汇总文件的输出目录 | `./PluginData/<插件名>` | `C:\Users\YourName\Desktop\uma_data` |

## 🛠️ 从源码构建

### 环境要求
- Visual Studio 2022 或 .NET 8.0 SDK
- Windows 开发环境（推荐）

### 构建步骤

1. **克隆宿主项目**
   ```bash
   git clone https://github.com/UmamusumeResponseAnalyzer/UmamusumeResponseAnalyzer.git
   cd UmamusumeResponseAnalyzer
   ```

2. **克隆本插件项目**
   ```bash
   # 在与 UmamusumeResponseAnalyzer 同级目录下
   cd ..
   git clone <your-plugin-repo-url> gugugagaLovePlugin
   ```

3. **调整项目引用路径**
   
   打开 `gugugagaLovePlugin/src/RaceRewardTracker.csproj`，确认 ProjectReference 路径正确：
   
   ```xml
   <!-- 如果目录结构为：
        ├── UmamusumeResponseAnalyzer/
        └── gugugagaLovePlugin/
        则使用以下路径： -->
   <ProjectReference Include="..\..\UmamusumeResponseAnalyzer\UmamusumeResponseAnalyzer\UmamusumeResponseAnalyzer.csproj" />
   ```

4. **编译插件**
   ```bash
   cd gugugagaLovePlugin/src
   dotnet build -c Release
   ```

5. **部署**
   - Windows: PostBuild 会自动复制到插件目录
   - 其他平台: 手动复制 `bin/Release/net8.0/gugugagaLovePlugin.dll` 到插件目录

### 跨平台注意事项

- **时区兼容性**: 当前使用 "China Standard Time"，在 Linux 环境可能需要调整为 "Asia/Shanghai"
- **PostBuild 脚本**: 仅在 Windows 下有效，其他平台请手动部署

## 📖 开发文档

- [已知物品ID对照表](docs/KnownItemIds.md) - 维护宝石等物品的ID映射关系
- [项目设计说明](docs/项目设计.md) - 详细的架构设计与实现原理

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

### 新增宝石ID支持

1. 在 `docs/KnownItemIds.md` 中记录新的物品ID
2. 在 `Plugin.cs` 的 `KnownGemKeys` 集合中添加对应的 `(item_type, item_id)` 元组
3. 重新编译并测试

### 代码规范

- 遵循现有代码风格
- 添加必要的注释
- 确保线程安全

## 📝 版本历史

- **v1.0.2** - 单一JSON文件累计、东八区时间、控制台输出优化
- **v1.0.1** - 基础功能实现
- **v1.0.0** - 初始版本

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## ⚠️ 免责声明

- 本插件仅供学习和个人使用
- 插件作者不对因使用本插件造成的任何损失承担责任
- 请确保遵守游戏服务条款
- 本插件为非官方第三方工具

## 🔗 相关链接

- [UmamusumeResponseAnalyzer 主项目](https://github.com/UmamusumeResponseAnalyzer/UmamusumeResponseAnalyzer)
- [问题反馈](../../issues)
- [最新版本下载](../../releases)

---

**🐧 gugugaga!!! 🐧**