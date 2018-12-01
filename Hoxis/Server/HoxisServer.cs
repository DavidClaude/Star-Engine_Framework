﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using DacLib.Generic;
using DacLib.Codex;


namespace DacLib.Hoxis.Server
{
    public static class HoxisServer
    {
        #region ret codes
        public const string ERR_MSG_CFG_UNINITIALIZED = "Configuration file should be initialized first";
        #endregion

        /// <summary>
        /// Hoxis server configuration
        /// </summary>
        public static TomlConfiguration config { get; private set; }

        public static string ip { get; private set; }

        public static int port { get; private set; }

        /// <summary>
        /// The max count of socket connection
        /// </summary>
        public static int maxConnection { get; private set; }

        public static int remainConn { get { return _connReception.remain; } }

        /// <summary>
        /// Hoxis server basic direction
        /// </summary>
        public static string basicPath { get { return AppDomain.CurrentDomain.BaseDirectory + @"\..\..\DacLib\Hoxis"; } }

        private static Socket _socket;
        private static CriticalPreformPool<HoxisConnection> _connReception;
        private static List<HoxisCluster> _clusters;

        /// <summary>
        /// Init the configuration, such as the ip, port, socket
        /// </summary>
        /// <param name="configPath"></param>
        public static void InitConfig(string configPath = "")
        {
            Ret ret;
            // Init config
            string path;
            if (configPath != "") { path = configPath; }
            else { path = basicPath + @"\Configs\hoxis_server.toml"; }
            config = new TomlConfiguration(path, out ret);
            if (ret.code != 0) { Console.WriteLine("[error]HoxisServer init: {0}", ret.desc); return; }

            // Assign ip, port and init the sokcet
            ip = SystemFunc.GetLocalIP(out ret);
            if (ret.code != 0) { Console.WriteLine("[error]HoxisServer init: {0}", ret.desc); return; }
            port = config.GetInt("server", "port", out ret);
            if (ret.code != 0) { Console.WriteLine("[error]HoxisServer init: {0}", ret.desc); return; }
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

            // Init conn reception
            maxConnection = config.GetInt("server", "max_conn", out ret);
            if (ret.code != 0) { Console.WriteLine("[error]HoxisServer init: {0}", ret.desc); return; }
            _connReception = new CriticalPreformPool<HoxisConnection>(maxConnection);

            // Init cluster
            _clusters = new List<HoxisCluster>();
            HoxisCluster.maxUser = config.GetInt("cluster", "max_conn", out ret);
            if (ret.code != 0) { Console.WriteLine("[error]HoxisServer init: {0}", ret.desc); return; }

            // Init team

            // Init connection
            HoxisConnection.readBufferSize = config.GetInt("conn", "read_buffer_size", out ret);
            if (ret.code != 0) { Console.WriteLine("[error]HoxisServer init: {0}", ret.desc); return; }

            Console.WriteLine("Configurations init success, server IP: {0}, port: {1}", ip, port.ToString());
        }

        /// <summary>
        /// Bind and listen
        /// </summary>
        public static void Listen()
        {
            try
            {
                IPAddress addr = IPAddress.Parse(ip);
                IPEndPoint ep = new IPEndPoint(addr, port);
                if (_socket == null) { throw new Exception(ERR_MSG_CFG_UNINITIALIZED); }
                _socket.Bind(ep);
                int count = config.GetInt("socket", "max_client_count");
                _socket.Listen(count);
                Console.WriteLine("Listen success, max connection count: {0}", maxConnection.ToString());
            }
            catch (Exception e) { Console.Write("[error]HoxisServer listen: {0}", e.Message); }
        }

        /// <summary>
        /// Begin accept socket connection within thread
        /// </summary>
        public static void BeginAccept()
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    Socket cs = _socket.Accept();
                    //will delete
                    Console.WriteLine("[test]New client: " + cs.RemoteEndPoint.ToString());
                    Ret ret;
                    int id = _connReception.Request(cs, out ret);
                    if (ret.code != 0) { Console.WriteLine("[error]HoxisServer connection request: {0}, socekt: {1}", ret.desc, cs.RemoteEndPoint); }
                    HoxisConnection conn = _connReception.GetPreform(id, out ret);
                    if (ret.code != 0) { Console.WriteLine("[error]HoxisServer connection get: {0}, socekt: {1}", ret.desc, cs.RemoteEndPoint); }
                    conn.connID = id;
                }
            });
            t.Start();
            Console.WriteLine("Begin accepting connection...");
        }

        public static HoxisConnection[] GetAll()
        {
            return _connReception.GetAll();
        }
    }
}
