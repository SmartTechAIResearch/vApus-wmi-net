/*
 * Copyright 2017 (c) Sizing Servers Lab
 * University College of West-Flanders, Department GKG
 * 
 * Author(s):
 *    Dieter Vandroemme
 */
using RandomUtils.Log;
using System;
using WMI;

namespace vApus_wmi_net {
    class Program {
        static void Main(string[] args) {
            try {
                var properties = Properties.GetInstance();

                Console.WriteLine(properties.Name + " " + properties.Version);
                Console.WriteLine(properties.Copyright);
                Console.WriteLine();

                WmiHelper.Init();

                Server.Start();

                if (!Server.Running) return;
                
                string line;
                do {
                    line = Console.ReadLine();
                } while (line != "q");

                Server.Stop();
            }
            catch (Exception ex) {
                Loggers.Log(Level.Error, "Failed starting the agent.", ex);
            }
        }
    }
}
