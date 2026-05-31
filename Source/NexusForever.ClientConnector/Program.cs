using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NexusForever.ClientConnector.Configuration;
using NexusForever.ClientConnector.Native;

namespace NexusForever.ClientConnector
{
    internal static class Program
    {
        private const string jsonFile = "config.json";

        [DllImport("kernel32.dll")]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        public static void Main()
        {
            if (!File.Exists(jsonFile))
            {
                Console.Write("Type in your host name: ");
                string hostName = Console.ReadLine();

                Console.Write("Type in your language: [en,de]");
                string language = Console.ReadLine();

                var clientConfig = new ClientConfiguration
                {
                    HostName = hostName,
                    Language = language
                };

                File.WriteAllText(jsonFile, JsonConvert.SerializeObject(clientConfig));
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile(jsonFile, false)
                .Build();

            LaunchClient(configuration.Get<ClientConfiguration>());
        }

        private static void LaunchClient(ClientConfiguration config)
        {
            var si = new STARTUPINFO();
            var pi = new PROCESS_INFORMATION();

            string client = "WildStar64.exe";
            if (!File.Exists(client))
                client = "WildStar32.exe";

            int realmDataCenterId = config.RealmDataCenterId > 0 ? config.RealmDataCenterId : 6;
            string commandLine = $"/auth {config.HostName} /authNc {config.HostName} /lang {config.Language} /patcher {config.HostName} /SettingsKey WildStar /realmDataCenterId {realmDataCenterId}";
            string extraArguments = BuildArgumentList(config.ExtraArguments);
            if (!string.IsNullOrWhiteSpace(extraArguments))
                commandLine = $"{commandLine} {extraArguments}";

            CreateProcess(client,
                commandLine,
                IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
        }

        private static string BuildArgumentList(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (string.IsNullOrWhiteSpace(argument))
                    continue;

                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(QuoteArgument(argument));
            }

            return builder.ToString();
        }

        private static string QuoteArgument(string argument)
        {
            if (argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
                return argument;

            var builder = new StringBuilder();
            builder.Append('"');

            int backslashCount = 0;
            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '"')
                {
                    builder.Append('\\', backslashCount * 2 + 1);
                    builder.Append(character);
                    backslashCount = 0;
                    continue;
                }

                if (backslashCount > 0)
                {
                    builder.Append('\\', backslashCount);
                    backslashCount = 0;
                }

                builder.Append(character);
            }

            if (backslashCount > 0)
                builder.Append('\\', backslashCount * 2);

            builder.Append('"');
            return builder.ToString();
        }
    }
}
