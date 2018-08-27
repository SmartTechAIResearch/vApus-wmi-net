/*
 * 2014 Sizing Servers Lab, affiliated with IT bachelor degree NMCT
 * University College of West-Flanders, Department GKG (www.sizingservers.be, www.nmct.be, www.howest.be/en)
 * 
 * Author(s):
 *    Dieter Vandroemme
 */
using RandomUtils.Log;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace vApus_wmi_net {
    internal class Properties {
        private static Properties _instance = new Properties();
        private string _name, _version, _copyright;
        private int _port = -1;
        private const int DEFAULTPORT = 5556;

        private Properties() { }

        public static Properties GetInstance() {
            return _instance;
        }

        /// <summary>
        /// This is actualy the Title from the assembly properties.
        /// </summary>
        public string Name {
            get {
                if (_name == null)
                    _name = Assembly.GetAssembly(this.GetType()).GetCustomAttribute<AssemblyTitleAttribute>().Title;
                return _name;
            }
        }
        /// <summary>
        /// This is actualy the Version from the assembly properties.
        /// </summary>
        public string Version {
            get {
                if (_version == null)
                    _version = Assembly.GetAssembly(this.GetType()).GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                return _version;
            }
        }
        /// <summary>
        /// This is actualy the Copyright from the assembly properties.
        /// </summary>
        public string Copyright {
            get {
                if (_copyright == null)
                    _copyright = Assembly.GetAssembly(this.GetType()).GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
                return _copyright;
            }
        }
        public int Port {
            get {
                if (_port == -1) {
                    try {
                        string propertiesfile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "vApus-agent.properties");
                        if (File.Exists(propertiesfile)) 
                            using (var sr = new StreamReader(propertiesfile)) 
                                while (sr.Peek() != -1) {
                                    string line = sr.ReadLine();
                                    if (line.StartsWith("port ", System.StringComparison.InvariantCultureIgnoreCase)) {
                                        if (!int.TryParse(line.Substring("port ".Length).Trim(), out _port))
                                            _port = DEFAULTPORT;
                                        break;
                                    }
                                }                            
                    }
                    catch (Exception ex) {
                        _port = DEFAULTPORT;
                        Loggers.Log(Level.Error, "Failed reasding the port from the properties file. Reverting to the default TCP port 5556.", ex);
                    }
                }
                return _port;
            }
        }
        /// <summary>
        /// In ms.
        /// </summary>
        public int RefreshCountersInterval {
            get {
                return 3000;
            }
        }

        /// <summary>
        /// Can be . or , because of for instance the locale settings. 
        /// </summary>
        public string DecimalSeparator {
            get {
                return CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;
            }
        }
    }
}
