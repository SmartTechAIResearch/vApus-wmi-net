/*
 * 2017 Sizing Servers Lab, affiliated with IT bachelor degree NMCT
 * University College of West-Flanders, Department GKG (www.sizingservers.be, www.nmct.be, www.howest.be/en)
 * 
 * Author(s):
 *    Dieter Vandroemme
 */
using Newtonsoft.Json;
using RandomUtils.Log;
using System;
using System.Net.Sockets;
using System.Timers;
using vApus.Monitor.Sources.Base;
using WMI;

namespace vApus_wmi_net {
    internal class Monitor {
        private Timer _timer = new Timer(Properties.GetInstance().RefreshCountersInterval);
        private Socket _client;
        private Entities _wiwEntities;

        public Monitor(Socket client) {
            _client = client;
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                SocketHelper.Write(_client, WmiHelper.RefreshValues(_wiwEntities));
            }
            catch (Exception ex) {
                if (_client.Connected) {
                    _client.Close();
                    if (Server.Running)
                        Loggers.Log(Level.Error, "Failed sending monitor counters.", ex);
                }
            }
        }

        /// <summary>
        /// Sets the wiw.
        /// </summary>
        /// <param name="wiw">The wiw as json.</param>
        internal void SetWIW(string wiw) { _wiwEntities = JsonConvert.DeserializeObject<Entities>(wiw); }

        public void Start() { _timer.Start(); }
        public void Stop() { _timer.Stop(); }
    }
}
