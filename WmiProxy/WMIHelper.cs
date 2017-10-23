/*
 * Copyright 2017 (c) Sizing Servers Lab
 * University College of West-Flanders, Department GKG
 * 
 * Author(s):
 *    Dieter Vandroemme
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using vApus.Monitor.Sources.Base;

namespace WMI {
    /// <summary>
    /// </summary>
    public static class WmiHelper {
        private static string _config, _wdyhRepresentation;
        private static Entities _wdyh;
        private static Dictionary<string, PerformanceCounter> _performanceCounters = new Dictionary<string, PerformanceCounter>(); //key == category + "." + counter + "." + instance (__Total__ surrogate for none)
        private static string[] _exceptcategories = { "Thread" };

        /// <summary>
        /// Initializes hw config and available counters. 
        /// </summary>
        public static void Init() {
            Console.WriteLine("Fetching the hardware configuration...");
            _config = Config;
            Console.WriteLine("OK");

            Console.WriteLine("Fetching the available counters...");
            _wdyh = WDYH;
            _wdyhRepresentation = WDYHRepresentation;
            Console.WriteLine("OK");
        }

        /// <summary>
        /// As XML.
        /// </summary>
        /// <returns></returns>
        public static string Config {
            get {
                if (_config != null) return _config;

                var systemInformation = new SystemInformation();
                string error = systemInformation.Get();
                if (!string.IsNullOrEmpty(error))
                    return error;

                var dic = new Dictionary<string, string[]>();
                // get all public instance properties
                PropertyInfo[] propertyInfos = typeof(SystemInformation).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo propInfo in propertyInfos)
                    dic.Add(propInfo.Name.Replace('_', ' '), propInfo.GetValue(systemInformation, null).ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

                return JsonConvert.SerializeObject(dic, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
        }

        /// <summary>
        /// Json serialized Entities. __Total__ surrogate instance for wmi counters that have none. 
        /// </summary>
        /// <returns></returns>
        public static Entities WDYH {
            get {
                if (_wdyh != null) return _wdyh;

                var wdyh = new Entities();
                var entity = new Entity(Environment.MachineName, true);
                string defaultInstance = "__Total__";

                PerformanceCounterCategory[] categories = PerformanceCounterCategory.GetCategories();
                Array.Sort(categories, PerformanceCounterCategoryComparer.GetInstance());
                foreach (PerformanceCounterCategory category in categories) {                 
                    try {
                        if (_exceptcategories.Contains(category.CategoryName)) continue; //Do not fetch temp counters --> takes forever and fails anyways.

                        string[] instances = category.GetInstanceNames();
                        Array.Sort(instances);

                        if (instances.Length == 0) {
                            PerformanceCounter[] counters = GetCounters(category);
                            if (counters != null)
                                foreach (PerformanceCounter counter in counters) {
                                    //Cleanup invalid counter
                                    if (counter.CounterName.Equals("No name", StringComparison.InvariantCultureIgnoreCase))
                                        continue;
                                    //try { counter.NextValue(); } catch { continue; } Too slow

                                    string counterInfoName = category.CategoryName + "." + counter.CounterName;
                                    var counterInfo = new CounterInfo(counterInfoName);

                                    counterInfo.GetSubs().Add(new CounterInfo(defaultInstance));

                                    string name = counterInfoName + "." + defaultInstance;
                                    if (!_performanceCounters.ContainsKey(name))
                                        _performanceCounters.Add(name, counter);

                                    entity.GetSubs().Add(counterInfo);
                                }
                        }
                        else {
                            foreach (string instance in instances) {
                                PerformanceCounter[] counters = GetCounters(category, instance);
                                if (counters != null)
                                    foreach (PerformanceCounter counter in counters) {
                                        //Cleanup invalid counter
                                        if (counter.CounterName.Equals("No name", StringComparison.InvariantCultureIgnoreCase))
                                            continue;
                                        //try { counter.NextValue(); } catch { continue; } Too slow

                                        string counterInfoName = category.CategoryName + "." + counter.CounterName;
                                        CounterInfo counterInfo = entity.GetSubs().Find(item => item.GetName() == counterInfoName);

                                        string name = counterInfoName + "." + instance;
                                        if (!_performanceCounters.ContainsKey(name)) {
                                            if (counterInfo == null) {
                                                counterInfo = new CounterInfo(counterInfoName);
                                                entity.GetSubs().Add(counterInfo);
                                            }

                                            _performanceCounters.Add(name, counter);
                                            counterInfo.GetSubs().Add(new CounterInfo(instance));
                                        }
                                    }
                            }
                        }
                    }
                    catch {
                        continue; //Corrupt or invalid category.
                    }
                }

                wdyh.GetSubs().Add(entity);
                
                return wdyh;
            }
        }

        /// <summary>
        /// Json serialized Entities. __Total__ surrogate instance for wmi counters that have none. 
        /// </summary>
        /// <returns></returns>
        public static string WDYHRepresentation {
            get {
                if (_wdyhRepresentation != null) return _wdyhRepresentation;
                return JsonConvert.SerializeObject(WDYH, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
        }
        private static PerformanceCounter[] GetCounters(PerformanceCounterCategory category, string instance = null) {
            PerformanceCounter[] counters = null;
            try {
                counters = instance == null ? category.GetCounters() : category.GetCounters(instance);
                Array.Sort(counters, PerformanceCounterComparer.GetInstance());
            }
            catch (Exception ex) {
                //Temp counter
            }
            return counters;
        }

        /// <summary>
        /// Returns wiw with filled in values.
        /// </summary>
        /// <param name="wiw"></param>
        /// <returns></returns>
        public static string RefreshValues(Entities wiw) {
            var settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            Entity entity = wiw.GetSubs()[0];
            foreach (CounterInfo counterInfo in entity.GetSubs())
                foreach (CounterInfo instance in counterInfo.GetSubs())
                    instance.SetCounter(FLoatToLongString(GetNextValue(counterInfo.GetName() + "." + instance.GetName())));

            wiw.SetTimestamp();
            string values = JsonConvert.SerializeObject(wiw, settings);
            return values;
        }
        private static float GetNextValue(string name) {
            if (_performanceCounters.ContainsKey(name))
                try {
                    return _performanceCounters[name].NextValue();
                }
                catch {
                    _performanceCounters.Remove(name);
                }
            return -1f;
        }

        private static string FLoatToLongString(float f) {
            string s = f.ToString().ToUpper();

            //if string representation was collapsed from scientific notation, just return it
            if (!s.Contains("E")) return s;

            char separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            string[] exponentParts = s.Split('E');
            string[] decimalParts = exponentParts[0].Split(separator);

            //fix missing decimal point
            if (decimalParts.Length == 1) decimalParts = new[] { exponentParts[0], "0" };
            string newNumber = decimalParts[0] + decimalParts[1];

            int exponentValue = int.Parse(exponentParts[1]);
            //positive exponent
            if (exponentValue > 0)
                s = newNumber + GetZeros(exponentValue - decimalParts[1].Length);
            else
                //negative exponent
                s = ("0" + separator + GetZeros(exponentValue + decimalParts[0].Length) + newNumber).TrimEnd('0');

            return s;
        }

        private static string GetZeros(int zeroCount) {
            if (zeroCount < 0)
                zeroCount = Math.Abs(zeroCount);

            return new string('0', zeroCount);
        }

        private class PerformanceCounterCategoryComparer : IComparer<PerformanceCounterCategory> {
            private static PerformanceCounterCategoryComparer _performanceCounterCategoryComparer = new PerformanceCounterCategoryComparer();

            public static PerformanceCounterCategoryComparer GetInstance() { return _performanceCounterCategoryComparer; }

            private PerformanceCounterCategoryComparer() { }

            public int Compare(PerformanceCounterCategory x, PerformanceCounterCategory y) { return x.CategoryName.CompareTo(y.CategoryName); }
        }
        private class PerformanceCounterComparer : IComparer<PerformanceCounter> {
            private static PerformanceCounterComparer _performanceCounterComparer = new PerformanceCounterComparer();

            public static PerformanceCounterComparer GetInstance() { return _performanceCounterComparer; }

            private PerformanceCounterComparer() { }

            public int Compare(PerformanceCounter x, PerformanceCounter y) { return x.CounterName.CompareTo(y.CounterName); }
        }
    }
}
