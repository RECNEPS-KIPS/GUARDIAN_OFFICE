﻿// author:KIPKIPS
// describe:配置表管理,负责加载和解析配置表
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Framework.Core.Singleton;
using Framework.Common;
using Framework.Core.Container;

namespace Framework.Core.Manager.Config {
    [MonoSingletonPath("[Manager]/ConfigManager")]
    public class ConfigManager : MonoSingleton<ConfigManager> {
        private string logTag = "ConfigManager";
        private string configPath = "Config/"; //配置表路径
        private RestrictedDictionary<string, List<dynamic>> _configDict = new RestrictedDictionary<string, List<dynamic>>(); //配置总表
        private RestrictedDictionary<string, RestrictedDictionary<string, string>> _typeDict = new RestrictedDictionary<string, RestrictedDictionary<string, string>>();
        public override void Initialize() {
            AnalyticsConfig();
        }
        // 解析配置表
        private void AnalyticsConfig() {
            _configDict.EnableWrite();
            _typeDict.EnableWrite();
            // _configDict = new Dictionary<string, List<dynamic>>();
            //获取所有配置表
            // UIUtils.LoadJsonByPath<List<JObject>>("Data/" + tabName + ".json");
            DirectoryInfo dir = new DirectoryInfo(configPath);
            FileSystemInfo[] files = dir.GetFileSystemInfos();
            string configName = "";
            string fullName = "";
            for (int i = 0; i < files.Length; i++) {
                configName = files[i].Name.Replace(".json", "");
                // fullName = configPath + files[i].Name;
                if (!_configDict.ContainsKey(configName)) {
                    _configDict.Add(configName, new List<dynamic>());
                    _configDict[configName].Add(null); //预留一个位置
                    // configDict[configName].Add();
                    // LogManager.Log(configPath + files[i].Name);
                    try {
                        List<JObject> jObjList = JsonUtils.LoadJsonByPath<List<JObject>>(configPath + files[i].Name);
                        JObject metatable = jObjList[jObjList.Count - 1];
                        if (metatable.ContainsKey("__metatable")) {
                            IEnumerable<JProperty> metatableProperties = metatable.Properties();
                            if (!_typeDict.ContainsKey(configName)) {
                                _typeDict.Add(configName, new RestrictedDictionary<string, string>());
                            }
                            foreach (JProperty metatableProp in metatableProperties) {
                                if (metatableProp.Name != "__metatable") {
                                    _typeDict[configName].EnableWrite();
                                    _typeDict[configName].Add(metatableProp.Name, metatableProp.Value.ToString());
                                    _typeDict[configName].ForbidWrite();
                                }
                            }
                            for (int j = 0; j < jObjList.Count - 1; j++) {
                                RestrictedDictionary<string, dynamic> table = new RestrictedDictionary<string, dynamic>();
                                table.EnableWrite();
                                IEnumerable<JProperty> properties = jObjList[j].Properties();
                                foreach (JProperty prop in properties) {
                                    switch (prop.Value.Type.ToString()) {
                                        case "Integer":
                                            table[prop.Name] = (int)prop.Value;
                                            break;
                                        case "Float":
                                            table[prop.Name] = (float)prop.Value;
                                            break;
                                        case "Boolean":
                                            table[prop.Name] = (bool)prop.Value;
                                            break;
                                        case "String":
                                            table[prop.Name] = prop.Value.ToString();
                                            break;
                                        case "Array":
                                            table[prop.Name] = HandleArray(prop.Value.ToArray());
                                            break;
                                        case "Object":
                                            table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                            break;
                                    }
                                }
                                table.ForbidWrite();
                                _configDict[configName].Add(table);
                            }
                        }
                    } catch (Exception ex) {
                        LogManager.Log(logTag, configName);
                        LogManager.LogError(logTag, ex.ToString());
                    }
                }
            }
            _configDict.ForbidWrite();
            _typeDict.ForbidWrite();
            LogManager.Log(logTag, "Config table data is parsed");
        }
        /// <summary>
        /// 处理字典类型的配置表
        /// </summary>
        /// <param name="jObj"></param>
        /// <param name="filedName"></param>
        /// <param name="cfName"></param>
        /// <returns></returns>
        private dynamic HandleDict(JObject jObj, string filedName, string cfName) {
            dynamic table = new RestrictedDictionary<dynamic, dynamic>();
            table.EnableWrite();
            RestrictedDictionary<string, string> valueTypeDict = _typeDict[cfName];
            IEnumerable<JProperty> properties = jObj.Properties();
            dynamic key = null;
            foreach (JProperty prop in properties) {
                if (valueTypeDict.ContainsKey(filedName)) {
                    if (valueTypeDict[filedName].StartsWith("dict<int")) {
                        key = int.Parse(prop.Name);
                    } else if (valueTypeDict[filedName].StartsWith("dict<float")) {
                        key = float.Parse(prop.Name);
                    } else if (valueTypeDict[filedName].StartsWith("dict<string")) {
                        key = prop.Name;
                    }
                    switch (prop.Value.Type.ToString()) {
                        case "Integer":
                            table.Add(key, (int)prop.Value);
                            break;
                        case "Float":
                            table.Add(key, (float)prop.Value);
                            break;
                        case "Boolean":
                            table.Add(key, (bool)prop.Value);
                            break;
                        case "String":
                            table.Add(key, prop.Value.ToString());
                            break;
                    }
                }
            }
            table.ForbidWrite();
            // LogManager.Log(logTag, table);    
            return table;
        }
        // 递归处理数组类型
        private dynamic HandleArray(JToken[] array) {
            dynamic table = new RestrictedDictionary<int, dynamic>();
            table.EnableWrite();
            for (int i = 1; i <= array.Length; i++) {
                switch (array[i - 1].Type.ToString()) {
                    case "Integer":
                        table.Add(i, (int)array[i - 1]);
                        break;
                    case "Float":
                        table.Add(i, (float)array[i - 1]);
                        break;
                    case "Boolean":
                        table.Add(i, (bool)array[i - 1]);
                        break;
                    case "String":
                        table.Add(i, array[i - 1].ToString());
                        break;
                    case "Array":
                        table.Add(i, HandleArray(array[i - 1].ToArray()));
                        break;
                }
            }
            table.ForbidWrite();
            return table;
        }

        // 获取配置表
        public List<dynamic> GetConfig(string configName) {
            try {
                if (_configDict.ContainsKey(configName)) {
                    return _configDict[configName];
                }
            } catch (Exception e) {
                return null;
            }
            return null;
        }

        // 获取配置表的指定id的Hashtable
        public dynamic GetConfig(string configName, int id) {
            try {
                if (_configDict.ContainsKey(configName)) {
                    if (_configDict[configName] != null && _configDict[configName][id] != null) {
                        return _configDict[configName][id];
                    }
                }
            } catch (Exception e) {
                return null;
            }
            return null;
        }
    }
}