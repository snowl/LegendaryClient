using System;
using RtmpSharp.IO;
using RtmpSharp.IO.AMF3;

namespace LegendaryClient.Logic.Riot
{
    [Serializable]
    [SerializedName("com.riotgames.platform.broadcast.BroadcastNotification")]
    public class BroadcastNotification
    {
        [SerializedName("broadcastMessages")]
        public object[] BroadcastMessages { get; set; }

        public void ReadExternal(IDataInput input)
        {

        }

        public void WriteExternal(IDataOutput output)
        {
            ;
        }
    }
}
