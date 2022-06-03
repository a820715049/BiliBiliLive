using System;
using System.Threading.Tasks;

namespace Liluo.BiliBiliLive
{
    public abstract class IBiliBiliLiveRequest
    {
        /// <summary>
        /// 申请异步连接
        /// </summary>
        /// <param name="roomId">房间号</param>
        /// <returns>连接结果</returns>
        public abstract Task<bool> Connect(int channelId);

        /// <summary>
        /// 断开连接
        /// </summary>
        public abstract void DisConnect();

        /// <summary>
        /// 房间人数改变时触发回调
        /// </summary>
        public Action<uint> OnRoomViewer;

        /// <summary>
        /// 监听弹幕回调函数
        /// </summary>
        public Action<BiliBiliLiveDanmuData> OnDanmuCallBack;

        /// <summary>
        /// 监听礼物回调函数
        /// </summary>
        public Action<BiliBiliLiveGiftData> OnGiftCallBack;

        /// <summary>
        /// 监听上舰回调函数
        /// </summary>
        public Action<BiliBiliLiveGuardData> OnGuardCallBack;

        /// <summary>
        /// 监听SC回调函数
        /// </summary>
        public Action<BiliBiliLiveSuperChatData> OnSuperChatCallBack;
    }
}