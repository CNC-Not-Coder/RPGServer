
using System;
using System.Collections.Generic;
using System.Net.Sockets;
/// <summary>
/// select 模型实现的网络IO
/// </summary>
namespace RPGServer.NetWork
{
    class SendBuffer
    {
        public bool IsSendOK { get; set; }
        public byte[] Data { get; set; }
        public uint SendOffset { get; set; }
    }
    class ReceiveBuffer
    {
        public bool IsReceiveOK { get; set; }
        public byte[] Data { get; set; }
        public uint ReceiveOffset { get; set; }
    }
    class IOObject
    {
        public bool IsValid { get; set; }
        public Socket Handle { get; set; }
        public List<SendBuffer> SendList { get; set; }
        public List<ReceiveBuffer> ReceiveList { get; set; }
        public IOObject(Socket handle)
        {
            if (handle == null || !handle.Connected)
            {
                IsValid = false;
                return;
            }
            IsValid = true;
            Handle = handle;
            SendList = new List<SendBuffer>();
            ReceiveList = new List<ReceiveBuffer>();
        }
    }
    class NetworkIO
    {
    }
}
