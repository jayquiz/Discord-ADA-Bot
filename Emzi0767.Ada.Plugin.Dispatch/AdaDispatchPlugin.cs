﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Emzi0767.Ada.Config;
using Emzi0767.Ada.Plugins;

namespace Emzi0767.Ada.Plugin.Dispatch
{
    public class AdaDispatchPlugin : IAdaPlugin
    {
        public string Name { get { return "ADA Dispatch Plugin"; } }
        public IAdaPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(AdaDispatchPluginConfig); } }

        private AutoResetEvent are;
        private UTF8Encoding utf8;
        private AdaDispatchPluginConfig conf;

        public void Initialize()
        {
            L.W("ADA-Disp", "Initializing ADA Dispatch socket");
            this.conf = new AdaDispatchPluginConfig();
            this.utf8 = new UTF8Encoding(false);
            var ip = new IPEndPoint(IPAddress.Any, 64000);
            var listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ip);
            listener.Listen(100);
            ThreadPool.QueueUserWorkItem(RunSocket, listener);
            L.W("ADA-Disp", "Done; listening at {0}:{1}", ip.Address, ip.Port);
        }

        public void LoadConfig(IAdaPluginConfig config)
        {
            var cfg = config as AdaDispatchPluginConfig;
            if (cfg != null)
                this.conf = cfg;
        }

        private void RunSocket(object _)
        {
            var listener = (Socket)_;
            are = new AutoResetEvent(false);
            while (true)
            {
                are.Reset();
                var ea = new SocketAsyncEventArgs();
                ea.Completed += Ea_Completed;
                //listener.BeginAccept(new AsyncCallback(AcceptConnection), listener);
                listener.AcceptAsync(ea);
                are.WaitOne();
            }
        }

        private void Ea_Completed(object sender, SocketAsyncEventArgs e)
        {
        //    
        //}
        //
        //private void AcceptConnection(IAsyncResult ar)
        //{
            this.are.Set();
            //var listener = (Socket)ar.AsyncState;
            //var handler = listener.EndAccept(ar);
            var listener = e.AcceptSocket;
            var handler = e.ConnectSocket;

            var blen = new byte[8];
            var blr = 8;
            while (blr > 0)
                blr -= handler.Receive(blen);
            var bl = BitConverter.ToUInt64(blen, 0);

            var buff = new byte[4096];
            var bfr = bl;
            while (bfr > 0)
                bfr -= (ulong)handler.Receive(buff, (int)(bl - bfr), (int)bfr, SocketFlags.None);

            var msg = utf8.GetString(buff, 0, (int)bl);
            msg = string.Format("**Date**: {0:yyyy-MM-dd HH:mm.ss zzz}{3}**Endpoint**: {1}{3}**Message**:{3}```{3}{2}{3}```", DateTime.Now, handler.RemoteEndPoint, msg, Environment.NewLine);
            AdaBotCore.AdaClient.SendMessage(msg, 207896989088743424u);
            L.W("ADA-Disp", "Message from {0}", handler.RemoteEndPoint);
        }
    }
}
