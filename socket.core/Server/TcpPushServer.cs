﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using socket.core.Common;
using System.Net;

namespace socket.core.Server
{
    /// <summary>
    /// push 推出数据
    /// </summary>
    public class TcpPushServer
    {
        /// <summary>
        /// 基础类
        /// </summary>
        private TcpServer tcpServer;
        /// <summary>
        /// TCP keepalive 心跳时间
        /// </summary>
        private int keepAliveTime;
        /// <summary>
        /// TCP keepalive 超时重发间隔
        /// </summary>
        private int keepAliveInterval;
        /// <summary>
        /// 连接成功事件 item1:connectId
        /// </summary>
        public event Action<int> OnAccept;
        /// <summary>
        /// 接收通知事件 item1:connectId,item2:数据
        /// </summary>
        public event Action<int, byte[]> OnReceive;
        /// <summary>
        /// 已发送通知事件  item1:connectId,item2:长度
        /// </summary>
        public event Action<int, int> OnSend;
        /// <summary>
        /// 断开连接通知事件 item1:connectId,
        /// </summary>
        public event Action<int> OnClose;
        /// <summary>
        /// 客户端列表
        /// </summary>
        public ConcurrentDictionary<int, string> ClientList
        {
            get
            {
                if(tcpServer!=null)
                {
                    return tcpServer.clientList;
                }
                else
                {
                    return new ConcurrentDictionary<int, string>();
                }                
            }
        }

        /// <summary>
        /// 设置基本配置
        /// </summary>   
        /// <param name="numConnections">同时处理的最大连接数</param>
        /// <param name="receiveBufferSize">用于每个套接字I/O操作的缓冲区大小(接收端)</param>
        /// <param name="overtime">超时时长,单位秒.(每10秒检查一次)，当值为0时，不设置超时</param>
        public TcpPushServer(int numConnections, int receiveBufferSize, int overtime)
        {
            Thread thread = new Thread(new ThreadStart(() =>
            {
                tcpServer = new TcpServer(numConnections, receiveBufferSize, overtime);
                tcpServer.OnAccept += TcpServer_eventactionAccept;
                tcpServer.OnReceive += TcpServer_eventactionReceive;
                tcpServer.OnSend += TcpServer_OnSend;
                tcpServer.OnClose += TcpServer_eventClose;
            }));
            thread.IsBackground = true;
            thread.Start();
        }


        /// <summary>
        /// 开启监听服务
        /// </summary>        
        /// <param name="port">监听端口</param>
        public void Start(int port)
        {
            while (tcpServer == null)
            {
                Thread.Sleep(10);
            }
            tcpServer.Start(port);
        }
        /// <summary>
        /// 开启监听服务
        /// </summary>        
        /// <param name="ip">监听IP</param>
        /// <param name="port">监听端口</param>
        /// <param name="keepAliveTime">keepalive 心跳时间</param>
        /// <param name="keepAliveInterval">keepalive 心跳超时重发时间</param>
        public void Start(IPAddress ip, int port, int keepAliveTime, int keepAliveInterval)
        {
            while (tcpServer == null)
            {
                Thread.Sleep(10);
            }
            tcpServer.Start(ip, port, keepAliveTime, keepAliveInterval);
        }
        /// <summary>
        /// 连接成功事件方法
        /// </summary>
        /// <param name="connectId">连接标记</param>
        private void TcpServer_eventactionAccept(int connectId)
        {
            if (OnAccept != null)
                OnAccept(connectId);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="connectId">连接ID</param>
        /// <param name="data">数据</param>
        /// <param name="offset">偏移位</param>
        /// <param name="length">长度</param>
        public void Send(int connectId, byte[] data, int offset, int length)
        {
            tcpServer.Send(connectId, data, offset, length);
        }

        /// <summary>
        /// 发送通知事件
        /// </summary>
        /// <param name="connectId">连接标记</param>
        /// <param name="length">已发送长度</param>
        private void TcpServer_OnSend(int connectId, int length)
        {
            if (OnSend != null)
                OnSend(connectId, length);
        }

        /// <summary>
        /// 接收通知事件方法
        /// </summary>
        /// <param name="connectId">连接ID</param>
        /// <param name="data">数据</param>
        /// <param name="offset">偏移位</param>
        /// <param name="length">长度</param>
        private void TcpServer_eventactionReceive(int connectId, byte[] data,int offset,int length)
        {
            if (OnReceive != null)
            {
                byte[] da = new byte[length];
                Buffer.BlockCopy(data, offset, da, 0, length);
                OnReceive(connectId, da);
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="connectId">连接标记</param>
        public void Close(int connectId)
        {
            tcpServer.Close(connectId);
        }

        /// <summary>
        /// 断开连接通知事件方法
        /// </summary>
        /// <param name="connectId">连接标记</param>
        private void TcpServer_eventClose(int connectId)
        {
            if (OnClose != null)
                OnClose(connectId);
        }

        /// <summary>
        /// 给连接对象设置附加数据
        /// </summary>
        /// <param name="connectId">连接标识</param>
        /// <param name="data">附加数据</param>
        /// <returns>true:设置成功,false:设置失败</returns>
        public bool SetAttached(int connectId, object data)
        {
            return tcpServer.SetAttached(connectId, data);
        }

        /// <summary>
        /// 获取连接对象的附加数据
        /// </summary>
        /// <param name="connectId">连接标识</param>
        /// <returns>返回附加数据</returns>
        public T GetAttached<T>(int connectId)
        {
            return tcpServer.GetAttached<T>(connectId);
        }
    }
}
