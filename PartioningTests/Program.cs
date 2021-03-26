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
            Console.WriteLine("Running dotnet --version");
            var dotnetVersion = "3.1.408"; // RunCommand("dotnet", "--version").Trim();
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

            var vstestConsolePath = @"C:\p\vstest\artifacts\Debug\netcoreapp2.1\vstest.console.dll"; // Path.Combine(sdkPath, dotnetVersion, "vstest.console.dll");
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
            // and the runner won't start
            Environment.SetEnvironmentVariable("VSTEST_CONNECTION_TIMEOUT", "20");
            var wrapper = new VsTestConsoleWrapper(vstestConsolePath, consoleParameters);

            Console.WriteLine($"Discovering tests.");
            var discoveryHandler = new DiscoveryHandler();

            if (!File.Exists(@"C:\t\PartioningTests\TestProject1\bin\Debug\netcoreapp3.1\TestProject1.deps.json")) {
                throw new Exception("file does not exist!");
            }

            // Make sure you don't provide null for the runsettings.
            // Make sure your dll paths are not sorrounded by whitespace.
            // Use the sync api.
            wrapper.DiscoverTests(new[] { dllpath }, "", discoveryHandler);

            var tests = discoveryHandler.DiscoveredTests;
            Console.WriteLine($"Found {tests.Count} tests.");


            var half = tests.Count / 2;

            var firstHalf = tests.Take(half).ToList();
            var secondHalf = tests.Skip(half).Take(1).ToList();

            // rem
            var thirdHalf = tests.Skip(half + 1).Take(1).ToList();


            var run1Handler = new RunHandler();
            var run2Handler = new RunHandler();
            
            // rem
            var run3Handler = new RunHandler();

            // Make sure you provide provide at least the root tag for the runsettings.
            wrapper.RunTests(firstHalf, "<RunSettings></RunSettings>", run1Handler);
            Console.WriteLine("First half:");
            run1Handler.TestResults.ForEach(WriteTestResult);

            wrapper.RunTests(secondHalf, "<RunSettings></RunSettings>", run2Handler);
            Console.WriteLine("Second half:");
            run2Handler.TestResults.ForEach(WriteTestResult);


            run1Handler.TestResults.Clear();
            run2Handler.TestResults.Clear();
            //rem
            run3Handler.TestResults.Clear();


            // Make sure you provide provide at least the root tag for the runsettings.
            var t1 = Task.Run(() => wrapper.RunTests(firstHalf, "<RunSettings></RunSettings>", run1Handler));
            var t2 = Task.Run(() => wrapper.RunTests(secondHalf, "<RunSettings></RunSettings>", run2Handler));
            var t3 = Task.Run(() => wrapper.RunTests(thirdHalf, "<RunSettings></RunSettings>", run3Handler));

            Task.WaitAll(t1, t2, t3);
            Console.WriteLine("task non-async but async");
            Console.WriteLine("First half:");
            run1Handler.TestResults.ForEach(WriteTestResult);
            Console.WriteLine("Second half:");
            run2Handler.TestResults.ForEach(WriteTestResult);
            Console.WriteLine("third half async:");
            run3Handler.TestResults.ForEach(WriteTestResult);

            run1Handler.TestResults.Clear();
            run2Handler.TestResults.Clear();
            //rem
            run3Handler.TestResults.Clear();

            Console.WriteLine(Stopwatch.GetTimestamp());
            var run1 = wrapper.RunTestsAsync(firstHalf, "<RunSettings></RunSettings>", run1Handler);
            // there is debounce for those requests, so the second will get merged with the first one comes 
            // fast enough after the first one
            var run2 = wrapper.RunTestsAsync(secondHalf, "<RunSettings></RunSettings>", run2Handler);
            await Task.WhenAll(run1, run2);
            var run3 = wrapper.RunTestsAsync(secondHalf, "<RunSettings></RunSettings>", run3Handler);
            await Task.WhenAll(run1, run2, run3);


            Console.WriteLine("First half async:");
            run1Handler.TestResults.ForEach(WriteTestResult);
            Console.WriteLine("Second half async:");
            run2Handler.TestResults.ForEach(WriteTestResult);

            //rem
            Console.WriteLine("third half async:");
            run3Handler.TestResults.ForEach(WriteTestResult);

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
