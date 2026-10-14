# NatAsset

NatAsset 是一个 Unity 资源管理框架，包含资源收集与 AssetBundle 构建、编辑器 Bundle 测试、异步资源和场景加载、句柄管理、依赖感知的延迟卸载，以及资源版本检查和下载代码。

框架仍处于开发阶段。本文以当前公开 API 和实际代码路径为准；平台枚举和实现分支不代表已完成对应平台的发布验证。原生平台热更新尚未全部完成，等待后续完善。

## 环境与当前能力

| 项目 | 当前仓库配置或行为 |
| --- | --- |
| Unity | 工程记录为 **2020.3.33f1c2**，见 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) |
| 构建依赖 | `com.unity.scriptablebuildpipeline` **1.19.6**，见 [manifest.json](Packages/manifest.json) |
| 构建管线 | **需要 SBP**。程序集通过包版本定义 `NATASSET_SBP_SUPPORT`；未启用时构建入口会抛错，不会自动退回 Built-in 管线 |
| 构建目标 | `StandaloneWindows64`、`Android`、`iOS`、`WebGL` |
| 资源加载 | 公开入口为异步加载，支持泛型、`Type` 参数和同类型批量加载 |
| 资源句柄 | 支持完成回调和 `await`；需要检查结果并管理释放 |
| 场景 | 异步加载，支持 `Single` / `Additive`；附加场景可通过句柄卸载 |
| 编辑器测试 | AssetDatabase 直读，或使用指定构建版本的 Bundle |
| 原生平台热更新 | 尚未全部完成，等待后续完善 |
| 清单格式 | `NatAssetSetting.NatAssetVersion = 2`；旧格式清单需要重新构建 |

运行时代码命名空间为 `NATFrameWork.NatAsset.Runtime`，编辑器代码命名空间为 `NATFrameWork.NatAsset.Editor`，注意实际拼写。

## 快速开始

1. 使用上述 Unity 版本打开工程，等待 Package Manager 恢复依赖并完成编译。
2. 选中 `Assets/NatAssetSObj.asset`，将 `RunWay` 设为 `Editor`。此模式直接读取工程资源，不需要先构建 Bundle。
3. 工程已有 `Assets/Scenes/SampleScene.unity`、`Assets/Scripts/Launch.cs`、`Assets/Scripts/UIDemo.cs` 和 `Assets/Res/Prefabs/Cube.prefab`，可先阅读示例。
4. 自行接入时，全局只保留一套初始化和每帧驱动。下面的脚本可放在独立场景的根对象上；不要与现有 `Launch` 同时驱动框架。

```csharp
using NATFrameWork.NatAsset.Runtime;
using UnityEngine;

public sealed class NatAssetQuickStart : MonoBehaviour
{
    private GameObject _instance;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        NatAssetUtil.Init(
            new StandaloneModeLoader(),
            OnInitialized,
            error => Debug.LogError(error));
    }

    private void OnInitialized()
    {
        AssetHandle handle = NatAssetMgr.LoadAssetAsync<GameObject>(
            "Assets/Res/Prefabs/Cube.prefab");
        handle.OnLoaded += loaded =>
        {
            if (!loaded.IsSuccess)
            {
                Debug.LogError("Cube 加载失败，请检查 Console。");
                loaded.Dispose();
                return;
            }

            _instance = Instantiate((GameObject)loaded.Asset);
            loaded.OnBind(_instance);
        };
    }

    private void Update()
    {
        NatAssetUtil.Update();
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);
        NatAssetUtil.Release();
    }
}
```

初始化完成后才能加载资源。`NatAssetUtil.Update()` 在初始化过程中也必须持续调用，用于推进清单请求和后续任务。需要切换场景时，应让驱动对象跨场景存活。

资源使用完整工程路径，例如 `Assets/Res/Prefabs/Cube.prefab`，包含扩展名，并保持原始大小写。Bundle 模式下，资源及其依赖还必须登记在当前清单中。

## 运行模式与清单来源

`RunWay` 和传给 `Init` 的清单加载器是两层配置：前者决定编辑器是否直接读取资源，后者决定 Bundle 模式从哪里读取清单。

| 配置 | 行为 |
| --- | --- |
| `RunWay.Editor` | 仅编辑器有效。通过 AssetDatabase 加载资源，初始化不读取 Bundle 清单 |
| `RunWay.PackageOnly` | 编辑器使用已应用的 Bundle 和清单；Player 固定走此运行方式 |
| `StandaloneModeLoader` | 使用随包清单；编辑器中使用 `<输出根目录>/<平台>/Editor` 下的测试清单 |
| `HostModeLoader` | 读取随包区和读写区清单，查找资源时读写区优先；不会自动发起远端版本更新 |
| `WebModeLoader` | 读取随包和远端清单，查找资源时远端优先；用于 WebGL 资源访问流程 |

`StandaloneModeLoader` 会检查清单格式、非空 Bundle 列表和平台；编辑器中还检查测试目录的同步状态、Bundle 文件存在性及长度，失败时调用 `Init` 的 `onFailure`。

`HostModeLoader` 对应的原生平台热更新尚未全部完成，等待后续完善。

源码：[清单加载器](Assets/NatAsset/Runtime/DataInfo/ManifestModeLoader)、[运行配置](Assets/NatAsset/Runtime/DataInfo/NatAssetSetting.cs)。

## 编辑器：收集、构建和测试资源

### 配置资产与菜单

- `Assets/NatAssetBuildSetting.asset`：保存收集规则、构建平台、输出目录、压缩和加密等设置。
- `Assets/NatAssetSObj.asset`：保存编辑器运行方式。
- 若缺少配置资产，框架在访问单例时会创建；也可通过 `Assets > Create > NATFramework` 创建。每种配置保留一份。
- 构建窗口：**NatAsset > BuildBundleWindow**。
- 分析器：**NatAsset > Profiler**。
- Shader 变体工具：**NatAsset > ShaderVariantCollectionWindow**。

构建窗口包括 `BundleSetting`、`PreBundlesView`、`Build`、`Version` 四个页签。

### 1. 设置收集规则

在 `BundleSetting` 中使用 `AddFolderPackage` 添加资源文件夹，选中 Package 后设置 `Collector`、`Encrypt` 和 `Group`。初次尝试可添加 `Assets/Res/Prefabs` 文件夹。

| 收集器 | 当前规则 |
| --- | --- |
| `NAssetToOneBundle` | 将 Package 中的资源集中到一个 Bundle，是默认收集器 |
| `NAssetToNBundle` | 按资源分别生成 Bundle |
| `NFolderToNBundle` | 无子目录的文件夹集中分包，其他层级继续递归，散落文件单独分包 |

`NSubFolderToNBundle`、`IGnore` 目前是占位类，没有实现 `IBuildCollector`，不能作为可用收集器。界面虽然保留了 `AddFilePackage`，但当前三个内置收集器在根节点 `Child == null` 时直接返回，单文件 Package 不会产生有效收集结果；请使用文件夹 Package 或自行扩展收集器。

在 `PreBundlesView` 点击 `Refresh` 检查结果。收集流程会分析隐式依赖、提取共享资源并拆分场景资源；预览结果不一定与手动划分的 Package 一一对应。

### 2. 构建资源版本

在 `Build` 页中：

1. 在 `ChooseBatchBuild` 选择目标平台，并确保安装了对应 Unity 构建模块。
2. 确认 `Output Path`、`Compression`、`GlobalEncrypt`、`Append Hash`。默认输出目录为工程根目录下的 `AssetBundles`，默认压缩为 LZ4。
3. 点击 `BundleBuild`。当前窗口通过 `NatAssetBuildService.BuildResources` 进行构建和校验。

`Output Path` 和 `Version` 在当前 Build 页是显示字段，输入不会保存；输出根目录来自 `NatAssetBuildSetting`。构建服务自动分配 `yyyy-MM-dd-HH-mm` 版本号，同一分钟重复构建时顺延，避免覆盖已有版本。

```text
AssetBundles/
└── StandaloneWindows64/
    ├── 2026-10-14-12-00/
    │   ├── NatAssetConfig.data
    │   ├── NatAssetConfig.data.json
    │   ├── nat-asset-resource.json
    │   └── ...Bundle 文件及其子目录
    └── Editor/
        ├── NatAssetConfig.data
        ├── nat-editor-resource.json
        └── ...当前测试版本的 Bundle
```

二进制 `NatAssetConfig.data` 是运行时使用的清单；JSON 便于检查构建信息。清单记录资源路径、资源依赖、卸载策略，以及平台、版本、Bundle 依赖、Hash、MD5、CRC、长度和文件路径等信息。

构建服务校验版本格式、平台、Bundle 文件长度、MD5 和依赖完整性。`Append Hash` 会改变实际文件名，部署时必须按照清单中的 `RelativePath` 保留相对目录和文件名。

原生平台可使用 `Nono` 或 `Offset`；Package 的 `Global` 表示跟随全局配置。`Offset` 只是添加 32 字节头部的偏移处理。**WebGL 构建强制不加密**，忽略全局及 Package 的加密选择。

源码：[构建服务](Assets/NatAsset/Editor/UtilHelper/NatAssetBuildService.cs)、[收集流程](Assets/NatAsset/Editor/UtilHelper/NatAssetBuildUtil.cs)、[SBP 构建](Assets/NatAsset/Editor/Core/Controller/Build/NatBuildConfig.SBP.cs)。

### 3. 应用到编辑器 Bundle 测试

在 `Build` 页的“编辑器 Bundle 测试资源”区域选择测试资源平台和更新模式：

- **自动跟随资源构建**：该平台资源构建成功后，自动同步到 `<输出根目录>/<平台>/Editor`。切换为自动模式本身不会立即应用已有版本。
- **手动指定版本**：选择版本或资源目录，点击“应用到编辑器测试”；后续构建不会自动替换当前测试版本。

手动应用前，将 Unity 当前构建平台切换为所选平台，并退出 Play Mode。播放、编译、资源导入或其他构建占用期间，不能构建或替换测试资源。

应用后，将 `NatAssetSObj.RunWay` 切换为 `PackageOnly`，使用 `StandaloneModeLoader` 初始化。确认界面显示的“当前生效”版本和来源；切换的是 Bundle，运行的 C# 仍是当前工程代码。

版本同步采用暂存目录、备份和恢复日志，并保留未被旧清单管理的文件。它不会自动清空已有的 `SandBox` / `TempCache`；测试旧版本时若使用 `HostModeLoader`，读写区中的覆盖资源仍可能影响结果。

每个平台的选择保存在 `UserSettings/NatAssetEditorResources.asset`。资源构建成功与测试目录同步成功是两个结果，发生同步警告时应检查当前生效版本。

源码：[版本选择](Assets/NatAsset/Editor/UtilHelper/NatAssetEditorResources.cs)、[测试资源同步](Assets/NatAsset/Editor/UtilHelper/EditorResourceSync.cs)。

### 4. 部署随包资源

Player 的随包目录为 `Application.streamingAssetsPath/BuildInRes`。部署完整的二进制清单及其引用的全部 Bundle，并保持相对路径。

`Version` 页可以按平台选择版本，按资源组拷贝；组名使用英文分号分隔，留空表示全部复制。

**当前 Version 页的“拷贝资源组至StreamingAsset”按钮会先递归删除整个 `Application.streamingAssetsPath`，而不只是 `BuildInRes`。** 若该目录还存放其他文件，应自行部署所需资源，或使用下述只写入指定空目录的编辑器 API。

### 编辑器脚本接入

放在 `Editor` 目录或编辑器程序集中的方法可使用：

```csharp
using NATFrameWork.NatAsset.Editor;
using UnityEngine;

public static class BuildNatResources
{
    public static void Build()
    {
        ResourceBuildResult result = NatAssetBuildService.BuildResources(
            ValidBuildTarget.StandaloneWindows64);
        Debug.Log($"资源输出：{result.Directory}，版本：{result.Version}");
        Debug.Log($"编辑器同步：{result.EditorSyncStatus} {result.EditorSyncWarning}");
    }
}
```

其他入口：`InspectResourceVersion(directory, target)` 校验已有版本，`CopyResourceVersion(sourceDirectory, destination, target)` 将清单和 Bundle 复制到空目录，`EditorResourceSync.Apply(source, buildTarget)` 应用测试版本。

新构建脚本应使用 `NatAssetBuildService`；旧的 `NatAssetBuildUtil.BuildAssetBundles` 仍存在，但不包含服务层的完整版本分配、构建后校验和编辑器同步流程。

## 运行时 API

以下资源操作要求先完成初始化，并持续调用 `NatAssetUtil.Update()`。片段放在业务方法内，包含 `await` 的片段放在 `async Task` 方法内。类型来自 `NATFrameWork.NatAsset.Runtime`，按需引入 `System.Collections.Generic`、`System.Threading.Tasks` 和 Unity 命名空间。

### 异步加载与句柄

```csharp
AssetHandle handle = await NatAssetMgr.LoadAssetAsync<GameObject>(
    "Assets/Res/Prefabs/Cube.prefab", Priority.High);

if (!handle.IsSuccess)
{
    handle.Dispose();
    return;
}

GameObject instance = UnityEngine.Object.Instantiate((GameObject)handle.Asset);
handle.OnBind(instance);
```

非泛型入口为 `LoadAssetAsync(string targetPath, Type type, Priority priority = Priority.Low)`。也可以订阅 `handle.OnLoaded`，如快速开始示例。

- `await` 返回句柄本身，**加载失败不会由 awaiter 自动转换成异常**；读取资源前检查 `IsSuccess`。
- 可检查 `IsValid`、`IsDone`、`IsLoading`、`IsSuccess`、`IsFaile`，其中 `IsFaile` 是当前 API 的实际拼写。
- `OnLoaded` 在成功或失败完成时触发；对已完成的有效句柄订阅时会立即调用，不保证延后到下一帧。
- 手动管理时，在使用结束后调用 `handle.Dispose()` 或 `NatAssetMgr.UnLoadAsset(handle)`，再清除自己的引用。释放句柄不等于销毁已实例化的 GameObject。
- `handle.OnBind(gameObject)` 将该句柄交给对象生命周期管理，对象销毁时释放。不要再将同一句柄交给多个对象共同管理，或在对象仍使用资源时提前释放。
- `AssetHandle`、`BatchAssetHandle`、`SceneHandle` 当前释放后不会回收到引用池供新请求复用；旧句柄仍不可继续使用。
- 释放尚未完成的句柄会取消它的通知并解除关联。不要一边 `await` 句柄、一边从其他逻辑 `Dispose` 它，再期待等待正常返回。

优先级从低到高为 `Bottom`、`Low`、`Middle`、`High`、`Top`，默认 `Low`。

### 实例化预制体

```csharp
// 明确传入 Priority，选择返回 Task<GameObject> 的重载。
GameObject instance = await NatAssetMgr.InstanceObjAsync(
    "Assets/Res/Prefabs/Cube.prefab", Priority.Low);

if (instance == null) return;

// 使用结束时销毁对象，内部绑定的资源句柄随之释放。
UnityEngine.Object.Destroy(instance);
```

回调重载为 `InstanceObjAsync(string targetPath, Action<GameObject> callback = null, Priority priority = Priority.Low)`，返回 `void`；失败时记录错误，不调用成功回调。当前公开 API 中没有旧 README 所写的 `LoadAsset` / `LoadAsset<T>` / `InstanceObj` 同步入口。

### 批量加载

```csharp
var paths = new List<string> { "Assets/Res/Prefabs/Cube.prefab" };
BatchAssetHandle batch = NatAssetMgr.LoadAssetAsync(paths, typeof(GameObject));

batch.OnLoaded += handles =>
{
    foreach (AssetHandle handle in handles)
    {
        if (handle.IsSuccess) Debug.Log($"已加载：{handle.Name}");
        else Debug.LogError($"加载失败：{handle.Name}");
    }

    // 示例仅查看结果；真实业务应在所有子资源使用结束后释放。
    batch.Dispose();
};
```

批量句柄支持 `await`；全部子句柄成功时 `batch.IsSuccess` 才为真。`OnLoaded` 参数为 `List<AssetHandle>`。释放批量句柄会释放其仍持有的所有子句柄。

混合类型可通过 `NatAssetMgr.GetBatchAssetHandle()` 创建批量句柄，并在开始等待或下一次 Update 前调用 `AddAssetHandle` 加入各类型加载句柄。若把某个子句柄转交给 GameObject 管理，应先 `RemoveAssetHandle`，避免批量释放时提前释放它。

### 场景加载与卸载

```csharp
SceneHandle scene = await NatAssetMgr.LoadSceneAsync(
    "Assets/Scenes/SampleScene.unity",
    UnityEngine.SceneManagement.LoadSceneMode.Additive);

if (scene.IsSuccess)
{
    scene.SetSceneActive();
    // 场景使用结束后再调用，而非必须在加载后立即卸载。
    NatAssetMgr.UnLoadSceneAsync(scene);
}
else
{
    scene.Dispose();
}
```

`Single` 加载由框架管理主场景切换，其句柄不能通过 `Dispose` / `UnLoadSceneAsync` 主动卸载；`Additive` 场景需要保留句柄并在使用结束后释放。`UnLoadSceneAsync` 返回 `void`，表示发起卸载，不是可等待的卸载完成任务。`SceneHandle.Process` 当前固定返回 `1`，不能用于真实进度条。

Bundle 场景需要被收集进构建资源。编辑器直接加载成功不能替代 Player 的场景加载验证。

### 释放与重新初始化

```csharp
// 在 async 方法中调用。先结束业务并销毁依赖这些资源的实例。
await NatAssetUtil.ReleaseAsync();

NatAssetUtil.Init(
    new StandaloneModeLoader(),
    () => Debug.Log("重新初始化完成"),
    error => Debug.LogError(error));
```

`Release()` 发起同一套异步释放流程，但不返回等待对象。需要重启资源系统时使用 `ReleaseAsync()`，等待进行中的加载、受管理的 Bundle 场景卸载、Bundle 释放和 `Resources.UnloadUnusedAssets()` 完成后再初始化；释放期间的新加载会被拒绝。

完全释放会使此前的资源对象和业务持有状态不再可靠，重新初始化后应重新获取资源，不要继续使用旧句柄或旧实例。

### 查询工具

| API | 用途 |
| --- | --- |
| `NatAssetUtil.GetAllBundleMD5()` | 获取清单中的 Bundle MD5 字典 |
| `NatAssetUtil.GetBundleMD5(bundleName)` | 查询指定 Bundle 的 MD5 |
| `NatAssetUtil.GetBundleHash(bundleName)` | 查询指定 Bundle 的 `Hash128`，参数不可省略 |
| `NatAssetUtil.CheckHasBundle(bundlePath)` | 检查当前清单是否登记了 Bundle；不等同于检查磁盘文件完整性 |
| `NatAssetUtil.GetBundleAllAssetPath(bundlePath)` | 获取 Bundle 登记的资源路径 |
| `NatAssetUtil.ImmediateUnLoadUnUseAsset()` | 跳过延迟，清理当前满足卸载条件的资源和 Bundle |

Bundle 查询适用于已加载清单的模式，参数使用清单中的逻辑 `BundlePath`，不是自行拼接的带 Hash 文件名。

源码：[NatAssetMgr](Assets/NatAsset/Runtime/NatAssetMgr.cs)、[NatAssetUtil](Assets/NatAsset/Runtime/NatAssetUtil.cs)、[句柄实现](Assets/NatAsset/Runtime/Core/Handle)。

## 引用、依赖与延迟卸载

释放句柄会归还框架引用；实际卸载还要检查加载保留、资源依赖、驻留依赖和 Bundle 状态。**引用数为零不代表该对象会立即被销毁。**

当前策略如下：

- 符合清单 `Independent` 策略的 TextAsset 和独立 Texture2D，在没有引用、进行中的加载或驻留依赖时可以单独卸载。
- Sprite、图集成员、Prefab、材质等通常按 `BundleOnly` 管理，随 Bundle 满足条件后卸载；不能对所有资源套用 `Resources.UnloadAsset`。
- Bundle 卸载检查关联资源和依赖 Bundle；相互依赖的 Bundle 会作为一组判断，最终使用 `AssetBundle.Unload(true)`。
- 编辑器直读模式只清除框架缓存，不主动销毁 AssetDatabase 对象。
- `ImmediateUnLoadUnUseAsset()` 会跳过延迟，但仍遵守引用和依赖约束，不会强行释放仍在使用的资源。

可在初始化前设置 `NatAssetSetting`：

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| `AssetDelayTime` | `60` | 符合独立卸载条件的资源延迟秒数 |
| `BundleDelayTime` | `120` | 符合卸载条件的 Bundle 延迟秒数 |
| `TaskFrameLimitLoaded` | `50` | 加载任务队列的每帧执行预算 |
| `TaskFrameLimitUnLoad` | `50` | 卸载任务队列的每帧执行预算 |
| `TaskFrameLimitNetLoad` | `50` | 网络任务队列的每帧执行预算；下载提供器也用它限制任务分配 |
| `UseCRC` | `true` | 原生 Bundle 文件加载的 CRC 校验开关 |
| `WebGLIsUseCache` | `true` | 兼容保留字段，当前不再控制 WebGL Bundle 加载行为 |

任务预算在任务系统初始化时读取。调度计数主要针对尚未结束的任务，不是每帧最多完成的资源数，也不是整个框架共享的 50 个任务上限。卸载倒计时使用 `Time.unscaledDeltaTime`，仍需要 Update 驱动。

源码：[生命周期与卸载判定](Assets/NatAsset/Runtime/DataInfo/RuntimeData.Lifetime.cs)、[资源清单元数据生成](Assets/NatAsset/Editor/UtilHelper/NatAssetEditorUtil.cs)。

## WebGL 与资源更新

### WebGL Bundle 加载

WebGL Player 通过 `UnityWebRequestAssetBundle.GetAssetBundle(path, manifestHash)` 异步加载未加密 Bundle，始终传入清单 Hash；不使用 `UseCRC`，也不受 `WebGLIsUseCache` 开关影响。旧的加密 WebGL 资源需要重新构建。

使用 `WebModeLoader` 前设置 `NatAssetMgr.UpdateURLAddress`，指向**直接包含清单的资源目录**，框架不会自动追加平台和版本目录。例如服务器布局：

```text
https://cdn.example.com/resources/WebGL/2026-10-14-12-00/
├── NatAssetConfig.data
└── ...清单 RelativePath 对应的 Bundle
```

远端路径首次读取后会被缓存，应在初始化前确定，不依赖运行中修改 URL 切换源。`WebModeLoader` 的远端 Bundle 访问与原生平台下载到 `SandBox` 的更新流程需要分别验证。

编辑器 `PackageOnly` 成功不代表浏览器路径已经通过测试：当前 Bundle 提供器按 `Application.platform == WebGLPlayer` 选择 WebGL 加载任务，编辑器仍走原生文件加载分支。浏览器部署、跨域访问和具体小游戏运行环境需要单独联调。

源码：[WebBundleTask](Assets/NatAsset/Runtime/Core/TaskSystem/LoadTask/WebBundleTask.cs)、[Bundle 任务选择](Assets/NatAsset/Runtime/Core/TaskSystem/Common/BaseProvider.cs)。

### 原生平台的更新入口

**原生平台热更新尚未全部完成，等待后续完善。**

目前已有的接口流程如下：

1. 设置 `NatAssetMgr.UpdateURLAddress`，通常用 `HostModeLoader` 初始化。
2. 调用 `NatAssetMgr.CheckAssetVersion(callback)`，比较远端、随包区和读写区清单。
3. 检查 `NatUpdaterInfo.Success` / `Error`，再查看 `TotalCount` 等差异信息。
4. 调用 `NatAssetMgr.UpdateAllGroup(info)` 发起更新，处理返回 `null` 的情况。
5. 在 `UpdateHandles.OnLoaded` 中逐个检查 `UpdateHandle.DownLoadSuccess`；所有业务处理结束后释放更新句柄。

还提供 `StopCheckAssetVersion()`、`GetTotalDownLoadRate()` 和 `GetGroupDownLoadRate(groupName)`；速率接口的单位是字节/秒。下载实现包含临时文件、Range 续传请求、重试和文件长度/MD5 校验。

源码：[版本检查](Assets/NatAsset/Runtime/Core/UpdaterSystem/Checker/VersionChecker.cs)、[更新系统](Assets/NatAsset/Runtime/Core/UpdaterSystem/UpdaterSystem.cs)、[汇总句柄](Assets/NatAsset/Runtime/Core/Handle/UpdateHandles.cs)、[单组句柄](Assets/NatAsset/Runtime/Core/Handle/UpdateHandle.cs)。

## 目录、扩展与排查

### 运行时目录

| 用途 | 路径 |
| --- | --- |
| Player 随包资源 | `Application.streamingAssetsPath/BuildInRes` |
| 编辑器 Bundle 测试资源 | `<输出根目录>/<当前构建平台>/Editor` |
| 编辑器读写区 / 下载临时区 | 上述测试目录下的 `SandBox` / `TempCache` |
| Standalone Player 读写区 / 下载临时区 | `Application.dataPath/SandBox` / `Application.dataPath/TempCache` |
| 其他 Player 读写区 / 下载临时区 | `Application.persistentDataPath/SandBox` / `Application.persistentDataPath/TempCache` |
| 远端资源 | `NatAssetMgr.UpdateURLAddress` 指定的目录 |

路径来自 [NatAssetPathTool](Assets/NatAsset/Runtime/Module/NatAssetPathTool.cs)。这些是代码的路径选择；部署时还应验证目标平台的实际文件访问权限和访问方式。

### 编辑器扩展

扩展脚本放在 `Editor` 目录或编辑器程序集内：

- 实现 `IBuildCollector.OnCollection(PackageVirtualRoot)` 定义分包规则，框架通过反射发现实现类。
- 实现 `OnBeforeBuildExe` 提供 `Priority` 和 `ExecutBeforBuild()`。按 Priority 升序执行，返回 `false` 或执行异常会中止构建。
- `OnExcludeFilePath.GenerateExcludeFilName()` 是旧的清单映射排除入口。目前按 Bundle 的 `CompletePath` 规范化后做精确匹配，不是递归目录通配，也不会阻止资源实际打入 Bundle。新版清单要求依赖完整，排除后仍被引用的资源会导致校验失败。

### 分析器与常见问题

`NatAsset > Profiler` 显示资源、场景、Bundle 的框架引用，以及加载和卸载任务。Handle 列表的注册方法当前没有接入句柄生命周期，不能依靠该列表是否为空来判断是否泄漏。

编辑器代码还可调用 `NATFrameWork.Profiler.ProfilerInfo.GetAssetLifetimeDiagnostics()`，查看外部引用、依赖引用、加载保留和卸载阻塞原因。这些是框架管理的引用信息，不是 Unity 原生对象引用计数。

| 现象 | 排查方向 |
| --- | --- |
| 找不到旧的 `NatFramework` 菜单 | 使用 `NatAsset > BuildBundleWindow` 等当前菜单；配置资产的创建菜单仍是 `NATFramework` |
| 初始化或异步加载一直不完成 | 检查是否有唯一且持续运行的 `NatAssetUtil.Update()` 驱动，以及 Console 中的错误 |
| 提示等待初始化，或框架正在退出 | 在初始化成功后加载；重启前等待 `ReleaseAsync()` |
| 编辑器 Bundle 测试清单不存在 | 构建或选择同平台资源，并“应用到编辑器测试”；不是只把资源复制到 StreamingAssets |
| 平台、清单格式或 MD5 不匹配 | 用当前代码重新构建对应平台，整套部署清单和 Bundle，不混用版本 |
| 提示资源依赖未收集 | 检查收集规则和排除规则，在 `PreBundlesView` 核对资源及共享依赖 |
| Dispose 后内存没有立刻下降 | 检查延迟、依赖引用、正在加载的任务、图集成员和 BundleOnly 策略 |
| 切换测试版本后资源与预期不符 | 核对生效目录，检查 Host 模式下的覆盖清单，以及编辑器与 Player 的路径差异 |

## 源码导航与验证建议

| 目录 / 文件 | 职责 |
| --- | --- |
| [Runtime/NatAssetMgr.cs](Assets/NatAsset/Runtime/NatAssetMgr.cs) | 面向业务的资源、场景和更新接口 |
| [Runtime/NatAssetUtil.cs](Assets/NatAsset/Runtime/NatAssetUtil.cs) | 初始化、驱动、释放和清单查询 |
| [Runtime/Core](Assets/NatAsset/Runtime/Core) | Loader、Handle、Provider、任务调度、场景和更新系统 |
| [Runtime/DataInfo](Assets/NatAsset/Runtime/DataInfo) | 清单、缓存、资源引用及生命周期 |
| [Editor/Core/Controller](Assets/NatAsset/Editor/Core/Controller) | 收集器、版本和 SBP 构建流程 |
| [Editor/UtilHelper](Assets/NatAsset/Editor/UtilHelper) | 构建服务、资源校验、测试版本同步 |
| [Editor/Window](Assets/NatAsset/Editor/Window) | 构建窗口、Profiler、Shader 变体工具 |

验证接入时，按以下顺序逐步扩大范围：

1. Editor 模式：初始化、Prefab 加载成功/失败、对象销毁后释放。
2. 本机平台 Bundle 模式：构建、校验、应用版本、加载资源和附加场景。
3. 生命周期：重复加载共享依赖、批量释放、延迟卸载、立即清理、释放期间重启。
4. 版本切换：自动同步、手动指定旧版本、缺失 Bundle、损坏清单和同步中断恢复。
5. 目标 Player：随包目录访问、场景切换和退出；WebGL 在实际浏览器中验证网络和 Hash 版本行为。
6. 原生平台热更新：尚未全部完成，等待后续完善后再进行验证。

后续修改 API、默认参数、菜单或清单格式时，应同步更新本文的对应示例和限制说明，并记录实际执行的平台验证结果。
