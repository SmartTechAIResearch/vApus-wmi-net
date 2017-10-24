/*
 * Copyright 2017 (c) Sizing Servers Lab
 * University College of West-Flanders, Department GKG
 * 
 * Author(s):
 *    Dieter Vandroemme
 */
using RandomUtils.Log;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WMI;

namespace vApus_wmi_net {
    internal static class Server {
        private static bool _running;
        private static TcpListener _listenerv4;
        private static TcpListener _listenerv6;

        [ThreadStatic]
        private static Monitor _monitor;

        public static bool Running {
            get { return _running; }
            set { _running = value; }
        }

        public static void Start() {
            try {
                int port = Properties.GetInstance().Port;
                _listenerv4 = new TcpListener(IPAddress.Any, port);
                _listenerv6 = new TcpListener(IPAddress.IPv6Any, port);

                _listenerv4.Start();
                _listenerv6.Start();

                _running = true;

                ThreadPool.QueueUserWorkItem((state) => {
                    while (_running) AcceptSocket(_listenerv4);
                }, null);

                ThreadPool.QueueUserWorkItem((state) => {
                    while (_running) AcceptSocket(_listenerv6);
                }, null);

                Console.WriteLine("Listening at port " + port);

            }
            catch (Exception ex) {
                _running = false;
                Loggers.Log(Level.Error, "Failed starting the server.", ex);
            }
        }

        public static void Stop() { _running = false; }

        private static void AcceptSocket(TcpListener listener) {
            try {
                var client = listener.AcceptSocket();

                ThreadPool.QueueUserWorkItem((state) => {
                    var c = state as Socket;
                    _monitor = new Monitor(c);

                    while (_running && c.Connected)
                        HandleRequest(c);
                }, client);
            }
            catch (Exception ex) {
                Loggers.Log(Level.Error, "Failed to accecpt the client connection.", ex);
            }
        }

        private static void HandleRequest(Socket client) {
            try {
                string message = SocketHelper.Read(client);

                switch (message) {
                    case "name":
                        message = Properties.GetInstance().Name;
                        break;
                    case "version":
                        message = Properties.GetInstance().Version;
                        break;
                    case "copyright":
                        message = Properties.GetInstance().Copyright;
                        break;
                    case "config":
                        message = WmiHelper.Config;
                        break;
                    case "sendCountersInterval":
                        message = Properties.GetInstance().RefreshCountersInterval.ToString();
                        break;
                    case "decimalSeparator":
                        message = Properties.GetInstance().DecimalSeparator;
                        break;
                    case "wdyh":
                        message = WmiHelper.WDYHRepresentation;
                        break;
                    case "start":
                        _monitor.Start();
                        message = "200";
                        break;
                    case "stop":
                        _monitor.Stop();
                        message = "200";
                        SocketHelper.Write(client, "200");
                        client.Close();
                        break;
                    default:
                        if (message.StartsWith("{\"timestamp\":")) {
                            _monitor.SetWIW(message);
                            message = "200";
                        }
                        else {
                            message = "404";
                        }
                        break;
                }

                SocketHelper.Write(client, message);
            }
            catch (Exception ex) {
                if (client.Connected) {
                    client.Close();
                    if (Running)
                        Loggers.Log(Level.Error, "Failed to handle the received request.", ex);
                }
            }
        }
    }
}
