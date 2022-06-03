using UnityEngine;
using System.Threading.Tasks;
using Liluo.BiliBiliLive.ImageLoad;

namespace Liluo.BiliBiliLive
{
    public static class BiliBiliLive
    {
        /// <summary>
        /// 创建一个直播间连接
        /// </summary>
        /// <param name="roomID">房间号</param>
        /// <returns>回调结果</returns>
        public static async Task<IBiliBiliLiveRequest> Connect(int roomID)
        {
            var liveRequest = new BiliBiliLiveRequest();
            bool request = await liveRequest.Connect(roomID);
            if (request)
            {
                return liveRequest;
            }
            return null;
        }

        /// <summary>
        /// 获得用户头像地址
        /// </summary>
        /// <param name="userID">用户ID</param>
        /// <returns>头像链接</returns>
        public static async Task<Texture2D> GetHeadTexture(int userID) => await BiliBiliLiveHeadImage.GetHeadTexture(userID);

        /// <summary>
        /// 获得用户头像地址
        /// </summary>
        /// <param name="userID">用户ID</param>
        /// <returns>头像链接</returns>
        public static async Task<Sprite> GetHeadSprite(int userID) => await BiliBiliLiveHeadImage.GetHeadSprite(userID);

    }
}