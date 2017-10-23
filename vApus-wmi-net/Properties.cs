/*
 * Copyright 2014 (c) Sizing Servers Lab
 * University College of West-Flanders, Department GKG
 * 
 * Author(s):
 *    Dieter Vandroemme
 */
using System.Globalization;
using System.Reflection;

namespace vApus_wmi_net {
    internal class Properties {
        private static Properties _instance = new Properties();
        private string _name, _version, _copyright;

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
        public int Port { get { return 5556; } }
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
