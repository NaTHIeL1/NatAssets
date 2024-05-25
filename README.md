# 介绍
+ NatAsset是一个unity资源管理框架，目前处于开发状态，框架采用引用计数管理资源卸载，支持同步异步加载资源，仅支持SBP构建，目前build-in模式存在问题后续计划移除build-in模块，该框架为个人学习框架，可作为学习参考。
# 注意事项
+ 目前在框架中的资源延迟卸载时间为60s，Bundle延迟卸载时间为60s，每帧任务最大运行数量为50个。目前无法修改。
+ 支持PC（使用streamingAssets目录）、安卓（默认streamingAssets目录）
+ IOS未测试；WebGL未适配。
# 编辑器使用
## 介绍
+ 资源框架建立了对应的资源编辑器
### ScriptObject
+ 首先在Asset目录下右键Create->NATFramework，新建NatAssetBuildSetting和NatAssetSObj，或者直接打开窗口等，该scriptObject也会自行创建。
+ 点击NatAssetSObj，RunWay为运行模式的选择，Editor模式不需要打包AssetBundle即可运行，切换至PackageOnly模式就需要对资源进行打包。
+ NatAssetBuildSetting为当前资源框架的数据缓存文件，以上ScriptObject都只在编辑器下被使用。

### 资源打包编辑器
菜单栏：NatFramework->NatAsset->BundleBuildWindow
+ 右侧点击AddFolderPackage，选择对应的资源文件夹；右侧点击AddFilePackage，选择对应的资源文件
+ 再左侧面板点击选中目标package， Collector为资源分包策略，Encrypt为资源加密策略，Group为资源所属组类别（Group目前未开发完成）
+ PreBundlesView页面点击Refresh可以预览当前资源分包策略的分包结果
+ Build窗口，ChooseBatchBuild处勾选需要打包资源的目标平台，默认输出路径为与Assets同级目录AssetBundles，资源对打入对应平台的文件夹中，Version为版本号，Compression选择打包压缩格式，建议选LZ4，GlobalEncrypt为全局资源加密方式，勾选AppendHash可使打出的文件拥有hash后缀。
+ 点击BundleBuild打包资源
+ ClearModule下，提供了三种清理缓存的方式
+ Version面板， Refresh按钮刷新界面，PlatformTarget选中目标平台，框中输入需要的group名称，电一拷贝按钮将目标资源拷贝到StreamingAsset目录下。
### 运行时分析器
菜单栏：NatFramework->NatAsset->Profiler
+ 最左侧的Handle列表（暂未完成）
+ 中间的RefInfo记录了每一个Asset,Scene,Bundle的引用计数，可以用于辅助找到有哪些资源忘记卸载。
+ 右侧上半部分为正在执行的加载任务，并标注任务的执行状态。
+ 右侧下半部分为正在执行的卸载任务，并同事标记任务的卸载进度。
# 如何切换Build-In和SBP
+ 在unity的packageManager中，只要选择下载了ScriptBuildPipleLine这个包，就会切换至SBP模式。否则使用Build-In模式打包。
# API使用
## NatAssetUtil
+ 首先使用NatAssetUtil作为流程控制
### 初始化(NatAssetUtil.Init)
+ StandaloneModeLoader 单机模式
+ HostModeLoader 网络热更模式（热更暂未完成）
+ WebModeLoader WebGL使用的模式（未完成）
``` c#
public void Init()
{
	StandaloneModeLoader standaloneModeLoader = new StandaloneModeLoader();
	NatAssetUtil.Init(standaloneModeLoader, Callbakc)
}

public void Callbakc()
{
	//此处代表资源框架初始化完毕，可以正式加载资源，作为正式启动游戏流程的开关
}
```
### 更新(NatAssetUtil.Update)
+ 在自己游戏流程的update中每帧调用资源框架的update函数驱动运行
``` c#
public void Update()
{
	NatAssetUtil.Update();
}
```
### 卸载(NatAssetUtil.Release)
+ 调用卸载流程会将所有资源都进行强制卸载，不论资源是否正在使用。在使用Release后需要重新Init资源框架。
``` c#
public void Release()
{
	NatAssetUtil.Release();
}
```
### 立即释放未使用的资源(NatAssetUtil.ImmediateUnLoadUnUseAsset)
+ 该接口直接释放所有引用计数为0的资源。
``` c#
NatAssetUtil.ImmediateUnLoadUnUseAsset()
```
### 获取所有Bundle的Md5值
``` c#
NatAssetUtil.GetAllBundleMD5()
```
### 获取指定Bundle的Hash值
``` c#
NatAssetUtil.GetBundleHash()
```
### 获取指定Bundle的MD5值
``` c#
string bundleName = "test"
NatAssetUtil.GetBundleMD5(bundleName)
```

## NatAssetMgr
### 资源加载与卸载
>资源加载采用句柄机制，异步句柄可使用异步await等待

#### 加载
+ 同步加载资源
``` c#
{
	AssetHandle assetHandle = 
		NatAssetMgr.LoadAsset("Assets/Res/Shader/ShaderVariantsCollection.shadervariants", 
		typeof(ShaderVariantCollection));
    ShaderVariantCollection shaderVariantCollection = 
	    (ShaderVariantCollection)assetHandle.Asset;
}

{
	AssetHandle textureHandle = NatAssetMgr.LoadAsset<Texture>("Assets/Res/Texture/texture.jpg");
	rawImage.texture = (assetHandle.Asset as Texture);
}
```
+ 异步加载资源
``` c#
public void LoadSpriteAsync(string path)
{
	AssetHandle assetHandle = NatAssetMgr.LoadAssetAsync<Sprite>(path);
    assetHandle.OnLoaded += BindSprite;
}

public async void LoadSpriteAsyncAw(string path)
{
	AssetHandle assetHandle = await NatAssetMgr.LoadAssetAsync<Sprite>(path);
    BindSprite(assetHandle);
}

public void BindSprite(AssetHandle assetHandle)
{
	assetHandle.OnBind(gameObject);
    image.sprite = assetHandle.Asset as Sprite;
}
```
+ 异步以及同步实例化资源接口(GameObject)，该接口会自定绑定物体的生命周期，当物体销毁时自行进行句柄卸载
``` c#
NatAssetMgr.InstanceObj(string targetPath)
NatAssetMgr.InstanceObjAsync(string targetPath，Action<GameObject> callback)
```

#### 卸载
+ 卸载资源
``` c#
{
	AssetHandle assetHandle = //引用某个已经存在的句柄
	NatAssetMgr.UnLoadAsset(assetHandle);
	//释放完毕后置空句柄，句柄资源会被回收复用
	assetHandle = null;
}
```
### 场景加载卸载
>场景加载可以无需管理场景句柄
#### 加载
+ 异步加载场景，不支持同步场景加载
``` c#
{
	string path = InputFieldScenePath.GetInputText();
    NatAssetMgr.LoadSceneAsync(path, LoadSceneMode.Single, Priority.Low);
}
```
#### 卸载
+ 异步卸载场景，只支持场景异步卸载，只有一个场景的情况下无法卸载当前场景
``` c#
{
	SceneHandle sceneHandle = ;//指向某个引用的场景句柄
    NatAssetMgr.UnLoadSceneAsync(sceneHandle);
}
```

### 句柄
#### 资源句柄
+ 异步回调，异步回调有两种形式，一种是使用await，一种是对应句柄调用OnLoaded委托。
``` c#
public void LoadSpriteAsyncAw(string path)
{
	AssetHandle assetHandle = await NatAssetMgr.LoadAssetAsync<Sprite>(path);
    BindSprite(assetHandle);
}

public void LoadSpriteAsync(string path)
{
	AssetHandle assetHandle = NatAssetMgr.LoadAssetAsync<Sprite>(path);
    assetHandle.OnLoaded += BindSprite;
}
```
+ 资源卸载，当资源加载返回句柄后，可以调用对应的资源卸载接口，这回直接卸载当前句柄所对应的资源，但已经实例化在场景中的资源需要自行卸载。
``` c#
public void LoadSpriteAsync(string path)
{
	AssetHandle assetHandle = NatAssetMgr.LoadAssetAsync<Sprite>(path);
	//卸载
	assetHandle.UnLoad()
}
```
+ 资源绑定，当资源被加载出来，通常手动管理较为麻烦，就采用绑定到对应GameObject的方法同步生命周期，随游戏物体被销毁一同卸载。
``` c#
public void LoadSpriteAsync(string path,GameObject obj)
{
	AssetHandle assetHandle = NatAssetMgr.LoadAssetAsync<Sprite>(path);
	//绑定
	assetHandle.OnBind(obj);
}
```
#### 场景句柄
> 场景句柄需关注await和OnLoaded即可，要卸载场景时，使用卸载接口传入对应句柄，或直接调用UnLoad函数。

+ 注意：场景句柄不存在回收机制，不论主动被动调用场景卸载都会将句柄置为无效句柄并抛弃。
+ 异步回调
``` c#
{
    string path = "Assets/Res/Scecns/TestScene.unity";
    SceneHandle sceneHandle = NatAssetMgr.LoadSceneAsync(path, LoadSceneMode.Single);
    sceneHandle.OnLoaded += (handle) =>
    {
        Debug.Log("场景加载完毕");
    };
}


{
    string path = "Assets/Res/Scecns/TestScene.unity";
    SceneHandle sceneHandle = await NatAssetMgr.LoadSceneAsync(path, LoadSceneMode.Single);
    Debug.Log("场景加载完毕");
}
```
## 反射接口
在Assets目录下新建Editor目录
+ OnExcludeFilePath
> 在Editor目录下新建脚本继承OnExcludeFilePath，该接口用于在配置文件中排除指定文件夹，使其中的资源不出现在配置文件的映射表中。指定文件夹的路径是相对于上述提到的资源文件根目录。
``` c#
public List<string> GenerateExcludeFilName()
{
    //排除指定文件夹，使其中的资源不出现在配置文件的映射表中
    return new List<string>()
    {
        "Assets/Res/Code/AotCode",
        "Assets/Res/Code/HotCode",
    };
}
```
# 后续计划
+ 将一些反射接口进行整合放进ScriptObject中，无需代码继承。
+ 修改SBP打包机制，完全自定义而不采用官方的Build接口。
+ 移除Build-In打包代码，只维护一套SBP。
+ 加入资源组概念，后续调整配置文件，以实现如按关卡下载资源等目标。
+ 完成Handle面板的代码。
+ 完成资源更新方面的代码。
