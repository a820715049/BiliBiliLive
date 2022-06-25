using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BitConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Liluo.BiliBiliLive
{
    public class BiliBiliLiveRequest : IBiliBiliLiveRequest
    {
        /// <summary>
        /// 弹幕聊天的服务器地址
        /// </summary>
        string chatPath = "chat.bilibili.com";
        /// <summary>
        /// Tcp 客户端 socket
        /// </summary>
        TcpClient client;
        /// <summary>
        /// 流
        /// </summary>
        Stream netStream;
        /// <summary>
        /// 判断是否连接
        /// </summary>
        bool connected = false;

        /// <summary>
        /// 默认主机的这两个服务器
        /// </summary>
        string[] defaultPaths = new string[] { "livecmt-2.bilibili.com", "livecmt-1.bilibili.com" };
        /// <summary>
        /// Http 对象
        /// </summary>
        HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
        /// <summary>
        /// 获取 房间ID 的地址
        /// </summary>
        string CIDInfoUrl = "https://api.live.bilibili.com/room/v1/Danmu/getConf?room_id=";
        /// <summary>
        /// 协议转换
        /// </summary>
        short protocolversion = 2;
        /// <summary>
        /// 端口号
        /// </summary>
        int chatPort = 2243;

        public event Action<int> OnRoomViewer;
        public event Action<BiliBiliLiveDanmuData> OnDanmuCallBack;
        public event Action<BiliBiliLiveGiftData> OnGiftCallBack;
        public event Action<BiliBiliLiveGuardData> OnGuardCallBack;
        public event Action<BiliBiliLiveSuperChatData> OnSuperChatCallBack;

        /// <summary>
        /// 申请异步连接  需要输入对应房间号
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public async Task<bool> Connect(int roomID)
        {
            if (connected)
            {
                UnityEngine.Debug.LogError("连接已存在");
                return true;
            }
            //token 令牌
            var token = "";
            try
            {
                //发起Http请求
                var req = await httpClient.GetStringAsync(CIDInfoUrl + roomID);
                JObject roomobj = JObject.Parse(req);
                token = roomobj["data"]["token"].ToString();
                chatPath = roomobj["data"]["host"].ToString();
                chatPort = roomobj["data"]["port"].Value<int>();
                if (string.IsNullOrEmpty(chatPath)) throw new Exception();
            }
            catch (Exception e)
            {
                chatPath = defaultPaths[UnityEngine.Random.Range(0, defaultPaths.Length)];
                UnityEngine.Debug.LogError($"获取弹幕服务器地址时出现错误，尝试使用默认服务器... 错误信息: {e}");
            }
            // 创建 TCP对象
            client = new TcpClient();
            // DNS解析域名 服务器IP地址
            var ipAddress = await System.Net.Dns.GetHostAddressesAsync(chatPath);
            // 随机选择一个进行连接
            await client.ConnectAsync(ipAddress[UnityEngine.Random.Range(0, ipAddress.Length)], chatPort);
            netStream = Stream.Synchronized(client.GetStream());

            UnityEngine.Debug.Log("发送验证消息");
            if (await SendJoinChannel(roomID, token))
            {
                UnityEngine.Debug.Log("成功");
                connected = true;
                // 发送心跳包
                _ = HeartbeatLoop();
                // 接收消息
                _ = ReceiveMessageLoop();
                return true;
            }
            UnityEngine.Debug.Log("失败");
            return false;
        }
        
        public void DisConnect() => _disconnect();

        async Task ReceiveMessageLoop()
        {
            var stableBuffer = new byte[16];
            var buffer = new byte[4096];
            while (this.connected)
            {
                try
                {
                    await netStream.ReadAsync(stableBuffer, 0, 16);
                    var protocol = DanmakuProtocol.FromBuffer(stableBuffer);
                    if (protocol.PacketLength < 16)
                    {
                        UnityEngine.Debug.LogError("协议失败: (L:" + protocol.PacketLength + ")");
                        continue;
                    }
                    var payloadlength = protocol.PacketLength - 16;
                    if (payloadlength == 0) continue;
                    buffer = new byte[payloadlength];
                    //继续接受 协议总长度-协议头部 长度 的字节数据
                    await netStream.ReadAsync(buffer, 0, payloadlength);
                    if (protocol.Version == 2 && protocol.Action == 5)
                    {
                        using (var ms = new MemoryStream(buffer, 2, payloadlength - 2))
                        using (var deflate = new DeflateStream(ms, CompressionMode.Decompress))
                        {
                            var headerbuffer = new byte[16];
                            try
                            {
                                while (true)
                                {
                                    await deflate.ReadAsync(headerbuffer, 0, 16);
                                    var protocol_in = DanmakuProtocol.FromBuffer(headerbuffer);
                                    payloadlength = protocol_in.PacketLength - 16;
                                    if (payloadlength <= 0) break;
                                    var danmakubuffer = new byte[payloadlength];
                                    await deflate.ReadAsync(danmakubuffer, 0, payloadlength);
                                    int num = 0;
                                    for (int i = 0; i < danmakubuffer.Length; i++)
                                    {
                                        if (danmakubuffer[i] == 0)
                                            num++;
                                    }
                                    if (num == danmakubuffer.Length) break;
                                    ProcessDanmaku(protocol.Action, danmakubuffer);
                                }
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogError($"读取弹幕消息失败。错误信息: {e}");
                            }
                        }
                    }
                    else
                    {
                        ProcessDanmaku(protocol.Action, buffer);
                    }
                }
                catch (Exception e)
                {
                    if (e is System.ObjectDisposedException)
                    {
                        UnityEngine.Debug.LogWarning("连接已释放");
                        break;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"接受消息时发生错误。错误信息: {e}");
                    }
                }
            }
        }

        void ProcessDanmaku(int action, byte[] buffer)
        {
            switch (action)
            {
                case 3: 
                    {
                        // 观众人数
                        var viewer = EndianBitConverter.BigEndian.ToInt32(buffer, 0); 
                        OnRoomViewer?.Invoke(viewer);
                        break;
                    }
                case 5:
                    {
                        // 弹幕
                        var json = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                        // UnityEngine.Debug.Log(obj["cmd"].ToString());

                        //using (System.IO.StreamWriter file = new System.IO.StreamWriter("log.txt", true))
                        //{
                        //    file.Write(json);//直接追加文件末尾，不换行
                        //    file.WriteLine();//直接追加文件末尾，换行 
                        //}

                        var obj = JObject.Parse(json);
                        switch (obj["cmd"].ToString())
                        {
                            case "DANMU_MSG":
                                {
                                    BiliBiliLiveDanmuData danmuData = new BiliBiliLiveDanmuData();
                                    danmuData.username = obj["info"][2][1].ToString();
                                    danmuData.content = obj["info"][1].ToString();
                                    danmuData.userId = obj["info"][2][0].Value<int>();
                                    danmuData.vip = obj["info"][2][3].ToString() == "1";
                                    danmuData.guardLevel = obj["info"][7].ToObject<int>();
                                    OnDanmuCallBack?.Invoke(danmuData);
                                    break;
                                }
                            // 礼物
                            case "SEND_GIFT":
                                {

                                    BiliBiliLiveGiftData giftData = new BiliBiliLiveGiftData();
                                    giftData.username = obj["data"]["uname"].ToString();
                                    giftData.userId = obj["data"]["uid"].Value<int>();
                                    giftData.giftName = obj["data"]["giftName"].ToString();
                                    giftData.giftId = obj["data"]["giftId"].Value<int>();
                                    giftData.num = obj["data"]["num"].Value<int>();
                                    giftData.price = obj["data"]["price"].Value<int>();
                                    giftData.total_coin = obj["data"]["total_coin"].Value<int>();
                                    OnGiftCallBack?.Invoke(giftData);
                                    break;
                                }
                            // 上舰
                            case "GUARD_BUY":
                                {
                                    BiliBiliLiveGuardData guardData = new BiliBiliLiveGuardData();
                                    guardData.username = obj["data"]["username"].ToString();
                                    guardData.userId = obj["data"]["uid"].ToObject<int>();
                                    guardData.guardLevel = obj["data"]["guard_level"].ToObject<int>();
                                    guardData.guardName = guardData.guardLevel == 3 ? "舰长" :
                                        guardData.guardLevel == 2 ? "提督" :
                                        guardData.guardLevel == 1 ? "总督" : "";
                                    guardData.guardCount = obj["data"]["num"].ToObject<int>();
                                    OnGuardCallBack?.Invoke(guardData);
                                    break;
                                }
                            // SC
                            case "SUPER_CHAT_MESSAGE":
                                {
                                    BiliBiliLiveSuperChatData superChatData = new BiliBiliLiveSuperChatData();
                                    superChatData.username = obj["data"]["user_info"]["uname"].ToString();
                                    superChatData.userId = obj["data"]["uid"].ToObject<int>();
                                    superChatData.content = obj["data"]["message"]?.ToString();
                                    superChatData.price = obj["data"]["price"].ToObject<decimal>();
                                    superChatData.keepTime = obj["data"]["time"].ToObject<int>();
                                    OnSuperChatCallBack?.Invoke(superChatData);
                                    break;
                                }
                            default:
                                if (obj["cmd"].ToString().StartsWith("DANMU_MSG"))
                                {
                                    BiliBiliLiveDanmuData danmuData = new BiliBiliLiveDanmuData();
                                    danmuData.username = obj["info"][2][1].ToString();
                                    danmuData.content = obj["info"][1].ToString();
                                    danmuData.userId = obj["info"][2][0].Value<int>();
                                    danmuData.vip = obj["info"][2][3].ToString() == "1";
                                    danmuData.guardLevel = obj["info"][7].ToObject<int>();
                                    OnDanmuCallBack?.Invoke(danmuData);
                                    break;
                                }
                                break;
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        async Task HeartbeatLoop()
        {
            try
            {
                while (this.connected)
                {
                    //每30秒发送一次 心跳
                    await SendHeartbeatAsync();
                    await Task.Delay(30000);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"与服务器连接时发生错误。错误信息: {e}");
                _disconnect();
            }

            // 发送ping包
            async Task SendHeartbeatAsync() => await SendSocketDataAsync(2);
        }

        void _disconnect()
        {
            connected = false;
            try
            {
                client.Close();
                netStream.Close();
                UnityEngine.Debug.Log("断开连接");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"断开连接时发生错误。错误信息: {e}");
            }
            netStream = null;
        }

        Task SendSocketDataAsync(int action, string body = "")
        {
            return SendSocketDataAsync(0, 16, protocolversion, action, 1, body);
        }
        async Task SendSocketDataAsync(int packetlength, short magic, short ver, int action, int param = 1, string body = "")
        {
            var playload = Encoding.UTF8.GetBytes(body);
            if (packetlength == 0) packetlength = playload.Length + 16;
            var buffer = new byte[packetlength];
            using (var ms = new MemoryStream(buffer))
            {
                var b = EndianBitConverter.BigEndian.GetBytes(buffer.Length);

                await ms.WriteAsync(b, 0, 4);
                b = EndianBitConverter.BigEndian.GetBytes(magic);
                await ms.WriteAsync(b, 0, 2);
                b = EndianBitConverter.BigEndian.GetBytes(ver);
                await ms.WriteAsync(b, 0, 2);
                b = EndianBitConverter.BigEndian.GetBytes(action);
                await ms.WriteAsync(b, 0, 4);
                b = EndianBitConverter.BigEndian.GetBytes(param);
                await ms.WriteAsync(b, 0, 4);
                if (playload.Length > 0)
                {
                    await ms.WriteAsync(playload, 0, playload.Length);
                }
                await netStream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        async Task<bool> SendJoinChannel(int channelId, string token)
        {
            var packetModel = new { roomid = channelId, uid = 0, protover = 2, token = token, platform = "danmuji" };
            var playload = JsonConvert.SerializeObject(packetModel);
            await SendSocketDataAsync(7, playload);
            return true;
        }
    }

    internal struct DanmakuProtocol
    {
        /// <summary>
        /// 消息总长度 (协议头 + 数据长度)
        /// </summary>
        public int PacketLength;
        /// <summary>
        /// 消息头长度 (固定为16[sizeof(DanmakuProtocol)])
        /// </summary>
        public short HeaderLength;
        /// <summary>
        /// 消息版本号
        /// </summary>
        public short Version;
        /// <summary>
        /// 消息类型
        /// </summary>
        public int Action;
        /// <summary>
        /// 参数, 固定为1
        /// </summary>
        public int Parameter;

        internal static DanmakuProtocol FromBuffer(byte[] buffer)
        {
            if (buffer.Length < 16) { throw new ArgumentException(); }
            return new DanmakuProtocol()
            {
                PacketLength = EndianBitConverter.BigEndian.ToInt32(buffer, 0),
                HeaderLength = EndianBitConverter.BigEndian.ToInt16(buffer, 4),
                Version = EndianBitConverter.BigEndian.ToInt16(buffer, 6),
                Action = EndianBitConverter.BigEndian.ToInt32(buffer, 8),
                Parameter = EndianBitConverter.BigEndian.ToInt32(buffer, 12),
            };
        }
    }
}