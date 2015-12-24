// Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using EnvDTE;
using Gauge.CSharp.Core;
using Gauge.VisualStudio.Exceptions;
using Gauge.VisualStudio.Extensions;
using Gauge.VisualStudio.Loggers;
using Process = System.Diagnostics.Process;

namespace Gauge.VisualStudio.Helpers
{
    public class GaugeDaemonHelper
    {
        private static readonly Dictionary<string, GaugeApiConnection> ApiConnections = new Dictionary<string, GaugeApiConnection>();

        private static readonly Dictionary<string, Process> ChildProcesses = new Dictionary<string, Process>();

        private static readonly Dictionary<string, int> ApiPorts = new Dictionary<string, int>();
        private static readonly List<Project> GaugeProjects = new List<Project>();

        public static GaugeApiConnection GetApiConnectionForActiveDocument()
        {
            return GetApiConnectionFor(GaugePackage.DTE.ActiveDocument.ProjectItem.ContainingProject);
        }

        public static void RegisterGaugeProject(Project project)
        {
            GaugeProjects.Add(project);
        }

        public static IEnumerable<GaugeApiConnection> GetAllApiConnections()
        {
            return GaugeProjects.Select(GetApiConnectionFor);
        }

        public static GaugeApiConnection GetApiConnectionFor(Project project)
        {
            GaugeApiConnection apiConnection;
            if (ApiConnections.TryGetValue(project.SlugifiedName(), out apiConnection))
                return apiConnection;
            apiConnection = StartGaugeAsDaemon(project);
            if (apiConnection != null)
            {
                ApiConnections.Add(project.SlugifiedName(), apiConnection);
            }
            return apiConnection;
        }

        private static int GetOpenPort()
        {
            const int scanStart = 1000;
            const int scanEnd = 2000;
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndPoints = properties.GetActiveTcpListeners();

            var portsInUse = tcpEndPoints.Select(p => p.Port).ToList();
            var unusedPort = 0;

            for (var port = scanStart; port < scanEnd; port++)
            {
                if (portsInUse.Contains(port)) continue;
                unusedPort = port;
                break;
            }
            return unusedPort;
        }

        public static void KillChildProcess(string slugifiedName)
        {
            if (ApiConnections.ContainsKey(slugifiedName))
            {
                try
                {
                    ApiConnections[slugifiedName].Dispose();
                    ApiConnections.Remove(slugifiedName);
                }
                catch
                {
                }
            }
            if (ChildProcesses.ContainsKey(slugifiedName))
            {
                try
                {
                    ChildProcesses[slugifiedName].Kill();
                    ChildProcesses.Remove(slugifiedName);
                }
                catch
                {
                }
            }
            if (ApiPorts.ContainsKey(slugifiedName))
            {
                ApiPorts.Remove(slugifiedName);
            }

            GaugeProjects.Remove(GaugeProjects.Find(project => string.CompareOrdinal(project.SlugifiedName(), slugifiedName) == 0));
        }

        public static bool ContainsApiConnectionFor(string slugifiedName)
        {
            return ApiConnections.ContainsKey(slugifiedName);
        }

        public static List<int> GetAllApiPorts()
        {
            return ApiPorts.Values.ToList();
        }
        private static GaugeApiConnection StartGaugeAsDaemon(Project gaugeProject)
        {
            var projectOutputPath = GetValidProjectOutputPath(gaugeProject);

            var openPort = GetOpenPort();
            OutputPaneLogger.WriteLine("[Debug] Opening Gauge Daemon for Project : {0},  at port: {1}", gaugeProject.Name, openPort);
            var slugifiedName = gaugeProject.SlugifiedName();

            ApiPorts.Add(slugifiedName, openPort);
            var gaugeStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(gaugeProject.FullName),
                UseShellExecute = false,
                FileName = "gauge.exe",
                CreateNoWindow = true,
                Arguments = "--daemonize",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            gaugeStartInfo.EnvironmentVariables["GAUGE_API_PORT"] = openPort.ToString(CultureInfo.InvariantCulture);
            try
            {
                gaugeStartInfo.EnvironmentVariables["gauge_custom_build_path"] = projectOutputPath;
            }
            catch
            {
                // ignored
            }

            var gaugeProcess = new Process
            {
                StartInfo = gaugeStartInfo
            };
            if (gaugeProcess.Start())
            {
                ChildProcesses.Add(slugifiedName, gaugeProcess);
            }

            OutputPaneLogger.WriteLine("[Debug] Opening Gauge Daemon with PID: {0}", gaugeProcess.Id);

            return new GaugeApiConnection(new TcpClientWrapper(openPort));
        }

        private static string GetValidProjectOutputPath(Project gaugeProject)
        {
            var projectOutputPath = gaugeProject.GetProjectOutputPath();

            if (string.IsNullOrEmpty(projectOutputPath))
            {
                OutputPaneLogger.WriteLine(
                    "[Error] Unable to retrieve Project Output path for Project : {0}. Not starting Gauge Daemon",
                    gaugeProject.Name);
                throw new GaugeApiInitializationException();
            }

            if (!Directory.Exists(projectOutputPath))
            {
                OutputPaneLogger.WriteLine("[Error] Project Output path '{0}' does not exists. Not starting Gauge Daemon",
                    projectOutputPath);
                throw new GaugeApiInitializationException();
            }

            if (!Directory.EnumerateFiles(projectOutputPath, "*.dll").Any())
            {
                OutputPaneLogger.WriteLine(
                    "[Error] Project Output path '{0}' does not contain any binadies. Ensure that project is built. Not starting Gauge Daemon",
                    projectOutputPath);
                throw new GaugeApiInitializationException();
            }

            return projectOutputPath;
        }
    }
}