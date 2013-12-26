using System;
using RtmpSharp.IO;
using RtmpSharp.IO.AMF3;
using System.Text;

namespace LegendaryClient.Logic.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.broadcast.BroadcastNotification")]
    public class BroadcastNotification : IExternalizable
    {
        [SerializedName("broadcastMessages")]
        public object[] BroadcastMessages { get; set; }
        public string Json { get; set; }

        public void ReadExternal(IDataInput input)
        {
            Json = input.ReadUtf((int)input.ReadUInt32());
        }

        public void WriteExternal(IDataOutput output)
        {
            var bytes = Encoding.UTF8.GetBytes(Json);

            output.WriteInt32(bytes.Length);
            output.WriteBytes(bytes);
        }
    }
}
