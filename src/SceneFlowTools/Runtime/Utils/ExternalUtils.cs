using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SceneFlowTools.Runtime.DynamicDetection;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace SceneFlowTools.Runtime
{
    public static class ExternalUtils
    {
        public static string Host;
        private static readonly HttpClient _httpClient = GetHttpClient();

        private static HttpClient GetHttpClient()
        {
            ServicePointManager.DefaultConnectionLimit = 32;
            return new()
            {
                Timeout = TimeSpan.FromSeconds(600)
            };
        }

        public static async Task<List<List<string>>> SceneDivisionWithFilePath(string sceneInfoFilePath, string method,
            object extraParams = null)
        {
            var body = new Dictionary<string, object>
            {
                { "scene_info_path", sceneInfoFilePath },
                { "method", method },
                { "extra", extraParams }
            };

            var content = CreateGzipJsonContent(body);

            return await PostAsync<List<List<string>>>("/scene_division", content).ConfigureAwait(false);
        }

        public static async Task<List<List<string>>> SceneDivisionWithContent(string sceneInfo, string method,
            object extraParams = null)
        {
            var body = new Dictionary<string, object>
            {
                { "scene_info", sceneInfo },
                { "method", method },
                { "extra", extraParams }
            };

            var content = CreateGzipJsonContent(body);

            return await PostAsync<List<List<string>>>("/scene_division", content).ConfigureAwait(false);
        }

        public static async Task<List<DynamicInfo>> DynamicDetection(string scriptName, string script, bool useCache = true)
        {
            var body = new Dictionary<string, object>
            {
                { "script_name", scriptName },
                { "script", script },
                { "use_cache", useCache }
            };

            var content = CreateGzipJsonContent(body);

            return await PostAsync<List<DynamicInfo>>("/dynamic_detection", content).ConfigureAwait(false);
        }

        private static async Task<T> PostAsync<T>(string relativeUrl, HttpContent content)
        {
            if (relativeUrl.StartsWith("/"))
                relativeUrl = relativeUrl.Substring(1);
            var resp = await _httpClient.PostAsync(
                $"http://{Host}/{relativeUrl}",
                content
            ).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"{resp.StatusCode} - {resp.ReasonPhrase}");
            }

            var respContent = await resp.Content.ReadAsStringAsync();
            T result;
            try
            {
                result = JsonConvert.DeserializeObject<T>(respContent, new JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(),
                    }
                });
                Debug.Log($"{respContent} --> {JsonConvert.SerializeObject(result)}");
            }
            catch (Exception e)
            {
                throw new Exception($"unable to deserialize dynamic detection result: {respContent}", e);
            }


            if (result == null)
            {
                throw new Exception($"unable to deserialize dynamic detection result: {respContent}");
            }

            return result;
        }

        private static ByteArrayContent CreateGzipJsonContent(object obj, bool snakeCase = true)
        {
            var bodyJson = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = snakeCase ? new SnakeCaseNamingStrategy() : new DefaultNamingStrategy()
                }
            });
            var content = new ByteArrayContent(CompressString(bodyJson));
            content.Headers.ContentEncoding.Add("gzip");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return content;
        }

        private static byte[] CompressString(string str)
        {
            // Debug.Log($"CompressString: Request body: {str}");
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress))
            using (var writer = new StreamWriter(gzip, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(str);
            }

            return ms.ToArray();
        }

        public static string AssetPathToFullPath(string assetPath)
        {
            Debug.Log($"Application.dataPath: {Application.dataPath}");
            if (assetPath.StartsWith("Assets"))
            {
                return Path.Combine(Application.dataPath, assetPath.Substring("Assets".Length + 1));
            }

            throw new ArgumentException("path must start with Assets");
        }
    }
}