using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;

namespace SysBot.ACNHFishing
{
    internal static class Program
    {
        private const string DefaultConfigPath = "config.json";

        private static async Task Main(string[] args)
        {
            string configPath;

            Console.WriteLine("Starting up...");
            if (args.Length > 0) {
                if (args.Length > 1) {
                    Console.WriteLine("Too many arguments supplied and will be ignored.");
                    configPath = DefaultConfigPath;
                }
                else {
                    configPath = args[0];
                }
            }
            else {
                configPath = DefaultConfigPath;
            }

            if (!File.Exists(configPath))
            {
                CreateConfigQuit(configPath);
                return;
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<CrossBotConfig>(json);
            if (config == null)
            {
                Console.WriteLine("Failed to deserialize configuration file.");
                WaitKeyExit();
                return;
            }

            SaveConfig(config, configPath);
            await BotRunner.RunFrom(config, CancellationToken.None).ConfigureAwait(false);
            WaitKeyExit();
        }

        private static void SaveConfig<T>(T config, string path)
        {
            var options = new JsonSerializerOptions {WriteIndented = true};
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(path, json);
        }

        private static void CreateConfigQuit(string configPath)
        {
            SaveConfig(new CrossBotConfig {IP = "192.168.0.1", Port = 6000}, configPath);
            Console.WriteLine("Created blank config file. Please configure it and restart the program.");
            WaitKeyExit();
        }

        private static void WaitKeyExit()
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
