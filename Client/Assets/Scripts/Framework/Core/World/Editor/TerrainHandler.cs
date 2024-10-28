// author:KIPKIPS
// date:2024.10.27 11:50
// describe:

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Framework.Core.World
{
    public struct TerrainDataStruct
    {
        public Vector3 pos;
        public int sliceSize;
        public float treeDistance;
        public float treeBillboardDistance;
        public float treeCrossFadeLength;
        public int treeMaximumFullLODCount;
        public float detailObjectDistance;
        public float detailObjectDensity;
        public float heightmapPixelError;
        public int heightmapMaximumLOD;
        public float basemapDistance;
        public ShadowCastingMode shadowCastingMode;
        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
        public string materialGUID;
    }

    public class TerrainHandler
    {
        #region Variable

        private const string LOGTag = "TerrainHandler";
        private const string TerrainSplitChar = "_";
        public readonly List<GameObject> terrainList = new();
        public readonly List<GameObject> colliderList = new();

        #endregion

        #region Helper

        public static bool CheckSourceTerrainAsset(string terrainAssetPath)
        {
            return File.Exists(terrainAssetPath);
        }

        public static void FocusTerrain(Transform terrainRoot, string terrainName)
        {
            var trs = terrainRoot.Find(terrainName);
            if (!trs) return;
            EditorGUIUtility.PingObject(trs.gameObject);
            Selection.activeGameObject = trs.gameObject;
            SceneView.lastActiveSceneView.FrameSelected();
            // SceneView.FrameLastActiveSceneView();
            // FocusDelay();
        }

        //清理切分的地形
        public void ClearSplitTerrains()
        {
            foreach (var t in terrainList)
            {
                Object.DestroyImmediate(t);
            }

            terrainList.Clear();
        }

        #endregion

        #region Load Terrain

        //加载切分的地形
        public void LoadSplitTerrain(string worldName, Transform terrainRoot, Action callback = null)
        {
            var worldDir = $"{DEF.RESOURCES_ASSETS_PATH}/Worlds/{worldName}";
            if (!Directory.Exists(worldDir))
            {
                LogManager.Log(LOGTag, "There is no split terrain data");
                return;
            }

            DirectoryInfo dirInfo = new DirectoryInfo(worldDir);
            DirectoryInfo[] subDirs = dirInfo.GetDirectories();
            foreach (var t in subDirs)
            {
                if (!t.Name.StartsWith("Chunk")) continue;
                var split = t.Name.Split('_');
                var y = int.Parse(split[1]);
                var x = int.Parse(split[2]);
                var saveDir = $"{worldDir}/{t.Name}";
                if (!Directory.Exists(saveDir))
                {
                    LogManager.Log(LOGTag, "There is no split terrain data");
                    continue;
                }

                // TerrainDataStruct terrainInfo = LoadTerrainInfo($"{saveDir}/data.bytes");
                LoadTerrainChunk(worldName, terrainRoot, x, y);
            }

            callback?.Invoke();
        }

        private void LoadTerrainChunk(string worldName, Transform terrainRoot, int x, int y)
        {
            var chunkDir = $"Chunk{TerrainSplitChar}{y}{TerrainSplitChar}{x}";
            var saveDir = $"{DEF.RESOURCES_ASSETS_PATH}/Worlds/{worldName}/{chunkDir}";
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>($"{saveDir}/terrain.asset");

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = $"{y}_{x}";
            go.transform.SetParent(terrainRoot);
            go.transform.localPosition = new Vector3(x * td.size.x, 0, y * td.size.z);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.gameObject.isStatic = true;

            terrainList.Add(go);
        }

        public static Terrain LoadSingleTerrain(Transform terrainRoot, string terrainAssetPath,Action<GameObject> callback = null)
        {
            if (!File.Exists(terrainAssetPath))
            {
                LogManager.Log(LOGTag, "该世界不存在地形资源");
                return null;
            }
            
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainAssetPath);
            var go = Terrain.CreateTerrainGameObject(td);
            go.transform.SetParent(terrainRoot);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.gameObject.isStatic = true;
            callback?.Invoke(go);
            return go.GetComponent<Terrain>();
        }

        #endregion

        #region Split Terrain

        //分割地形
        public void SplitTerrain(Terrain terrain, string worldName, int rows, int columns)
        {
            if (terrain == null)
            {
                LogManager.LogError(LOGTag, "Target terrain is null");
                return;
            }

            try
            {
                var terrainData = terrain.terrainData;
                // var heightmap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
                // var originalHeightmapResolution = terrainData.heightmapResolution;
                // 计算适应原地图分辨率的混合贴图分辨率和基础贴图分辨率
                var adaptedAlphamapResolution = terrainData.baseMapResolution / rows;
                var adaptedBaseMapResolution = terrainData.alphamapResolution / rows;
                var heightmapResolution = (terrainData.heightmapResolution - 1) / rows;
                var splatProtos = terrainData.splatPrototypes;

                // 计算子地图的大小
                var originalSize = terrainData.size;
                var tileWidth = originalSize.x / columns;
                var tileLength = originalSize.z / rows;

                //循环宽和长,生成小块地形
                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < columns; col++)
                    {
                        //创建资源
                        var chunkDir = $"Chunk{TerrainSplitChar}{row}{TerrainSplitChar}{col}";

                        EditorUtility.DisplayProgressBar("正在分割地形", chunkDir,(row * rows + col) / (float)(rows * columns));
                        var saveDir = $"{DEF.RESOURCES_ASSETS_PATH}/Worlds/{worldName}/{chunkDir}";
                        if (AssetDatabase.IsValidFolder(saveDir))
                        {
                            AssetDatabase.DeleteAsset(saveDir);
                        }

                        Directory.CreateDirectory(saveDir);

                        var assetPath = $"{saveDir}/Terrain.asset";

                        // 创建一个新的GameObject用于表示子地图
                        var tileObject = Terrain.CreateTerrainGameObject(null);
                        tileObject.name = "Tile_" + row + "_" + col;
                        tileObject.transform.SetParent(terrain.transform);

                        //设置高度
                        var xBase = terrainData.heightmapResolution / rows;
                        var yBase = terrainData.heightmapResolution / rows;
                        var height = terrainData.GetHeights(yBase * col, xBase * row, xBase + 1, yBase + 1);

                        // 添加Terrain组件并设置高度图
                        var tileTerrain = tileObject.GetComponent<Terrain>();
                        var terrainData1 = CreateTerrainData(height, adaptedAlphamapResolution, adaptedBaseMapResolution, heightmapResolution, originalSize, rows, columns);
                        tileTerrain.terrainData = terrainData1;
                        terrainData1.name = tileTerrain.name + "_terrainData";
                        //Debug.Log(originalHeightmapResolution + "=>" + tileObject.name + ":" + tileTerrain.terrainData.heightmapResolution);

                        //设置地形原型
                        var newSplats = new SplatPrototype[splatProtos.Length];
                        for (var i = 0; i < splatProtos.Length; ++i)
                        {
                            newSplats[i] = new SplatPrototype
                            {
                                texture = splatProtos[i].texture,
                                tileSize = splatProtos[i].tileSize
                            };

                            var offsetX = (terrainData1.size.x * row) % splatProtos[i].tileSize.x + splatProtos[i].tileOffset.x;
                            var offsetY = (terrainData1.size.z * col) % splatProtos[i].tileSize.y + splatProtos[i].tileOffset.y;
                            newSplats[i].tileOffset = new Vector2(offsetX, offsetY);
                        }

                        terrainData1.splatPrototypes = newSplats;

                        // 调整子地图的大小和位置
                        var data = tileTerrain.terrainData;
                        data.size = new Vector3(tileWidth, originalSize.y, tileLength);
                        tileObject.transform.localPosition = new Vector3(col * tileWidth, 0, row * tileLength);
                        
                        //Tree
                        CopyVegetationData(terrainData, data, row, col, rows, columns);

                        // 设置地形纹理,草地
                        CopyTerrainTextureData(terrain, tileTerrain, row, col, rows, columns);
                        
                        AssetDatabase.CreateAsset(terrainData1, assetPath);

                        
                        AssetDatabase.SaveAssets();
                    }
                }
                
                GenerateWorldData(terrain,worldName,rows, columns,tileWidth,tileLength);
            }
            catch (Exception e)
            {
                LogManager.LogError(LOGTag, e.Message + "\n" + e.StackTrace);
            }
            finally
            {
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        private static void GenerateWorldData(Terrain terrain, string worldName,int rows, int columns,float tileWidth,float tileLength)
        {
            var savePath = $"{DEF.RESOURCES_ASSETS_PATH}/Worlds/{worldName}/WorldData.bin";
            if(File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            
            var fs = new FileStream(savePath, FileMode.Create);
            var writer = new BinaryWriter(fs);
            try
            {
                writer.Write(terrain.terrainData.size.y);//地形高度
                writer.Write(rows);//行数
                writer.Write(columns);//列数
                writer.Write(tileWidth);//地形块尺寸宽
                writer.Write(tileLength);//地形块尺寸高
            }
            catch (Exception e)
            {
                LogManager.LogError(LOGTag, e.Message);
            }

            writer.Close();
            fs.Close();
            AssetDatabase.Refresh();
        }

        private TerrainData CreateTerrainData(float[,] heightmap, int alphamapResolution, int baseMapResolution, int heightmapResolution, Vector3 originalSize, int rows, int columns)
        {
            var terrainData = new TerrainData
            {
                heightmapResolution = heightmapResolution,
                alphamapResolution = alphamapResolution,
                baseMapResolution = baseMapResolution,
                size = new Vector3(originalSize.x / columns, originalSize.y, originalSize.z / rows)
            };

            terrainData.SetHeights(0, 0, heightmap);
            return terrainData;
        }

        private void CopyVegetationData(TerrainData sourceTerrainData, TerrainData targetTerrainData, int row, int col, int rows, int columns)
        {
            // 获取原始地形的植被数据
            var sourceTreePrototypes = sourceTerrainData.treePrototypes;
            var sourceTreeInstances = sourceTerrainData.treeInstances;

            // 清空目标地形的植被数据
            targetTerrainData.treePrototypes = sourceTreePrototypes;

            // 创建新的 List 以保存调整过的植被实例
            var adjustedTreeInstances = new List<TreeInstance>();

            // 计算子地图的范围
            var tileWidth = sourceTerrainData.size.x / columns;
            var tileLength = sourceTerrainData.size.z / rows;

            var startX = col * tileWidth;
            var endX = (col + 1) * tileWidth;

            var startZ = row * tileLength;
            var endZ = (row + 1) * tileLength;

            // 复制植被实例，并根据地形的缩放和位置调整
            foreach (var sourceTreeInstance in sourceTreeInstances)
            {
                // 百分比坐标转换为真实坐标
                var normalizedX = sourceTreeInstance.position.x * sourceTerrainData.size.x;
                var normalizedZ = sourceTreeInstance.position.z * sourceTerrainData.size.z;

                // 检查植被实例是否在当前子地图的范围内
                if (!(normalizedX >= startX) || !(normalizedX < endX) || !(normalizedZ >= startZ) || !(normalizedZ < endZ))
                {
                    continue;
                }
                // 调整植被实例的位置
                var adjustedTreeInstance = sourceTreeInstance;
                adjustedTreeInstance.position = new Vector3(normalizedX % targetTerrainData.size.x / targetTerrainData.size.x, sourceTreeInstance.position.y, normalizedZ % targetTerrainData.size.z / targetTerrainData.size.z);
                LogManager.Log(LOGTag, targetTerrainData.name + "=>" + adjustedTreeInstance.position);
                // 添加调整过的植被实例到 List 中
                adjustedTreeInstances.Add(adjustedTreeInstance);
            }

            // 将 List 转换为数组并赋值给目标地形的植被数据
            targetTerrainData.treeInstances = adjustedTreeInstances.ToArray();
            //Debug.Log(targetTerrainData.name+ " treeInstances:"+ adjustedTreeInstances.Count);
        }

        private void CopyTerrainTextureData(Terrain sourceTerrain, Terrain targetTerrain, int row, int col, int rows, int columns)
        {
            var sourceTerrainData = sourceTerrain.terrainData;
            var targetTerrainData = targetTerrain.terrainData;

            // 获取原始地形的纹理数据
            var sourceAlphamaps = sourceTerrainData.GetAlphamaps(0, 0, sourceTerrainData.alphamapResolution, sourceTerrainData.alphamapResolution);

            // 计算子地图的范围
            var startAlphamapX = Mathf.FloorToInt((float)col / columns * sourceTerrainData.alphamapResolution);
            var endAlphamapX = Mathf.FloorToInt((float)(col + 1) / columns * sourceTerrainData.alphamapResolution);
            var startAlphamapY = Mathf.FloorToInt((float)row / rows * sourceTerrainData.alphamapResolution);
            var endAlphamapY = Mathf.FloorToInt((float)(row + 1) / rows * sourceTerrainData.alphamapResolution);

            // 计算目标地图的范围
            const int targetStartX = 0;
            var targetEndX = targetTerrainData.alphamapResolution;
            const int targetStartY = 0;
            var targetEndY = targetTerrainData.alphamapResolution;

            // 复制纹理数据
            var targetAlphamaps = new float[targetEndY - targetStartY, targetEndX - targetStartX, sourceTerrainData.alphamapLayers];
            for (var layer = 0; layer < sourceTerrainData.alphamapLayers; layer++)
            {
                for (int y = startAlphamapY, ty = targetStartY; y < endAlphamapY && ty < targetEndY; y++, ty++)
                {
                    for (int x = startAlphamapX, tx = targetStartX; x < endAlphamapX && tx < targetEndX; x++, tx++)
                    {
                        targetAlphamaps[ty - targetStartY, tx - targetStartX, layer] = sourceAlphamaps[y, x, layer];
                    }
                }
            }

            // 设置目标地形的纹理数据
            targetTerrainData.SetAlphamaps(0, 0, targetAlphamaps);

            // 更新地形纹理贴图
            targetTerrain.terrainData = targetTerrainData; // 更新 TerrainData 引用
            targetTerrain.Flush();
        }

        #endregion

        #region Collider Boxes

        //生成碰撞盒
        public void GenColliderBoxes(Transform colliderRoot, int xMax, int yMax, Vector2 chunkSize, Vector2 colliderSize, float terrainHeight, Action callback = null)
        {
            for (var y = 0; y < yMax; ++y)
            {
                for (var x = 0; x < xMax; ++x)
                {
                    GenColliderBox(colliderRoot, x, y, chunkSize, colliderSize, terrainHeight);
                }
            }
            callback?.Invoke();
        }

        private void GenColliderBox(Transform colliderRoot, int x, int y, Vector2 chunkSize, Vector2 colliderSize, float terrainHeight)
        {
            var nodeName = $"{y}_{x}";
            var oldTrs = colliderRoot.Find(nodeName);
            if (oldTrs)
            {
                Object.DestroyImmediate(oldTrs.gameObject);
            }

            var go = new GameObject(nodeName);
            go.transform.SetParent(colliderRoot);
            var trs = go.transform;
            trs.name = nodeName;
            trs.localRotation = Quaternion.identity;
            trs.localScale = Vector3.one;
            var collider = go.AddComponent<BoxCollider>();
            trs.localPosition = new Vector3((x + 0.5f) * chunkSize.x, 0, (y + 0.5f) * chunkSize.y);
            collider.size = new Vector3(colliderSize.x, terrainHeight, colliderSize.y);
            collider.isTrigger = true;
            colliderList.Add(go);
        }

        //清理碰撞盒
        public void ClearColliderBoxes(Action callback = null)
        {
            foreach (var t in colliderList)
            {
                Object.DestroyImmediate(t);
            }

            colliderList.Clear();
            callback?.Invoke();
        }

        //加载碰撞盒子
        public void LoadColliderBoxes(Transform colliderRoot, string worldName, int xMax, int yMax, Vector2 chunkSize,Action callback = null)
        {
            for (var y = 0; y < yMax; y++)
            {
                for (var x = 0; x < xMax; x++)
                {
                    var chunkDir = $"Chunk{TerrainSplitChar}{y}{TerrainSplitChar}{x}";
                    var saveDir = $"{DEF.RESOURCES_ASSETS_PATH}Environment/{worldName}/{chunkDir}";
                    var filePath = $"{saveDir}/data.bytes";
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var reader = new BinaryReader(fs);
                    try
                    {
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadInt32();
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadInt32();
                        reader.ReadSingle();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadSingle();
                        reader.ReadString();
                        var colliderX = reader.ReadInt32();
                        var colliderY = reader.ReadInt32();
                        var colliderZ = reader.ReadInt32();
                        GenColliderBox(colliderRoot, x, y, chunkSize, new Vector2(colliderX, colliderZ), colliderY);
                    }
                    catch (Exception e)
                    {
                        LogManager.LogError(LOGTag, e.Message);
                    }

                    reader.Close();
                    fs.Close();
                }
            }

            callback?.Invoke();
        }

        public static void SaveColliderBoxes(Transform colliderRoot, string worldName, int rows, int columns, Action callback = null)
        {
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    
                    var chunkDir = $"Chunk{TerrainSplitChar}{col}{TerrainSplitChar}{row}";
                    var savePath = $"{DEF.RESOURCES_ASSETS_PATH}/Worlds/{worldName}/{chunkDir}/Data.bin";
                    if(File.Exists(savePath))
                    {
                        File.Delete(savePath);
                    }
                    var fs = new FileStream(savePath, FileMode.Create);
                    var writer = new BinaryWriter(fs);
                    
                    writer.Write(0);
                }
            }
        }
        [Obsolete]
        public static void ObsoleteSaveColliderBoxes(Transform colliderRoot, string worldName, int xMax, int yMax,Action callback = null)
        {
            for (var y = 0; y < yMax; y++)
            {
                for (var x = 0; x < xMax; x++)
                {
                    var chunkDir = $"Chunk{TerrainSplitChar}{y}{TerrainSplitChar}{x}";
                    var saveDir = $"{DEF.RESOURCES_ASSETS_PATH}Environment/{worldName}/{chunkDir}";
                    var filePath = $"{saveDir}/data.bytes";
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var reader = new BinaryReader(fs);
                    var terrainData = new TerrainDataStruct();
                    var itemDataStructList = new List<ItemDataStruct>();
                    var success = true;
                    try
                    {
                        //terrain data
                        var posX = reader.ReadSingle();
                        var posY = reader.ReadSingle();
                        var posZ = reader.ReadSingle();
                        terrainData.pos = new Vector3(posX, posY, posZ);
                        terrainData.sliceSize = reader.ReadInt32();
                        terrainData.treeDistance = reader.ReadSingle();
                        terrainData.treeBillboardDistance = reader.ReadSingle();
                        terrainData.treeCrossFadeLength = reader.ReadInt32();
                        terrainData.treeMaximumFullLODCount = reader.ReadInt32();
                        terrainData.detailObjectDistance = reader.ReadSingle();
                        terrainData.detailObjectDensity = reader.ReadSingle();
                        terrainData.heightmapPixelError = reader.ReadSingle();
                        terrainData.heightmapMaximumLOD = reader.ReadInt32();
                        terrainData.basemapDistance = reader.ReadSingle();
                        terrainData.shadowCastingMode = (ShadowCastingMode)reader.ReadInt32();
                        terrainData.lightmapIndex = reader.ReadInt32();
                        var lightmapScaleOffsetX = reader.ReadSingle();
                        var lightmapScaleOffsetY = reader.ReadSingle();
                        var lightmapScaleOffsetZ = reader.ReadSingle();
                        var lightmapScaleOffsetW = reader.ReadSingle();
                        terrainData.lightmapScaleOffset = new Vector4(lightmapScaleOffsetX, lightmapScaleOffsetY,
                            lightmapScaleOffsetZ, lightmapScaleOffsetW);
                        terrainData.materialGUID = reader.ReadString();
                        //ignore line old collider
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();

                        //item data
                        var cnt = reader.ReadInt32();
                        for (var i = 0; i < cnt; i++)
                        {
                            ItemDataStruct ids = new ItemDataStruct();
                            var tX = reader.ReadSingle();
                            var tY = reader.ReadSingle();
                            var tZ = reader.ReadSingle();
                            ids.pos = new Vector3(tX, tY, tZ);
                            tX = reader.ReadSingle();
                            tY = reader.ReadSingle();
                            tZ = reader.ReadSingle();
                            ids.rotate = Quaternion.Euler(tX, tY, tZ);
                            tX = reader.ReadSingle();
                            tY = reader.ReadSingle();
                            tZ = reader.ReadSingle();
                            ids.scale = new Vector3(tX, tY, tZ);
                            ids.lightingMapIndex = reader.ReadInt32();
                            tX = reader.ReadSingle();
                            tY = reader.ReadSingle();
                            tZ = reader.ReadSingle();
                            var w = reader.ReadSingle();
                            ids.lightingMapOffsetScale = new Vector4(tX, tY, tZ, w);
                            ids.guid = reader.ReadString();
                            ids.name = reader.ReadString();
                            itemDataStructList.Add(ids);
                        }
                    }
                    catch (Exception e)
                    {
                        LogManager.LogError(LOGTag, e.Message);
                        success = false;
                    }

                    reader.Close();
                    fs.Close();
                    if (!success) continue;
                    {
                        File.Delete(filePath);
                        fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                        BinaryWriter writer = new BinaryWriter(fs);
                        //恢复记录的地形数据
                        Vector3 pos = terrainData.pos;
                        writer.Write(pos.x);
                        writer.Write(pos.y);
                        writer.Write(pos.z);
                        writer.Write(terrainData.sliceSize);
                        writer.Write(terrainData.treeDistance);
                        writer.Write(terrainData.treeBillboardDistance);
                        writer.Write(terrainData.treeCrossFadeLength);
                        writer.Write(terrainData.treeMaximumFullLODCount);
                        writer.Write(terrainData.detailObjectDistance);
                        writer.Write(terrainData.detailObjectDensity);
                        writer.Write(terrainData.heightmapPixelError);
                        writer.Write(terrainData.heightmapMaximumLOD);
                        writer.Write(terrainData.basemapDistance);
                        writer.Write((int)terrainData.shadowCastingMode);
                        writer.Write(terrainData.lightmapIndex);
                        writer.Write(terrainData.lightmapScaleOffset.x);
                        writer.Write(terrainData.lightmapScaleOffset.y);
                        writer.Write(terrainData.lightmapScaleOffset.z);
                        writer.Write(terrainData.lightmapScaleOffset.w);
                        writer.Write(terrainData.materialGUID);

                        //记录新的碰撞盒数据
                        var nodeName = $"{y}_{x}";
                        var oldTrs = colliderRoot.Find(nodeName);
                        var colliderX = 0;
                        var colliderY = 0;
                        var colliderZ = 0;
                        if (oldTrs)
                        {
                            var collider = oldTrs.GetComponent<BoxCollider>();
                            if (collider)
                            {
                                var v = collider.size;
                                colliderX = (int)v.x;
                                colliderY = (int)v.y;
                                colliderZ = (int)v.z;
                            }
                        }

                        writer.Write(colliderX);
                        writer.Write(colliderY);
                        writer.Write(colliderZ);

                        //恢复item数据
                        writer.Write(itemDataStructList.Count);
                        foreach (var ids in itemDataStructList)
                        {
                            writer.Write(ids.pos.x);
                            writer.Write(ids.pos.y);
                            writer.Write(ids.pos.z);
                            writer.Write(ids.rotate.x);
                            writer.Write(ids.rotate.y);
                            writer.Write(ids.rotate.z);
                            writer.Write(ids.scale.x);
                            writer.Write(ids.scale.y);
                            writer.Write(ids.scale.z);
                            writer.Write(ids.lightingMapIndex);
                            writer.Write(ids.lightingMapOffsetScale.x);
                            writer.Write(ids.lightingMapOffsetScale.y);
                            writer.Write(ids.lightingMapOffsetScale.z);
                            writer.Write(ids.lightingMapOffsetScale.w);
                            writer.Write(ids.guid);
                            writer.Write(ids.name);
                        }

                        writer.Flush();
                        writer.Close();
                        fs.Close();
                    }
                }
            }

            callback?.Invoke();
        }

        #endregion

        #region Convert Terrain

        //To Mesh
        public void Converter()
        {
            if (Selection.objects.Length <= 0)
            {
                LogManager.Log(LOGTag, "Please select the [Terrain] in the [Hierarchy]");
                return;
            }

            var terrainObj = Selection.objects[0] as GameObject;
            if (terrainObj == null)
            {
                LogManager.Log(LOGTag, "Select objects is not [GameObject]");
                return;
            }

            var terrain = terrainObj.GetComponent<Terrain>();
            if (terrain == null)
            {
                LogManager.Log(LOGTag, "Select the object missing [Terrain] component");
                return;
            }

            var terrainData = terrain.terrainData;
            if (terrainData == null)
            {
                LogManager.Log(LOGTag, "Terrain component missing TerrainData");
                return;
            }

            const int vertexCountScale = 4;
            var w = terrainData.heightmapResolution;
            var h = terrainData.heightmapResolution;
            var size = terrainData.size;
            var alphaMapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            var meshScale = new Vector3(size.x / (w - 1f) * vertexCountScale, 1, size.z / (h - 1f) * vertexCountScale);
            // [dev] terrainData.splatPrototypes 有问题,若每个图片大小不一,则出问题
            var uvScale = new Vector2(1f / (w - 1f), 1f / (h - 1f)) * vertexCountScale * (size.x / terrainData.splatPrototypes[0].tileSize.x);
            w = (w - 1) / vertexCountScale + 1;
            h = (h - 1) / vertexCountScale + 1;
            var vertices = new Vector3[w * h];
            var uvs = new Vector2[w * h];
            var alphasWeight = new Vector4[w * h];
            for (var i = 0; i < w; i++)
            {
                for (var j = 0; j < h; j++)
                {
                    var index = j * w + i;
                    var z = terrainData.GetHeight(i * vertexCountScale, j * vertexCountScale);
                    vertices[index] = Vector3.Scale(new Vector3(i, z, j), meshScale);
                    uvs[index] = Vector2.Scale(new Vector2(i, j), uvScale);

                    // alpha map
                    var i2 = (int)(i * terrainData.alphamapWidth / (w - 1f));
                    var j2 = (int)(j * terrainData.alphamapHeight / (h - 1f));
                    i2 = Mathf.Min(terrainData.alphamapWidth - 1, i2);
                    j2 = Mathf.Min(terrainData.alphamapHeight - 1, j2);
                    var alpha0 = alphaMapData[j2, i2, 0];
                    var alpha1 = alphaMapData[j2, i2, 1];
                    var alpha2 = alphaMapData[j2, i2, 2];
                    var alpha3 = alphaMapData[j2, i2, 3];
                    alphasWeight[index] = new Vector4(alpha0, alpha1, alpha2, alpha3);
                }
            }

            /*
             * 三角形
             *     b       c
             *      *******
             *      *   * *
             *      * *   *
             *      *******
             *     a       d
             */
            var triangles = new int[(w - 1) * (h - 1) * 6];
            var triangleIndex = 0;
            for (var i = 0; i < w - 1; i++)
            {
                for (var j = 0; j < h - 1; j++)
                {
                    var a = j * w + i;
                    var b = (j + 1) * w + i;
                    var c = (j + 1) * w + i + 1;
                    var d = j * w + i + 1;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                triangles = triangles,
                tangents = alphasWeight // 将地形纹理的比重写入到切线中
            };
            const string transName = "[dev]MeshFromTerrainData";
            var t = terrainObj.transform.parent.Find(transName);
            if (t == null)
            {
                var go = new GameObject(transName, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                t = go.transform;
            }

            // 地形渲染
            var mr = t.GetComponent<MeshRenderer>();
            var mat = mr.sharedMaterial;
            if (!mat) mat = new Material(Shader.Find("Custom/Environment/TerrainSimple"));
            for (var i = 0; i < terrainData.splatPrototypes.Length; i++)
            {
                var sp = terrainData.splatPrototypes[i];
                mat.SetTexture("_Texture" + i, sp.texture);
            }

            t.parent = terrainObj.transform.parent;
            t.position = terrainObj.transform.position;
            t.gameObject.layer = terrainObj.layer;
            t.GetComponent<MeshFilter>().sharedMesh = mesh;
            t.GetComponent<MeshCollider>().sharedMesh = mesh;
            mr.sharedMaterial = mat;
            t.gameObject.SetActive(true);
            terrainObj.SetActive(false);
            LogManager.Log(LOGTag, "Convert terrain to mesh finished!");
        }

        #endregion
    }
}