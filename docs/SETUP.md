# JREMonitors 插件安装运行/配置

本插件要求的最低操作系统为：Windows 10 版本 1703（Build
15063）及以上，并要求安装[BveEx](https://github.com/automatic9045/BveEX)框架。

## 前期准备

本插件不关联于具体的车辆数据，为保证通用性，请将插件目录置于场景文件夹下。

### 安装字体

本项目不附带界面所使用的字体，请自行在系统中安装以下字体，或将其置于插件目录下的`Fonts`文件夹中：

| 字体名称                       | 字重               | 备注          |
| -------------------------- | ---------------- | ----------- |
| Century Gothic             | Regular          | Windows 已内置 |
| Century Gothic Paneuropean | Regular、SemiBold | <br />      |
| Myriad Pro                 | SemiBold、Bold    | <br />      |
| Yu Gothic UI               | Bold             | Windows 已内置 |
| FOT-スーラ Pro                | Bold             | <br />      |
| FOT-スーラ Pro DB             | DemiBold         | <br />      |
| Arial                      | Regular、Bold     | Windows 已内置 |

### 插件与配置引用

```text
Scenarios/
├── JREMonitors/
│   └── BveEx.Plugins.JREMonitors.dll
├── Map/
│   ├── YourMap.txt
│   └── YourMap.JREMonitorConfig.json
├── Vehicles/
│   ├── YourVehicle.txt
│   ├── YourVehicle.VehiclePluginUsing.xml
│   └── YourVehicle.JREMonitorConfig.json
├── YourScenario.txt
└── YourScenario.JREMonitorConfig.json
```

请在YourVehicle.VehiclePluginUsing.xml中添加对JREMonitors程序集的引用：

```xml
<?xml version="1.0" encoding="utf-8"?>
<BveExPluginUsing>
    <Assembly Path="..\JREMonitors\BveEx.Plugins.JREMonitors.dll"/>
</BveExPluginUsing>
```

插件加载时，将按照Vehicle-Map-Scenario的顺序加载JSON配置，后加载的配置会和前一个配置进行合并。

配置中可使用`parent`字段引用父配置路径，插件加载时将自动进行合并。

对象与数组合并默认使用追加模式，可在子配置对应属性后加`=`符号覆盖父级对应属性。

## 配置说明

[Examples](examples)目录下有多个常见车辆数据的基础示例配置，可根据需要进行修改。

> \[!WARNING]
> **重要提示**
>
> 因DWM合成限制，当游戏渲染所使用的显卡、游戏窗口和外置窗口所处显示屏的显卡不同，且各显示器的刷新率不同时，画面变动时可能出现明显掉帧情况。若遇到此问题，请尝试以下方法：
>
> * 将游戏窗口移至连接的显卡和游戏使用显卡相同的显示器。
>
> * 在系统设置中将主显示器设置为连接的显卡和游戏使用显卡相同的显示器。
>
> * 将外置窗口设为全屏模式（右键菜单设置）。
>
> * 使用全屏模式启动游戏。
>
> * 让各显示器使用相同的显卡(如开启独显直连)。
>
> 此外，还需要关闭G-Sync/FreeSync。

以下以 TypeScript 类型声明描述车辆配置结构，字段注释说明含义、默认值与取值约束。

```typescript
/**
 * 基础车辆配置（配置文件根节点）。
 */
interface VehicleConfig {
    /** 使用的面板输入预设；预设为预定义的一组输入索引，车辆使用了对应插件时可直接引用。支持的值随车型变化。 */
    inputPresets?: string[];

    /**
     * 定义输入索引。键为该输入类型的名称，值为对应的面板索引（单个数字或数字数组）。
     * - 值为数组时，对应多个面板索引；
     * - 值为 `-1` 时，该输入对应的值恒为 `0`；
     * - 值为 `-2` 时，该输入对应的值恒为 `1`；
     * - 值为 `null`（或空数组）时，等效于未绑定任何索引。
     * - 部分输入已内置默认行为，未配置该输入时回退到默认行为。
     */
    inputs?: Record<string, number | number[] | null>;

    /** 监视器配置。键为监视器编号。 */
    monitors?: Record<string, MonitorConfig>;

    /** 显示/界面相关配置。 */
    display?: DisplayConfig;

    /** 输出配置。 */
    outputs?: {
        /**
         * 声音输出。键为声音名称，值为声音文件路径。目前仅 E233 支持，支持的键如下：
         * - `buttonClick`：按钮点击音效。
         * - `arrivalHint`：进站提示音效。
         * - `passHint`：通过提示音效。
         * - `airSectionHint`：分相区间提示音效。
         * - `radioChannelChange`：无线频道变更音效。
         */
        sound?: Record<string, string>;
    };

    /** 是否启用配置热重载。此属性仅在初始化阶段读取；启用后保存（写入）配置文件即可触发重载并自动应用变更，无需重启场景。 */
    enableHotReload?: boolean;
}

/** JRE系列车辆配置， */
interface JREVehicleConfig extends VehicleConfig {
    /**
     * 默认编组名称，支持的值随车型变化。
     * - E233-0：`4after6` | `6` | `10` | `10g` | `6+4` | `4after8` | `8` | `12` | `8+4`
     * - E233-1000：`10`
     * - E233-3000：`5` | `10` | `10+5`
     * - E233-5000：`4` | `6` | `6+4` | `10`
     */
    defaultFormation?: string;

    /**
     * 默认信号系统名称。仅 E233-1000 支持，取值为 `datc` | `atc6`；其余番台禁止填写。
     */
    defaultSignalSystem?: string;

    /**
     * 各编组所使用的性能曲线文件路径。键为编组名，值为曲线文件路径。
     * 某编组未配置时，将回退至车辆的初始性能。
     */
    performanceCurves?: Record<string, string>;

    /**
     * 各编组使用的车辆参数文件路径。键为编组名，值为车辆参数文件路径。
     * 某编组未配置或解析失败时，将回退至车辆文件中定义的参数路径。
     * 不会更新 `[OneLeverCab]`、`[Cab]`、`[ViewPoint]`。
     */
    vehicleParameters?: Record<string, string>;
}

/** E233系相关配置。 */
interface E233Config extends JREVehicleConfig {
    /** TIMS 相关配置。 */
    tims?: TIMSConfig;

    /**
     * 是否搭载 TASC 设备。
     * - E233-0、E233-1000：可设置，默认 `true`；
     * - E233-3000、E233-5000：固定为 `false`，填写该字段会导致配置校验失败。
     */
    supportsTasc?: boolean;
}

/** TIMS（乘务支援信息表示系统）配置。 */
interface TIMSConfig {
    /** 车辆行驶方向，默认为 `left`。 */
    vehicleDirection?: 'left' | 'right';

    /** 室温基准值。 */
    baseInteriorTemperature?: number | null;

    /** 外温基准值。 */
    externalTemperature?: number | null;

    /** 湿度基准值。 */
    baseHumidity?: number | null;

    /** IC 卡路径。启用热重载时，路径及对应内容变更均可自动响应。 */
    icCardPath?: string;

    /**
     * TIMS 画面 18 号点阵文字的字体族名称。未设置、字体不存在或缺字形时回退到 `MS Gothic`。
     * 此为监视器构建期定型配置，更改此属性将重建整个监视器系统。
     */
    timsFont18Family?: string;

    /**
     * M电/E电 秒数文字的 Y 轴偏移量，默认为 `1`（适配 `MS Gothic`）；自定义字体对位不齐时调整。
     * 此为监视器构建期定型配置，更改此属性将重建整个监视器系统。
     */
    timeTableSecondsOffsetY?: number | null;

    /** 调试功能，里程单位是否精确到米。 */
    showMileageInMeter?: boolean;
}

/** 监视器配置。 */
interface MonitorConfig {
    /**
     * 监视器原始画面分辨率。可选值及默认值随车型变化；E233系支持
     * `800x600`、`1024x768`（默认）、`1440x1080`、`1600x1200`、`1920x1440`。
     * 调高可提升清晰度，但会增加 GPU 负载。
     */
    resolution?: string;

    /** 监视器在游戏驾驶室内的相关配置。 */
    cab?: CabProjectionConfig;

    /** 监视器关联的外置显示窗口相关配置。 */
    external?: ExternalDisplayConfig;
}

/** 监视器在游戏驾驶室内的投影配置。 */
interface CabProjectionConfig {
    /** 是否将监视器画面投影到游戏驾驶室内，默认为 `true`。 */
    enabled?: boolean;
    /**
     * 定义监视器纹理的四边形顶点，据此决定 3D 透视效果，坐标系与 Panel 文件一致。
     * 必须为凸四边形，且面积必须大于 0。
     */
    positions: Quad2D;

    /** 纹理的旋转中心点，等价于 Needle.Origin。默认（`null`）为四边形的左上角。 */
    origin?: [number, number] | null;

    /** 纹理的旋转角度，单位为度，默认为 `0`。不影响 3D 透视效果应用，但合理设置可改善倾斜画面下的显示质量。 */
    rotation?: number;

    /** 纹理绘制顺序，默认显示在最上层，等价于 Needle.Layer。 */
    layer?: number | null;

    /** 纹理绕 X 轴的旋转角度（度），默认为 `0`。不影响 3D 透视效果应用，但合理设置可改善倾斜画面下的显示质量。 */
    tiltX?: number;

    /** 纹理绕 Y 轴的旋转角度（度），默认为 `0`。不影响 3D 透视效果应用，但合理设置可改善倾斜画面下的显示质量。 */
    tiltY?: number;

    /** 监视器画面圆角半径，默认为 `0`。此值相对于监视器的原始分辨率。 */
    borderRadius?: number;

    /** 动态光照配置。 */
    lighting?: LightingConfig;
}

/** 四边形顶点（坐标与 Panel 文件一致）。 */
interface Quad2D {
    topLeft: [number, number];
    topRight: [number, number];
    bottomLeft: [number, number];
    bottomRight: [number, number];
}

/** 动态光照配置。 */
interface LightingConfig {
    /** 是否启用动态光照，默认为 `true`。 */
    enabled?: boolean;

    /** 亮光完全生效的临界点。 */
    brightExtent?: [number, number] | null;

    /** 阴影完全生效的临界点。 */
    shadowExtent?: [number, number] | null;

    /** 夜间暗适应提亮倍数。此值在各车型中已配置默认值（E233 默认 1.3）。 */
    nightAdaptationGain?: number;

    /** 夜间调低亮度时亮度下降的快慢程度。此值在各车型中已配置默认值（E233 默认 0.55）。 */
    nightDimmingResponse?: number;

    /** 夜间高光压缩门限。此值在各车型中已配置默认值（E233 默认 0.75）。 */
    compressThresholdNight?: number;

    /** LCD 物理对比度。此值在各车型中已配置默认值（E233 默认 1200）。 */
    panelContrastRatio?: number;

    /** LCD 漏光基准色调。此值在各车型中已配置默认值（E233 默认 #E8EFFF）。 */
    leakColor?: Color3;

    /** 玻璃亮部反射率。 */
    glassReflectanceLight?: number;

    /** 玻璃暗部反射率。 */
    glassReflectanceDark?: number;

    /** 玻璃反光色调。 */
    glareColor?: Color3;
}

/** 监视器关联的外置显示窗口配置。 */
interface ExternalDisplayConfig {
    /** 监视器加载时是否同步显示外置窗口，默认为 `false`。可在右键菜单中手动显示或隐藏。 */
    show?: boolean;

    /** 外置窗口的画面缩放与对齐模式，默认为 `letterbox`。 */
    displayMode?: 'letterbox' | 'center' | 'stretch';
}

/** 显示/界面相关配置。 */
interface DisplayConfig {
    /**
     * 监视器画面残影衰减时间（秒）。默认 `0.1`，最小 `0`，最大 `0.5`。设为 0 可提高性能。
     */
    ghostingDecayTime?: number;

    /**
     * 帧缓冲区大小。设为 0 时使用默认值。
     * - D3D9 模式：默认 2，最小 1，最大 3；画面恒定延迟 `N-1` 帧；
     * - D3D9Ex 模式：默认 MaxFrameLatency+2，最小 3，最大 7；常规稳态延迟 1 帧，视 GPU 负载在 `[0, N-2]` 帧间自适应。
     * - 推荐设置为 `N >= MaxFrameLatency+2`，不足时会导致监视器内容帧率下降。
     */
    bufferFrameCount?: number;

    /** 调试选项：显示调试日志窗口。 */
    showDebugWindow?: boolean;

    /** 调试选项：显示监视器画面中各组件的绘制边界。 */
    showUiDebugRect?: boolean;

    /** 调试选项：显示游戏驾驶室内纹理的实际边界。 */
    showCabTextureBoundsRect?: boolean;
}

/**
 * 颜色。三种写法皆可：
 * - 十六进制字符串：`"#E8EFFF"`
 * - RGB 数组：`[0.91, 0.94, 1.0]`（值 >1 时按 0–255 归一化）
 * - RGB 对象：`{ r: 0.91, g: 0.94, b: 1.0 }`
 */
type Color3 =
    | string
    | [number, number, number]
    | { r: number; g: number; b: number };

/**
 * 路径类型。配置中写一个文件路径字符串即可，反序列化时会自动按配置文件所在目录
 * 解析为绝对路径对象。路径相对配置文件所在目录，也支持绝对路径。
 */
type ConfigPath = string;
```

以下以 TypeScript 类型声明描述 IC 卡相关配置结构，字段注释说明含义、默认值与取值约束。

```typescript
/**
 * IC 卡内容配置。路径由上层 `tims.icCardPath` 指定。
 * IC 卡未插入时，下列运行信息展示为空。
 */
interface TIMSICCardConfig {
    /** 运营区所。 */
    depot?: string;

    /** 行路番号。 */
    dutyNumber?: string;

    /** 运用列表。 */
    legs?: TIMSLegConfig[];
}

/** 单个运用。 */
interface TIMSLegConfig {
    /** 编组名称。支持的值随车型变化（参考各番台编组取值）。 */
    formation: string;

    /** 列车番号。 */
    trainNumber: string;

    /** 对应“?列番”。 */
    trainNumberChar?: string;

    /** 画面类型。 */
    displayMode: TIMSDisplayMode;

    /** 列车行先（停在降车站和目的地不同时使用）。 */
    overrideDestination?: TIMSDestinationConfig;

    /** 次行路。 */
    nextDuty?: TIMSNextDutyConfig;

    /** 切换等待时间。仅当非首个运用、且当前首站与上运用降车站重叠时生效。 */
    switchDuration?: number;

    /** 节点列表。 */
    routeNodes: TIMSRouteNode[];
}

/** 列车行先。 */
interface TIMSDestinationConfig {
    /** 终点站名称。 */
    name?: string;
}

/** 次行路。 */
interface TIMSNextDutyConfig {
    /** 列车番号。 */
    trainNumber: string;

    /** 到着时刻。 */
    arrivalTime?: string;

    /** 发车时刻。 */
    departureTime?: string;
}

/** 画面类型（TIMS 显示模式）。 */
type TIMSDisplayMode = "mDen" | "eDen";

/**
 * 路线节点（多态）。`type` 决定具体节点形态。
 * - `"station"`：车站
 * - `"slowSection"`：徐行区间
 * - `"airSection"`：分相区间
 * - `"mileageCorrection"`：里程矫正点
 * - `"signalSystemChange"`：信号系统切换点
 */
type TIMSRouteNode =
    TIMSStationConfig
    | TIMSSlowSectionConfig
    | TIMSAirSectionConfig
    | TIMSMileageCorrectionPointConfig
    | TIMSSignalSystemChangePointConfig;

/** 车站节点。 */
interface TIMSStationConfig {
    /** 节点判别符。 */
    type: "station";

    /** 车站 ID，用于匹配游戏线路中的车站。 */
    stationId: string;

    /** 站名显示文本，未配置时使用游戏线路中的车站名。 */
    displayName?: string;

    /** 是否为采时站，未配置时根据是否配置了到着/发车时刻自动推断。 */
    isTimingStation?: boolean | null;

    /** 覆盖到着时刻，未配置时使用游戏车站的到着时刻。 */
    overrideArrivalTime?: string | null;

    /** 运转时分。 */
    overrideStopDuration?: number | null;

    /** 覆盖发车时刻，未配置时使用游戏车站的发车时刻；终点站恒不生效。 */
    overrideDepartureTime?: string | null;

    /** 切至当前站的时机。未配置时自动推断。 */
    switchMode?: "minStopPosition" | "doorOpen" | null;

    /** 停站时是否显示“停”字样，仅在该站为停车站时生效。 */
    showStopText?: boolean;

    /** 运转速度，仅在 E 电生效；未配置时沿用上一站的值。 */
    standardOperatingSpeed?: string;

    /** 番线。 */
    trackName?: string;

    /** 该站对应的重映射里程，作为徐行区间等里程换算的参照点。 */
    remappedMileage?: number | null;

    /** 番线显示颜色。 */
    trackColor?: Color3 | null;

    /** 站名颜色，在 M 电时为文字颜色，在 E 电时为背景颜色。 */
    color?: Color3 | null;

    /** 站名文字颜色。仅在E电生效。 */
    eDenTextColor?: Color3 | null;

    /** 当前站距下一站的位置指示线颜色，仅在 E 电生效。 */
    lineColor?: Color3 | null;

    /** 当前站距下一站的位置指示线宽度，仅在 E 电生效。最小 1，最大 3。 */
    lineStrokeWidth?: number;

    /** 进站提示距站点的偏移量。 */
    arrivalHintOffset?: number;

    /** 无线电频道，未配置时沿用上一站的值。 */
    radioChannel?: string;

    /** 进站限速，仅在 M 电生效。 */
    speedLimitArrival?: string;

    /** 出站限速，仅在 M 电生效。 */
    speedLimitDeparture?: string;

    /** 控制里程加算/减算，默认为加算。 */
    mileageDirection?: "increment" | "decrement";

    /** 车站作业任务（待/整/分/併），默认不显示。 */
    stationTask?: "none" | "waiting" | "adjustment" | "decoupling" | "coupling";

    /** 停站类型（停车/通过），未配置时根据游戏车站自动推断。 */
    stopType?: "stop" | "pass" | null;

    /** 该站起生效的信号系统，仅在显式配置时生效，未配置时沿用默认信号系统。取值与 `defaultSignalSystem` 相同。 */
    activeSignalSystem?: string;

    /** 该站起适用的列车种别，未配置时沿用上一站的值。 */
    trainType?: TIMSTrainType;

    /** 信号系统切换等待时间。 */
    signalSystemSwitchDuration?: number;

    /** 列车种别切换等待时间。 */
    trainTypeSwitchDuration?: number;

    /** 站内闭塞起点距车站位置的偏移量。 */
    stationBlockStartOffset?: number;

    /** 站内闭塞终点距车站位置的偏移量。 */
    stationBlockEndOffset?: number;
}

/** 徐行区间节点。 */
interface TIMSSlowSectionConfig {
    /** 节点判别符。 */
    type: "slowSection";

    /** 徐行区间相对于游戏地图的起始位置。 */
    startLocation: number;

    /** 徐行区间相对于游戏地图的结束位置（实际终止点会自动加上编组长度）。 */
    endLocation: number;

    /** 徐行区间限速。 */
    speedLimit: number;
}

/** 分相区间节点。 */
interface TIMSAirSectionConfig {
    /** 节点判别符。 */
    type: "airSection";

    /** 分相区间相对于游戏地图的起始位置。 */
    startLocation: number;

    /** 分相区间相对于游戏地图的结束位置，不得小于 `startLocation`。 */
    endLocation: number;

    /** 进入提示距区间起点的偏移量：车辆位置到达 `startLocation - hintOffset` 时播放分相区间提示音（`airSectionHint`）。 */
    hintOffset: number;
}

/** 里程矫正点节点。 */
interface TIMSMileageCorrectionPointConfig {
    /** 节点判别符。 */
    type: "mileageCorrection";

    /** 断里程矫正点相对于游戏地图的位置。 */
    location: number;

    /** 重映射里程。 */
    remappedMileage: number;

    /** 控制里程加算/减算，默认为加算。 */
    mileageDirection?: "increment" | "decrement";
}

/** 信号系统切换点节点。 */
interface TIMSSignalSystemChangePointConfig {
    /** 节点判别符。 */
    type: "signalSystemChange";

    /** 切换点相对于游戏地图的位置。 */
    startLocation: number;

    /** 切换点后生效的信号系统。取值与 `defaultSignalSystem` 相同。 */
    signalSystem: string;
}

/** 列车种别。 */
type TIMSTrainType = "local" | "rapid";
```

下面说明各车型（番台）支持的输入索引预设（`inputPresets`）与输入索引（`inputs`）。
输入索引预设统一在下方列出；输入索引按配置继承结构从父到子（基础层 → JRE → E233 → E233-0 → E233-1000）依次列出。

### 输入索引预设（`inputPresets`）

输入索引预设为预定义的一组输入索引，车辆可直接引用；各番台支持的预设见「输入索引」一节中对应番台的小结。

#### `GAP-ATS-P`

ATS-P 系列信号插件相关索引。

| 索引名称                  | 面板索引 |
| --------------------- | ---- |
| `atsPPower`           | `2`  |
| `atsPPatternApproach` | `3`  |
| `atsPBrakeCutout`     | `4`  |
| `atsPServiceBrake`    | `5`  |
| `atsPEnabled`         | `6`  |
| `atsPFailure`         | `7`  |
| `atsPEmergencyBrake`  | `8`  |

#### `GAP-ATS-S`

ATS-S 系列信号插件相关索引。

| 索引名称            | 面板索引 |
| --------------- | ---- |
| `atsSPower`     | `0`  |
| `atsSActivated` | `1`  |

#### `ATC-6`

ATC-6 信号插件相关索引。

| 索引名称                  | 面板索引 |
| --------------------- | ---- |
| `atcSpeed0`           | `71` |
| `atcSpeed15`          | `72` |
| `atcSpeed25`          | `73` |
| `atcSpeed45`          | `74` |
| `atcSpeed55`          | `75` |
| `atcSpeed65`          | `76` |
| `atcSpeed75`          | `77` |
| `atcSpeed90`          | `78` |
| `atcSpeed100`         | `79` |
| `atcSpeed110`         | `80` |
| `atcSpeed120`         | `81` |
| `atc6PatternApproach` | `68` |
| `atc6Shunt`           | `69` |
| `atc6AbsoluteStop`    | `70` |
| `atc6ServiceBrake`    | `58` |
| `atc6EmergencyBrake`  | `57` |
| `atc6Power`           | `52` |
| `atc6TurnOff`         | `54` |

#### `Mi5000-DATC`

Mi5000-DATC 信号插件相关索引。

| 索引名称                  | 面板索引     |
| --------------------- | -------- |
| `datcSpeedLimit`      | `67`     |
| `datcPatternApproach` | `68`     |
| `datcAbsoluteStop`    | `70`     |
| `datcServiceBrake`    | `58, 65` |
| `datcEmergencyBrake`  | `57`     |
| `datcPower`           | `52`     |
| `datcTurnOff`         | `54`     |

### 输入索引（`inputs`）

输入索引按配置继承结构从父到子列出；各番台支持的输入索引预设见对应番台的小结。

#### 基础层（Builtin）

| 索引名称         | 功能   | 补充                 |
| ------------ | ---- | ------------------ |
| `brakeNotch` | 制动级位 | 未配置时回退到读取游戏制动级位状态。 |

#### JRE 层（JREVehicleConfig）

| 索引名称            | 功能   | 补充                |
| --------------- | ---- | ----------------- |
| `constantSpeed` | 定速   | 未配置时回退到读取游戏定速状态。  |
| `deviceVoltage` | 设备电压 | 未配置时回退到默认值 `105`。 |

#### E233 层（E233Config）

| 索引名称              | 功能   | 补充                 |
| ----------------- | ---- | ------------------ |
| `catenaryVoltage` | 架线电压 | 未配置时回退到默认值 `1500`。 |

#### E233-0 层

支持输入索引预设：`GAP-ATS-P`、`GAP-ATS-S`。

| 索引名称                      | 功能           | 补充                                                  |
| ------------------------- | ------------ | --------------------------------------------------- |
| `holdSpeed`               | 抑速           | 配置此选项时必须同时指定定速索引，否则校验失败。                            |
| `atsPPower`               | P電源          | <br />                                              |
| `atsPPatternApproach`     | パターン接近       | <br />                                              |
| `atsPServiceBrake`        | 常用ブレーキ       | <br />                                              |
| `atsPEmergencyBrake`      | 非常ブレーキ       | <br />                                              |
| `atsPBrakeCutout`         | ブレーキ開放       | <br />                                              |
| `atsPEnabled`             | ATS-P        | <br />                                              |
| `atsPFailure`             | 故障           | <br />                                              |
| `atsSPower`               | ATS電源        | <br />                                              |
| `atsSActivated`           | ATS動作        | <br />                                              |
| `tascBrakeNotch`          | TASC制动级位     | <br />                                              |
| `tascPower`               | TASC電源       | 未配置时回退为点亮。                                          |
| `tascPattern`             | TASCパターン     | <br />                                              |
| `tascBrake`               | TASCブレーキ     | <br />                                              |
| `tascEnabled`             | 是否关闭TASC切    | 与 `tascDisabled` 功能互斥，二者不能同时定义。                     |
| `tascDisabled`            | 是否显示TASC切    | 与 `tascEnabled` 功能互斥，二者不能同时定义。                      |
| `tascFailure`             | TASC故障       | <br />                                              |
| `tascFixedDistance`       | 定位置          | <br />                                              |
| `vehicleDoorAllClosed`    | 車両ドア全閉       | 未配置时回退到游戏车门全闭状态。                                    |
| `platformDoorAllClosed`   | ホームドア全閉      | <br />                                              |
| `platformInterlocking`    | ホームドア連携      | <br />                                              |
| `platformDecoupling`      | ホームドア分離      | <br />                                              |
| `tascHoldingBrake`        | 転動防止ブレーキ     | <br />                                              |
| `platformDoorCutout`      | ホームドア開放      | <br />                                              |
| `carDoor1` \~ `carDoor12` | 对应车厢车门整体开闭状态 | 若配置则只能模拟到该节车厢整体的门开闭状态；未配置则可根据游戏内数据独立模拟该车厢内每扇门的开闭状态。 |

#### E233-1000 层

支持输入索引预设：`ATC-6`、`Mi5000-DATC`。

| 索引名称                      | 功能               | 补充                                                  |
| ------------------------- | ---------------- | --------------------------------------------------- |
| `atcSpeed0`               | ATC速度制限（0km/h）   | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed15`              | ATC速度制限（15km/h）  | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed25`              | ATC速度制限（25km/h）  | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed45`              | ATC速度制限（45km/h）  | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed55`              | ATC速度制限（55km/h）  | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed65`              | ATC速度制限（65km/h）  | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed75`              | ATC速度制限（75km/h）  | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed90`              | ATC速度制限（90km/h）  | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed100`             | ATC速度制限（100km/h） | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed110`             | ATC速度制限（110km/h） | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atcSpeed120`             | ATC速度制限（120km/h） | 仅在信号系统为 `atc6` 时生效；不是可配置输入，仅作 `ATC-6` 预设映射。         |
| `atc6Shunt`               | 入換               | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6AbsoluteStop`        | 絶対停止             | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6PatternApproach`     | パターン接近           | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6InchingActivated`    | インチング制御中         | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6TurnOff`             | 切                | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6PatternCleared`      | パターン低滅           | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6EmergencyRun`        | 非常運転             | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6ServiceBrake`        | ATC常用            | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6EmergencyBrake`      | ATC非常            | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6OverrunAction`       | 停通防止動作           | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6Power`               | ATC電源            | 仅在信号系统为 `atc6` 时生效。                                 |
| `atc6Cutout`              | ATC開放            | 仅在信号系统为 `atc6` 时生效。                                 |
| `datcSpeedLimit`          | ATC速度制限          | 仅在信号系统为 `datc` 时生效。                                 |
| `datcShunt`               | 入換               | 仅在信号系统为 `datc` 时生效。                                 |
| `datcAbsoluteStop`        | 絶対停止             | 仅在信号系统为 `datc` 时生效。                                 |
| `datcPatternApproach`     | パターン接近           | 仅在信号系统为 `datc` 时生效。                                 |
| `datcInchingActivated`    | インチング制御中         | 仅在信号系统为 `datc` 时生效。                                 |
| `datcTurnOff`             | 切                | 仅在信号系统为 `datc` 时生效。                                 |
| `datcPatternCleared`      | パターン低減           | 仅在信号系统为 `datc` 时生效。                                 |
| `datcEmergencyRun`        | 非常運転             | 仅在信号系统为 `datc` 时生效。                                 |
| `datcServiceBrake`        | ATC常用            | 仅在信号系统为 `datc` 时生效。                                 |
| `datcEmergencyBrake`      | ATC非常            | 仅在信号系统为 `datc` 时生效。                                 |
| `datcOverrunAction`       | 停通防止動作           | 仅在信号系统为 `datc` 时生效。                                 |
| `datcPower`               | ATC電源            | 仅在信号系统为 `datc` 时生效。                                 |
| `datcCutout`              | ATC開放            | 仅在信号系统为 `datc` 时生效。                                 |
| `atcHoldingBrakeActive`   | 転動防止動作           | <br />                                              |
| `tascBrakeNotch`          | TASC制动级位         | <br />                                              |
| `tascPower`               | TASC電源           | 未配置时回退为点亮。                                          |
| `tascPattern`             | TASCパターン         | <br />                                              |
| `tascBrake`               | TASCブレーキ         | <br />                                              |
| `tascEnabled`             | 是否关闭TASC切        | 与 `tascDisabled` 功能互斥，二者不能同时定义。                     |
| `tascDisabled`            | 是否显示TASC切        | 与 `tascEnabled` 功能互斥，二者不能同时定义。                      |
| `tascFailure`             | TASC故障           | <br />                                              |
| `tascFixedDistance`       | 定位置              | <br />                                              |
| `vehicleDoorAllClosed`    | 車両ドア全閉           | 未配置时回退到游戏车门全闭状态。                                    |
| `platformDoorAllClosed`   | ホームドア全閉          | <br />                                              |
| `platformInterlocking`    | ホームドア連携          | <br />                                              |
| `platformDecoupling`      | ホームドア分離          | <br />                                              |
| `carDoor1` \~ `carDoor10` | 对应车厢车门整体开闭状态     | 若配置则只能模拟到该节车厢整体的门开闭状态；未配置则可根据游戏内数据独立模拟该车厢内每扇门的开闭状态。 |

#### E233-3000 层

支持输入索引预设：`GAP-ATS-P`、`GAP-ATS-S`。

| 索引名称                      | 功能           | 补充                                                  |
| ------------------------- | ------------ | --------------------------------------------------- |
| `holdSpeed`               | 抑速           | 配置此选项时必须同时指定定速索引，否则校验失败。                            |
| `atsPPower`               | P電源          | <br />                                              |
| `atsPPatternApproach`     | パターン接近       | <br />                                              |
| `atsPServiceBrake`        | 常用ブレーキ       | <br />                                              |
| `atsPEmergencyBrake`      | 非常ブレーキ       | <br />                                              |
| `atsPBrakeCutout`         | ブレーキ開放       | <br />                                              |
| `atsPEnabled`             | ATS-P        | <br />                                              |
| `atsPFailure`             | 故障           | <br />                                              |
| `atsSPower`               | ATS電源        | <br />                                              |
| `atsSActivated`           | ATS動作        | <br />                                              |
| `carDoor1` \~ `carDoor15` | 对应车厢车门整体开闭状态 | 若配置则只能模拟到该节车厢整体的门开闭状态；未配置则可根据游戏内数据独立模拟该车厢内每扇门的开闭状态。 |

#### E233-5000 层

支持输入索引预设：`GAP-ATS-P`、`GAP-ATS-S`。

| 索引名称                      | 功能           | 补充                                                  |
| ------------------------- | ------------ | --------------------------------------------------- |
| `atsPPower`               | P電源          | <br />                                              |
| `atsPPatternApproach`     | パターン接近       | <br />                                              |
| `atsPServiceBrake`        | 常用ブレーキ       | <br />                                              |
| `atsPEmergencyBrake`      | 非常ブレーキ       | <br />                                              |
| `atsPBrakeCutout`         | ブレーキ開放       | <br />                                              |
| `atsPEnabled`             | ATS-P        | <br />                                              |
| `atsPFailure`             | 故障           | <br />                                              |
| `atsSPower`               | ATS電源        | <br />                                              |
| `atsSActivated`           | ATS動作        | <br />                                              |
| `carDoor1` \~ `carDoor10` | 对应车厢车门整体开闭状态 | 若配置则只能模拟到该节车厢整体的门开闭状态；未配置则可根据游戏内数据独立模拟该车厢内每扇门的开闭状态。 |

