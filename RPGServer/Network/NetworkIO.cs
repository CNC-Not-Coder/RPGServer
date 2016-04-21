
using System;
using System.Collections.Generic;
using System.Net.Sockets;
/// <summary>
/// select 模型实现的网络IO
/// </summary>
namespace RPGServer.NetWork
{
    struct SendBuffer
    {
        public bool IsSendOK;
        public byte[] Data;
        public uint SendOffset;
        public SendBuffer(byte[] data)
        {
            IsSendOK = false;
            Data = data;
            SendOffset = 0;
        }
    }
    struct ReceiveBuffer
    {
        public bool IsReceiveOK;
        public byte[] Data;
        public uint ReceiveOffset;
        public ReceiveBuffer(byte[] data)
        {
            IsReceiveOK = true;
            Data = data;
            ReceiveOffset = 0;
        }
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
