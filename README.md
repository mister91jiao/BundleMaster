# BundleMaster
<br/> Unity资源加载大师(纯代码ET框架集成用)</br>

<br/>网站地址: https://www.unitybundlemaster.com</br>
<br/>视频教程</br>
<br/>YouTube : https://www.youtube.com/watch?v=3P7yJu01j0I</br>
<br/>B站 : https://www.bilibili.com/video/BV19341177Ek</br>
<br/>QQ讨论群: 787652036</br>
<br/>非ET框架集成版本: https://github.com/mister91jiao/BundleMaster_IntegrateETTask</br>

<br/>注意事项: </br>
<br/>WebGL 平台需要加上 BMWebGL 宏，部署使用 Local 模式运行(网页直接刷新就行所以不需要更新)，注意避免正在异步加载一个资源的时候有同步加载同一个资源。</br>
<br/>Switch 平台需要加上 Nintendo_Switch 宏，理论上可以进行热更但因为政策原因所以没有对更新功能进行适配，因此部署依然需要在 Local 模式 下运行，除此之外还要加上 NintendoSDKPlugin，不然会报错，政策原因不上传这部分内容，有需要switch开发需要可以找我联系</br>

<br/>代码示例:</br>

      //初始化流程
      public async ETTask Init()
      {
         //定义要检查的资源包名
         Dictionary<string, bool> updatePackageBundle = new Dictionary<string, bool>()
         {
            {"MainAssets", false},
         };
         //检查是否需要更新
         UpdateBundleDataInfo _updateBundleDataInfo = await AssetComponent.CheckAllBundlePackageUpdate(updatePackageBundle);
         if (_updateBundleDataInfo.NeedUpdate)
         {
            //增量更新更新
            await AssetComponent.DownLoadUpdate(_updateBundleDataInfo);
         }
         //初始化资源包
         await AssetComponent.Initialize("MainAssets");
      }

      //加载资源代码示例
      public async ETTask Example()
      {
         //同步加载资源
         GameObject playerAsset1 = AssetComponent.Load<GameObject>(out LoadHandler handler, "Assets/Bundles/Role/Player1.prefab");
         GameObject player1 = UnityEngine.Object.Instantiate(playerAsset1);

         //异步加载资源
         GameObject playerAsset2 = await AssetComponent.LoadAsync<GameObject>("Assets/Bundles/Role/Player2.prefab");
         GameObject player2 = UnityEngine.Object.Instantiate(playerAsset2);
            
         //卸载资源
         handler.UnLoad();
         AssetComponent.UnLoadByPath("Assets/Bundles/Role/Player2.prefab");
      }
    
      //别忘了配置生命周期
      void Update()
      {
         AssetComponent.Update();
      }
      void OnLowMemory()
      {
         //移动平台低内存时候调用，可选
         //AssetComponent.ForceUnLoadAll();
      }
      void OnDestroy()
      {
         //如果当前正在更新资源，取消更新
         _updateBundleDataInfo?.CancelUpdate();
         //游戏销毁调用
         AssetComponent.Destroy();
      }

<br/>友情链接: </br>
<br/>JEngine 一款不错的客户端框架: https://github.com/JasonXuDeveloper/JEngine</br>
<br/>HybridCLR 革命性的热更新解决方案: https://github.com/focus-creative-games/hybridclr</br>
