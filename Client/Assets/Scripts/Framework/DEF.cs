﻿// author:KIPKIPS
// date:2022.05.10 22:54
// describe:定义类

namespace Framework
{
    /// <summary>
    /// 定义类
    /// </summary>
    public static class DEF
    {
        public const int SYSTEM_STANDARD_DPI = 96; //系统默认dpi
        public const int TRUE = 1;
        public const int FALSE = 0;
        
        //UIBinding枚举值的间隔
        public static readonly int BIND_ENUM_GAP = 10000;

        public static readonly string ASSET_BUNDLE_PATH = "Assets/ResourcesAssets/Misc/asset_bundles_map.asset";

        public static readonly string RESOURCES_ASSETS_PATH = "Assets/ResourcesAssets";
        
        public static readonly string ASSET_BUNDLE_SUFFIX = "ab";

        public enum ECharacterControllerType
        {
            FPS = 0,
            TPS_A = 1,
            TPS_B = 2,
            TPS_C = 3,
            TOPDOWN = 4,
        }
        
        public const int CHUNK_SIZE = 256;
        public const string ENV_ROOT = "[Environment]"; //根节点
        
        public const string COLLIDER_ROOT = "[Collider]"; //碰撞盒节点
        public const string ITEM_ROOT = "[Item]"; //场景元素节点
        public const string TERRAIN_ROOT = "[Terrain]"; //地形节点
        
        public static string LIGHTMAP_TEXTURE_DIR(int idx) => $"Lightmap-{idx}_comp_dir";
        public static string LIGHTMAP_TEXTURE_LIGHT(int idx) => $"Lightmap-{idx}_comp_light";
        public static string LIGHTMAP_TEXTURE_SHADOWMASK(int idx) => $"Lightmap-{idx}_comp_shadowmask";
    }
}