using Ices.Crossport.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.LibCrossport.Settings;
using UnityEngine;
using UnityEngine.Windows;
using File = System.IO.File;

namespace Ices.Crossport
{
    public class CrossportConfigurationManager
    {
        private static Dictionary<string, string> _configs = new();

        public static CrossportSetting GetReceiverDefault() =>
            new()
            {
                signaling = new()
                {
                    address = CrossportClientUtils.PresetServers[0],
                    capacity = 0,
                    interval = 5.0f
                },
                audio = new() { CrossportAudioSetting.Default() },
                video = new() { CrossportVideoSetting.Default() },
            };

        private static string ConfigurationDirectory
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
                {
                    return Application.persistentDataPath;
                }

                var args = Environment.GetCommandLineArgs().ToList();
                var dirIntend = args.IndexOf("-cpd");
                return dirIntend != -1
                    ? args[dirIntend + 1]
                    : Environment.GetEnvironmentVariable("CROSSPORT_CFG_DIR") ?? Environment.CurrentDirectory;
            }
        }

        public static void Initialize()
        {
            foreach (var configFile in new DirectoryInfo(ConfigurationDirectory).GetFiles()
                         .Where(f => f.Name.EndsWith(".cpcfg.json")))
            {
                var configName = configFile.Name[..^(".cpcfg.json".Length)];
                _configs.Add(configName, configFile.FullName);
            }
        }

        public static CrossportSetting GetRawSetting(string configFileName)
        {
            return JsonUtility.FromJson<CrossportSetting>(File.ReadAllText(configFileName));
        }

        public static CrossportSetting GetSetting(string configName)
        {
            if (!_configs.Any()) Initialize();
            if (_configs.ContainsKey(configName))
                return JsonUtility.FromJson<CrossportSetting>(File.ReadAllText(_configs[configName]));
            Debug.LogErrorFormat
            (
                "Setting named '{0}' is not found in config dir '{1}'",
                configName,
                ConfigurationDirectory
            );
            return null;
        }

        public static CrossportSetting GetPlaceHoldenSetting(string configName)
        {
            if (!_configs.Any()) Initialize();
            if (_configs.ContainsKey(configName))
                return JsonUtility.FromJson<CrossportSetting>
                    (RemovePlaceHolders(File.ReadAllText(_configs[configName])));
            Debug.LogErrorFormat
            (
                "Setting named '{0}' is not found in config dir '{1}'",
                configName,
                ConfigurationDirectory
            );
            return null;
        }

        private static readonly Regex PlaceHolderRegex = new(@"\$\{\w+\}");

        private static string RemovePlaceHolders(string origin)
        {
            var matches = PlaceHolderRegex.Matches(origin);
            for (var index = 0; index < matches.Count; index++)
            {
                var match = matches[index];
                if (!match.Success || string.IsNullOrEmpty(match.Value)) continue;
                var toReplace = match.Value;
                var envName = toReplace[2..^1];
                origin = origin.Replace(match.Value, Environment.GetEnvironmentVariable(envName));
            }

            return origin;
        }

        public static void SaveSetting(string configName, CrossportSetting setting)
        {
            if (!_configs.ContainsKey(configName))
                _configs[configName] = Path.Join(ConfigurationDirectory, $"{configName}.cpcfg.json");
            File.WriteAllText(_configs[configName], JsonUtility.ToJson(setting));
        }
    }
}