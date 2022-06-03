using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Liluo.BiliBiliLive.ImageLoad
{
    public static class BiliBiliLiveHeadImage
    {
        /// <summary>
        /// 用户头像地址缓存
        /// </summary>
        static Dictionary<int, string> imgdic = new Dictionary<int, string>();

        static async Task<string> GetPath(int userID)
        {
            if (!imgdic.ContainsKey(userID))
            {

                string Url = "https://api.bilibili.com/x/space/acc/info?mid=" + userID.ToString() + "&jsonp=jsonp";
                UnityWebRequest req = UnityWebRequest.Get(Url);  //创建request
                req.timeout = 30 * 1000;
                req.SetRequestHeader("user-agent", @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1092.0 Safari/536.6");
                req.SetRequestHeader("referer", @"https://space.bilibili.com/21792043");
                var webRequest = req.SendWebRequest();
                await ExtendTask.WaitUntil(() => webRequest.isDone);
#if UNITY_2020_1_OR_NEWER
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogError($"获取失败！");
                    return null;
                }
                else
                {
                    string jsonData = req.downloadHandler.text;
                    var data = JObject.Parse(jsonData);
                    jsonData = data["data"]["face"].ToString();
                    // 防止连发两条的情况
                    if (!imgdic.ContainsKey(userID))
                        imgdic.Add(userID, jsonData);
                    return jsonData;
                }
            }
            else
            {
                return imgdic[userID];
            }
        }

        public static async Task<Texture2D> GetHeadTexture(int userID)
        {
            string url = await GetPath(userID);
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            var data = request.SendWebRequest();
            await ExtendTask.WaitUntil(() => data.isDone);
#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"获取失败！");
                return null;
            }
            else
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                return tex;
            }
        }

        public static async Task<Sprite> GetHeadSprite(int userID)
        {
            Texture2D tex = await GetHeadTexture(userID);
            if (tex is null) return null;
            Sprite temp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
            return temp;
        }
    }
}


public static class ExtendTask
{
    public delegate bool ActionBool();
    public static async Task WaitUntil(ActionBool callback)
    {
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(0.05f));
        } while (!callback());
    }
}