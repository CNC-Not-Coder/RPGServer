
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
/// <summary>
/// select 模型实现的网络IO
/// </summary>
namespace RPGServer.NetWork
{
    class IOBuffer
    {
        public bool IsUsed;
        public byte[] Data;
        public uint ReadPointer;
        public uint WritePointer;
        public IOBuffer()
        {
            IsUsed = false;
            Data = new byte[512 * 1024];//512K
            ReadPointer = 0;
            WritePointer = 0;
        }
        public void Reset()
        {
            ReadPointer = 0;
            WritePointer = 0;
            IsUsed = false;
        }
        public void Release()
        {
            Data = null;
            ReadPointer = 0;
            WritePointer = 0;
            IsUsed = false;
        }
    }
    class IOBufferManager
    {
        private List<IOBuffer> mBufferList = new List<IOBuffer>();
        private static IOBufferManager s_Instance = new IOBufferManager();
        public static IOBufferManager Instance
        {
            get { return s_Instance; }
        }
        public IOBuffer NewIOBuffer()
        {
            int len = mBufferList.Count;
            for (int i = 0; i < len; i++)
            {
                if (!mBufferList[i].IsUsed)
                {
                    mBufferList[i].IsUsed = true;
                    return mBufferList[i];
                }
            }
            IOBuffer buff = mBufferList.Find(p => p.IsUsed == false);
            if(buff == null)
            {
                buff = new IOBuffer();
                mBufferList.Add(buff);
            }
            buff.Reset();
            buff.IsUsed = true;
            return buff;
        }
        public void DeleteIOBuffer(IOBuffer buffer)
        {
            if (buffer == null)
                return;
            buffer.IsUsed = false;
            int index = mBufferList.IndexOf(buffer);
            if (index < 0)
                buffer.Release();
        }
    }
    class IOObject
    {
        public bool IsValid { get; set; }
        public IOBuffer SendBuffer { get; set; }
        public IOBuffer ReceiveBuffer { get; set; }
        public IOObject()
        {
            SendBuffer = IOBufferManager.Instance.NewIOBuffer();
            ReceiveBuffer = IOBufferManager.Instance.NewIOBuffer();
            if(SendBuffer == null || ReceiveBuffer == null)
            {
                IOBufferManager.Instance.DeleteIOBuffer(SendBuffer);
                IOBufferManager.Instance.DeleteIOBuffer(ReceiveBuffer);
            }

            IsValid = true;
        }
        //发送缓冲区是否有数据
        public bool NeedSend
        {
            get {throw new NotImplementedException();}
        }
        //接收缓冲是否满了
        public bool NeedReceive
        {
            get {throw new NotImplementedException();}
        }
        //将一条消息加入发送缓冲区，发送缓冲区不够直接丢弃？
        public void SendMessage(byte[] data)
        {
            throw new NotImplementedException();
        }
        //从接收缓冲区获取一条完整消息（验证size）
        public byte[] PeekMessage()
        {
            throw new NotImplementedException();
        }
        //客户端数据到来，将数据添加到接收缓冲区，缓冲区不够就直接丢弃？
        public void NotifyMessageReceived(byte[] data, int offset, int size)
        {
            
        }
        //从发送缓冲区取出消息发送
        public int GetSendBuffer(byte[] container)
        {
            throw new NotImplementedException();
        }
        //数据发送完成，完成了多少
        public void NotifyMessageSent(int size)
        {
            
        }
    }
    class NetworkIO
    {
        public delegate void OnConnectedDelegate(IOObject client);
        public OnConnectedDelegate OnConnected = null;

        private Dictionary<Socket, IOObject> mIOObjectMap = new Dictionary<Socket, IOObject>();
        private Socket mListener = null;
        private List<Socket> mSendList = new List<Socket>();
        private List<Socket> mReceiveList = new List<Socket>();
        private List<Socket> mErrorCheck = new List<Socket>();
        private byte[] mReceiveBuff = new byte[10 * 1024];//10K
        private byte[] mSendBuff = new byte[10 * 1024];//10K
        public void Init(int port)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint addr = new IPEndPoint(IPAddress.Any, port);
            s.Bind(addr);
            s.Listen(128);
            s.Blocking = false;
            s.NoDelay = true;
            mListener = s;
        }
        public void Tick()
        {
            GenNeedSendSockets();
            GenNeedReceiveSockets();

            Socket.Select(mReceiveList, mSendList, mErrorCheck, 0);

            ProcessReceive();
            ProcessSend();
        }
        private void ProcessReceive()
        {
            Socket sk = null;
            int len = mReceiveList.Count;
            for (int i = 0; i < len; i++)
            {
                sk = mReceiveList[i];
                if (sk != mListener)
                {
                    IOObject buffer = GetIOObjectBySocket(sk);
                    if (buffer != null)
                    {
                        int size = sk.Available > mReceiveBuff.Length ? mReceiveBuff.Length : sk.Available;
                        int ct = mReceiveList[i].Receive(mReceiveBuff, 0, size, SocketFlags.None);
                        buffer.NotifyMessageReceived(mReceiveBuff, 0, ct);
                    }
                }
                else
                {
                    Socket s = mListener.Accept();
                    s.Blocking = false;
                    s.NoDelay = true;
                    IOObject client = new IOObject();
                    mIOObjectMap.Add(s, client);
                    if(OnConnected != null)
                    {
                        OnConnected.Invoke(client);
                    }
                }
            }
        }
        private void ProcessSend()
        {
            Socket sk = null;
            IOObject buffer = null;
            int len = mSendList.Count;
            for (int i = 0; i < len; i++)
            {
                sk = mSendList[i];
                buffer = GetIOObjectBySocket(sk);
                if (buffer != null)
                {
                    int size = buffer.GetSendBuffer(mSendBuff);
                    int sendSize = sk.Send(mSendBuff, 0, size, SocketFlags.None);
                    buffer.NotifyMessageSent(sendSize);
                }
            }
        }
        private List<Socket> GenNeedSendSockets()
        {
            List<Socket> list = mSendList;
            list.Clear();
            foreach(var pair in mIOObjectMap)
            {
                if(pair.Value.NeedSend)
                {
                    list.Add(pair.Key);
                }
            }
            return list;
        }
        private List<Socket> GenNeedReceiveSockets()
        {
            List<Socket> list = mReceiveList;
            list.Clear();
            list.Add(mListener);
            foreach (var pair in mIOObjectMap)
            {
                if (pair.Value.NeedReceive)
                {
                    list.Add(pair.Key);
                }
            }
            mErrorCheck.Clear();
            mErrorCheck.AddRange(list);
            return list;
        }
        private IOObject GetIOObjectBySocket(Socket s)
        {
            if(mIOObjectMap.ContainsKey(s))
            {
                return mIOObjectMap[s];
            }
            return null;
        }
    }
}
