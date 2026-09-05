# JREMonitors

JREMonitors 是一款基于 Direct2D 渲染、为 BVE Trainsim 5.8/6 开发的列车电子屏监视器显示插件。

## 截图

|                  E233-0(saha_Common)                  |                  E233-1000(SotoHane)                  |                  E233-3000(SotoHane)                  |
|:-----------------------------------------------------:|:-----------------------------------------------------:|:-----------------------------------------------------:|
| ![E233-0(saha_Common)](docs/assets/screenshot-01.png) | ![E233-1000(SotoHane)](docs/assets/screenshot-02.png) | ![E233-3000(SotoHane)](docs/assets/screenshot-03.png) |

## 核心功能

- 基于四顶点坐标配置游戏内监视器纹理位置，自动处理3D透视
- 游戏内画面点击交互，自动处理3D透视及驾驶室晃动
- 游戏内画面支持动态光照，模拟阳光下渐变反光以及黑暗环境下画面主观亮度提高
- 外置监视器画面显示及交互
- 画面残影效果模拟及分辨率调节
- 全配置支持热重载，配置变更时无需重载线路即可应用最新更改
- E233系
  - 已支持0番台、1000番台、3000番台、5000番台
  - TIMS画面已实现：仪表盘、保安表示灯、S00AA(準備中画面)、S00AB(初期選択)、D00AA(運転士メニュー)、D01AA/D01AB(
      M電/E電運転情報)、D02AA(車両情報)、D05AA/D05AB(ブレーキ確認情報)、C00AA(車掌メニュー)、C01AA/C01AB(車掌情報)
  - IC卡配置仅需填写各站ID即可自动处理大部分数据显示
  - 不同运行区间可指定不同编组及对应的车辆参数、性能曲线文件路径，自动更改游戏内车厢数量、车辆参数以及性能曲线

## 项目架构

| 工程 | 定位 | 说明 |
| --- | --- | --- |
| `JREMonitors.Core` | 基础内核 | 一个基于 `Vortice.Direct2D1` 的响应式 + 保留模式渲染引擎，内置画面按需更新、冻结检测、行扫描式渐进刷新及残影效果。 |
| `JREMonitors.JRE` | 通用车辆逻辑 | JR东日本系车辆通用逻辑，当前内容较少，后续将逐步迁移至本工程。 |
| `JREMonitors.E233` | 车型实现 | E233 系的 UI 实现与业务逻辑。 |
| `JREMonitors.BveEx` | 框架适配层 | BveEx 适配：车辆多态配置解析、车辆服务适配、游戏内纹理显示与热重载。 |
| `JREMonitors.SandBox` | 调试沙盒 | 独立调试入口，可离线预览 UI 与业务逻辑。 |

依赖关系自下而上：`Core` ← `JRE` ← `E233` ← `BveEx`，`SandBox` 独立于游戏运行。

## Direct3D9Ex API 支持

本插件的游戏内画面同步逻辑针对 Direct3D9Ex
进行了特殊适配，配合[BveEx.Plugins.D3D9DeviceHacker](https://github.com/Litrix007/BveEx.Plugins.D3D9DeviceHacker)
，游戏内画面同步无需经过CPU中转，缩短耗时。

## 安装运行/配置

本插件要求的最低操作系统为：Windows 10 版本 1703（Build
15063）及以上，并要求安装[BveEx](https://github.com/automatic9045/BveEX)框架。

详见 [SETUP.md](docs/SETUP.md)。

## 构建

### 环境要求

- Windows 10/11 + Visual Studio / JetBrains Rider（含 .NET Framework 4.8 开发包）
- Windows 10 SDK（构建时会用其 `fxc.exe` 将 HLSL 编译为 `*.cso` 并嵌入资源）
- BVE 5.8/6。BveEx 工程的 `BveTs.exe` 与 `Mackoy.XmlInterfaces.dll`
  通过 `HintPath` 引用本地 BVE 安装目录，请按下文说明修改为
  `(your-bve-installation-path)`

### 调整路径

`JREMonitors.Core.csproj` 的 `CompileHLSL` target 硬编码了本机 Windows SDK 路径：

- `FxcAbsoluteExe` → `D:\Windows Kits\10\bin\<版本>\x64\fxc.exe`
- `IncludeHeaders` → `D:\Windows Kits\10\Include\<版本>\um`

若你安装的 Windows SDK 版本不同，需在构建前修改这两个属性；BVE 路径同理，修改
`JREMonitors.BveEx.csproj` 中的 `HintPath` 为 `(your-bve-installation-path)\Mackoy.XmlInterfaces.dll`。

## 免责声明

本项目为 BVE TrainSim 的非官方同人二次创作插件，仅供铁道模拟爱好者交流与学习使用。

本项目中的 "JRE" 仅指代 JR 东日本（East Japan Railway Company）旗下列车车型。

本项目与东日本旅客铁道株式会社（JR East）无任何官方关联、赞助或授权关系。作品中涉及的列车设计、商标及名称版权均归其各自所有者所有。

## 依赖

### [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) (MIT)

### [SlimDX](https://github.com/SlimDX/slimdx) (MIT)

### [BveEx](https://github.com/automatic9045/BveEX) (PolyForm Noncommercial License 1.0.0)
