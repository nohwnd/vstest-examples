using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PartioningTests
{
    class Program
    {
        static async Task Main()
        {
            // in the next 60 lines I am just building the project and resolving the vstest console
            // this is specific to this approach, and is not mandatory to be done this way, 
            // I am just trying to make the project portable
            Console.WriteLine("Running dotnet --version");
            var dotnetVersion = RunCommand("dotnet", "--version").Trim();
            Console.WriteLine(dotnetVersion);
            Console.WriteLine("Running dotnet --list-sdks");
            var sdks = RunCommand("dotnet", "--list-sdks")
                    .Split(Environment.NewLine)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();
            sdks.ForEach(Console.WriteLine);

            var currentSdk = sdks.First(line => line.StartsWith(dotnetVersion));
            var sdkPath = currentSdk.Replace($"{dotnetVersion} [", "").TrimEnd(']');

            Console.WriteLine($"Sdk path is: {sdkPath}");

            var vstestConsolePath = Path.Combine(sdkPath, dotnetVersion, "vstest.console.dll");
            Console.WriteLine($"Test console path is: {vstestConsolePath}");


            Console.WriteLine($"Finding test project.");
            var here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testProjectName = "TestProject1";
            var testProjectPath = Path.GetFullPath(Path.Combine(here, "..", "..", "..", "..", testProjectName, $"{testProjectName}.csproj"));

            Console.WriteLine($"Building test project in path: {testProjectPath}");
            var buildOutput = RunCommand("dotnet", $"build {testProjectPath}");
            Console.WriteLine(buildOutput);

            var dllpath = buildOutput.Split(Environment.NewLine)
                .Select(line => line.Trim())
                .First(line => line.EndsWith($"{testProjectName}.dll"))
                .Replace($"{testProjectName} ->", "")
                .Trim();
            Console.WriteLine($"Test project dll: {dllpath}");

            var logPath = Path.Combine(here, "logs", "log.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            var consoleParameters = new ConsoleParameters { LogFilePath = logPath };

            // make the timeout shorter to see errors faster in case we do something wrong
            // and the runner won't start, you can look at the logs after the run, or use 
            // DebugView++ to see them in real time
            Environment.SetEnvironmentVariable("VSTEST_CONNECTION_TIMEOUT", "20");

            // THIS is where the interesting code starts :) We make a console wrapper, 
            // and point it at our console path, this can be either vstest.console.exe
            // if we are taking it from VS installation, or from Microsoft.TestPlaform
            // nuget package,
            // or vstest.console.dll if we are using the one from dotnet sdk.
            var wrapper = new VsTestConsoleWrapper(vstestConsolePath, consoleParameters);

            Console.WriteLine($"Discovering tests.");
            var discoveryHandler = new DiscoveryHandler();

            // Discover all tests from the assembly.
            // Make sure you don't provide null for the runsettings.
            // Make sure your dll paths are not sorrounded by whitespace.
            // Use the sync api.
            wrapper.DiscoverTests(new[] { dllpath }, "", discoveryHandler);

            var tests = discoveryHandler.DiscoveredTests;
            Console.WriteLine($"Found {tests.Count} tests.");

            
            var half = tests.Count / 2;

            // split them to two batches
            var firstHalf = tests.Take(half).ToList();
            var secondHalf = tests.Skip(half).ToList();

            var run1Handler = new RunHandler();
            var run2Handler = new RunHandler();

            // Running each batch
            // Make sure you provide provide at least the root tag for the runsettings.
            wrapper.RunTests(firstHalf, "<RunSettings></RunSettings>", run1Handler);
            Console.WriteLine("First half:");
            run1Handler.TestResults.ForEach(WriteTestResult);

            wrapper.RunTests(secondHalf, "<RunSettings></RunSettings>", run2Handler);
            Console.WriteLine("Second half:");
            run2Handler.TestResults.ForEach(WriteTestResult);

            // Trying it with async
            run1Handler.TestResults.Clear();
            run2Handler.TestResults.Clear();

            // Make sure you provide provide at least the root tag for the runsettings.
            var run1 = wrapper.RunTestsAsync(firstHalf, "<RunSettings></RunSettings>", run1Handler);
            // there is a bug that will report using one of the handlers when the requests come too close together
            // this won't happen for third request. BUT it should not matter to you if you use the same handler for all 
            // batches as it is usual.
            var run2 = wrapper.RunTestsAsync(secondHalf, "<RunSettings></RunSettings>", run2Handler);
            await Task.WhenAll(run1, run2);


            Console.WriteLine("First half async:");
            run1Handler.TestResults.ForEach(WriteTestResult);
            Console.WriteLine("Second half async:");
            run2Handler.TestResults.ForEach(WriteTestResult);

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void WriteTestResult(TestResult testResult)
        {
            Console.WriteLine($"  {testResult.Outcome}`t{testResult.TestCase.DisplayName }`n{testResult.ErrorMessage}");
        }

        static string RunCommand(string command, string arguments)
        {
            var sb = new StringBuilder();
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = false;
            process.Start();
            sb.AppendLine(process.StandardOutput.ReadToEnd());
            process.WaitForExit();
            return sb.ToString();
        }

    }

    internal class DiscoveryHandler : ITestDiscoveryEventsHandler
    {
        public List<TestCase> DiscoveredTests { get; } = new List<TestCase>();

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            DiscoveredTests.AddRange(discoveredTestCases);
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            if (lastChunk != null)
            {
                DiscoveredTests.AddRange(lastChunk);
            }
        }

        public void HandleLogMessage(TestMessageLevel level, string message) { }

        public void HandleRawMessage(string rawMessage) { }
    }

    internal class RunHandler : ITestRunEventsHandler
    {
        public bool Done { get; private set; }
        public List<TestResult> TestResults { get; } = new List<TestResult>();

        public void HandleLogMessage(TestMessageLevel level, string message) { }

        public void HandleRawMessage(string rawMessage) { }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, 
            TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, 
            ICollection<string> executorUris) { }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            TestResults.AddRange(testRunChangedArgs.NewTestResults);
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            return default;
        }
    }
}
