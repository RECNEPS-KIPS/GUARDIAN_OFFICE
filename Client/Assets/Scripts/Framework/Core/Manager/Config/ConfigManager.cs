﻿// author:KIPKIPS
// describe:配置表管理,负责加载和解析配置表

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json.Linq;
using Framework.Core.Singleton;
using Framework.Common;
using Framework.Core.Container;

namespace Framework.Core.Manager.Config
{
    /// <summary>
    /// 配置管理器
    /// </summary>
    // [MonoSingletonPath("[Manager]/ConfigManager")]
    public class ConfigManager : Singleton<ConfigManager>
    {
        private static readonly Dictionary<EConfig, string> ConfigNameDict = new()
        {
            { EConfig.Color, "Color" },
            { EConfig.StartStringTable, "StartStringTable" },
            { EConfig.LevelSetting, "LevelSetting" },
            { EConfig.Scene, "Scene" },
            { EConfig.ColorDef, "ColorDef" },
            { EConfig.Character, "Character" },
            { EConfig.CharacterController, "CharacterController" },
            { EConfig.GrowthTemp, "GrowthTemp" },
        };

        private const string LOGTag = "ConfigManager";
        private const string ConfigPath = "Config/"; //配置表路径

        private static readonly RestrictedDictionary<string, List<dynamic>> _configDict = new(); //配置总表

        private static readonly RestrictedDictionary<string, RestrictedDictionary<string, string>> _typeDict = new();

        /// <summary>
        /// 
        /// </summary>
        public void Launch()
        {
            LogManager.Log(LOGTag, "ConfigManager Launch");
            AnalyticsConfig();
        }

        // 解析配置表
        private void AnalyticsConfig()
        {
            _configDict.EnableWrite();
            _typeDict.EnableWrite();
            //TODO:配置表加载优化,目前是全部加载
            // _configDict = new Dictionary<string, List<dynamic>>();
            //获取所有配置表
            // UIUtils.LoadJsonByPath<List<JObject>>("Data/" + tabName + ".json");
            var dir = new DirectoryInfo(ConfigPath);
            var files = dir.GetFileSystemInfos();
            foreach (var t in files)
            {
                var configName = t.Name.Replace(".json", "");
                // fullName = configPath + files[i].Name;
                if (_configDict.ContainsKey(configName)) continue;
                _configDict.Add(configName, new List<dynamic>());
                // _configDict[configName].Add(null); //预留一个位置
                // configDict[configName].Add();
                LogManager.Log(LOGTag, "Load Config:", t.Name);
                try
                {
                    var jObjList = JsonUtils.LoadJsonByPath<List<JObject>>(ConfigPath + t.Name);
                    var metatable = jObjList[^1];
                    if (metatable.ContainsKey("__metatable"))
                    {
                        var metatableProperties = metatable.Properties();
                        if (!_typeDict.ContainsKey(configName))
                        {
                            _typeDict.Add(configName, new RestrictedDictionary<string, string>());
                        }

                        foreach (var metatableProp in metatableProperties)
                        {
                            if (metatableProp.Name == "__metatable") continue;
                            _typeDict[configName].EnableWrite();
                            _typeDict[configName].Add(metatableProp.Name, metatableProp.Value.ToString());
                            _typeDict[configName].ForbidWrite();
                        }
                        // LogManager.Log(LOGTag,"_typeDict",configName,_typeDict);

                        for (var j = 0; j < jObjList.Count - 1; j++)
                        {
                            var table = new RestrictedDictionary<string, dynamic>();
                            table.EnableWrite();
                            var properties = jObjList[j].Properties();
                            foreach (var prop in properties)
                            {
                                if (configName == "Scene")
                                {
                                    LogManager.Log(LOGTag, configName, _typeDict[configName][prop.Name], prop.Value.Type.ToString());
                                }

                                switch (_typeDict[configName][prop.Name])
                                {
                                    case "int":
                                        table[prop.Name] = (int)prop.Value;
                                        break;
                                    case "float":
                                        table[prop.Name] = (float)prop.Value;
                                        break;
                                    case "bool":
                                        table[prop.Name] = (bool)prop.Value;
                                        break;
                                    case "string":
                                        table[prop.Name] = prop.Value.ToString();
                                        break;
                                    case "vector2":
                                        var v2 = prop.Value.ToArray();
                                        var v2o = UnityEngine.Vector2.zero;
                                        if (v2.Length == 2)
                                        {
                                            v2o.x = (float)v2[0];
                                            v2o.y = (float)v2[1];
                                        }
                                        table[prop.Name] = v2o;
                                        break;
                                    case "vector3":
                                        var v3 = prop.Value.ToArray();
                                        var v3o = UnityEngine.Vector3.zero;
                                        if (v3.Length == 3)
                                        {
                                            v3o.x = (float)v3[0];
                                            v3o.y = (float)v3[1];
                                            v3o.z = (float)v3[2];
                                        }
                                        table[prop.Name] = v3o;
                                        break;
                                    case "list<int>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "list<float>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "list<bool>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "list<string>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "list<list<int>>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "list<list<float>>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "list<list<bool>>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "list<list<string>>":
                                        table[prop.Name] = HandleArray(prop.Value.ToArray());
                                        break;
                                    case "dict<int,int>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<int,float>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<int,bool>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<int,string>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<float,int>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<float,float>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<float,bool>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<float,string>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<string,int>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<string,float>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<string,bool>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                    case "dict<string,string>":
                                        table[prop.Name] = HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName);
                                        break;
                                }
                                // table[prop.Name] = prop.Value.Type.ToString() switch
                                // {
                                //     "Integer" => (int)prop.Value,
                                //     "Float" => (float)prop.Value,
                                //     "Boolean" => (bool)prop.Value,
                                //     "String" => prop.Value.ToString(),
                                //     "Array" => HandleArray(prop.Value.ToArray()),
                                //     "Object" => HandleDict(prop.Value.ToObject<JObject>(), prop.Name, configName),
                                //     _ => table[prop.Name]
                                // };
                            }

                            table.ForbidWrite();
                            _configDict[configName].Add(table);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log(LOGTag, configName);
                    LogManager.LogError(LOGTag, ex.ToString());
                }
            }

            _configDict.ForbidWrite();
            _typeDict.ForbidWrite();
            LogManager.Log(LOGTag, "Config table data is parsed");
        }

        /// <summary>
        /// 处理字典类型的配置表
        /// </summary>
        /// <param name="jObj"></param>
        /// <param name="filedName"></param>
        /// <param name="cfName"></param>
        /// <returns></returns>
        private dynamic HandleDict(JObject jObj, string filedName, string cfName)
        {
            dynamic table = new RestrictedDictionary<dynamic, dynamic>();
            table.EnableWrite();
            var valueTypeDict = _typeDict[cfName];
            var properties = jObj.Properties();
            dynamic key = null;
            foreach (var prop in properties)
            {
                if (valueTypeDict.ContainsKey(filedName))
                {
                    if (valueTypeDict[filedName].StartsWith("dict<int"))
                    {
                        key = int.Parse(prop.Name);
                    }
                    else if (valueTypeDict[filedName].StartsWith("dict<float"))
                    {
                        key = float.Parse(prop.Name);
                    }
                    else if (valueTypeDict[filedName].StartsWith("dict<string"))
                    {
                        key = prop.Name;
                    }

                    switch (prop.Value.Type.ToString())
                    {
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
        private static dynamic HandleArray(IReadOnlyList<JToken> array)
        {
            dynamic table = new RestrictedDictionary<int, dynamic>();
            table.EnableWrite();
            for (var i = 0; i < array.Count; i++)
            {
                switch (array[i].Type.ToString())
                {
                    case "Integer":
                        table.Add(i, (int)array[i]);
                        break;
                    case "Float":
                        table.Add(i, (float)array[i]);
                        break;
                    case "Boolean":
                        table.Add(i, (bool)array[i]);
                        break;
                    case "String":
                        table.Add(i, array[i].ToString());
                        break;
                    case "Array":
                        table.Add(i, HandleArray(array[i].ToArray()));
                        break;
                }
            }

            table.ForbidWrite();
            return table;
        }

        /// <summary>
        /// 获取配置表
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        public static List<dynamic> GetConfig(EConfig configName)
        {
            try
            {
                if (ConfigNameDict.TryGetValue(configName, out var name))
                {
                    if (_configDict.TryGetValue(name, out var config))
                    {
                        return config;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }


        /// <summary>
        /// 获取配置表的指定id的Hashtable
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static dynamic GetConfigByID(EConfig configName, int id)
        {
            try
            {
                if (ConfigNameDict.TryGetValue(configName, out var name))
                {
                    if (_configDict.ContainsKey(name))
                    {
                        //待优化
                        foreach (var cf in _configDict[name])
                        {
                            if (cf["id"] == id)
                            {
                                return cf;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }
    }
}