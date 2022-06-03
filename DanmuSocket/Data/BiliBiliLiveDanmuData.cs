namespace Liluo.BiliBiliLive
{
    public struct BiliBiliLiveDanmuData
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string username;

        /// <summary>
        /// 弹幕内容
        /// </summary>
        public string content;

        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId;

        /// <summary>
        /// VIP用户（老爷）
        /// </summary>
        public bool vip;

        /// <summary>
        /// 舰队等级（舰长:3 提督:2 总督:1）
        /// </summary>
        public int guardLevel;
    }
}
