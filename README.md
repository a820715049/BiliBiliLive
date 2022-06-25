### 极简式 Unity 获取 bilibili 直播弹幕、SC、上舰、礼物等
#### 1. 声明
[下载链接](https://github.com/a820715049/BiliBiliLive)
软件均仅用于学习交流，请勿用于任何商业用途！
#### 2. 介绍
该项目为Unity实时爬取B站直播弹幕。
 - 项目介绍：通过传入B站直播间账号，实现监控B站直播弹幕、SC、上舰、礼物等。
 - 运行方式：下载后将文件夹 文件夹 BiliBiliLive 拖进 Unity 的 Asset 文件夹内即可完成安装。
#### 3. 运行需求
1. **Unity2019** 或更高
2. **c# 5.0**以上
3. 运行需要 Json 插件解析Json，若您的项目已经导入则无视即可。若未导入可将文件夹内的Json文件导入。
#### 4. 使用方式
0. 您需要在主脚本中引入命名空间 `using Liluo.BiliBiliLive;`
1. 在任意Mono脚本中编写以下脚本，以建立一个连接到BiliBili直播间。RoomID 为房间号。
注意，本插件大量使用了异步编程，对于这样的方法，您需要使用**async**修饰类，并使用**await**等待函数完成。
```csharp
IBiliBiliLiveRequest req;
async void Init(int RoomID)
{
    // 创建一个直播间监听对象
    req = await BiliBiliLive.Connect(RoomID);
}
```
3. 如需释放监听，可使用 DisConnect 方法释放。

```csharp
void OnDestroy()
{
    // 释放监听对象
    req.DisConnect();
    req = null;
}
```
4. 定时监听房间人数
该函数每隔一段时间调用，其入参为当前房间人数（热度）。
```csharp
req.OnRoomViewer += number =>
{
    Debug.Log($"当前房间人数为: {number}");
};
```
5. 监听指定内容
以下是个函数为主要监听使用函数，其入参分别为对应监听事件的相关信息结构体。VS中按下 **Ctrl+左键** 即可了解以下结构体提供的具体信息。
```csharp
/// 监听弹幕回调函数
event Action<BiliBiliLiveDanmuData> OnDanmuCallBack;

/// 监听礼物回调函数
event Action<BiliBiliLiveGiftData> OnGiftCallBack;

/// 监听上舰回调函数
event Action<BiliBiliLiveGuardData> OnGuardCallBack;

/// 监听SC回调函数
event Action<BiliBiliLiveSuperChatData> OnSuperChatCallBack;
```
6. 获取用户头像
也许你会需要获取对应用户的头像，本插件提供两种方法供选择
```csharp
// 获得Texture图像
BiliBiliLive.GetHeadTexture(userId);
// 获得精灵图像
BiliBiliLive.GetHeadSprite(userId);
```
#### 4. 示例启动脚本
```csharp
using UnityEngine;
using UnityEngine.UI;
using Liluo.BiliBiliLive;

public class Online : MonoBehaviour
{
    public Image img;
    public int RoomID;
    IBiliBiliLiveRequest req;

    async void Start()
    {
        // 创建一个监听对象
        req = await BiliBiliLive.Connect(RoomID);
        req.OnDanmuCallBack += GetDanmu;
        req.OnGiftCallBack += GetGift;
        req.OnSuperChatCallBack += GetSuperChat;
        bool flag = true;
        req.OnRoomViewer += number =>
        {
        	// 仅首次显示
            if (flag) Debug.Log($"当前房间人数为: {number}");
        };
    }

    /// <summary>
    /// 接收到礼物的回调
    /// </summary>
    public async void GetGift(BiliBiliLiveGiftData data)
    {
        Debug.Log($"<color=#FEA356>礼物</color> 用户名: {data.username}, 礼物名: {data.giftName}, 数量: {data.num}, 总价: {data.total_coin}");
        img.sprite = await BiliBiliLive.GetHeadSprite(data.userId);
    }

    /// <summary>
    /// 接收到弹幕的回调
    /// </summary>
    public async void GetDanmu(BiliBiliLiveDanmuData data)
    {
        Debug.Log($"<color=#60B8E0>弹幕</color> 用户名: {data.username}, 内容: {data.content}, 舰队等级: {data.guardLevel}");
        img.sprite = await BiliBiliLive.GetHeadSprite(data.userId);
    }

    /// <summary>
    /// 接收到SC的回调
    /// </summary>
    public async void GetSuperChat(BiliBiliLiveSuperChatData data)
    {
        Debug.Log($"<color=#FFD766>SC</color> 用户名: {data.username}, 内容: {data.content}, 金额: {data.price}");
        img.sprite = await BiliBiliLive.GetHeadSprite(data.userId);
    }

    private void OnApplicationQuit()
    {
        req.DisConnect();
    }
}
```
#### 4. 运行截图
![在这里插入图片描述](https://img-blog.csdnimg.cn/1da30020560045dc87d3a9e543c18488.png)
