using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static UnitTestProject14.DebuggerUtil;
using DiagnosticsProcess = System.Diagnostics.Process;

namespace UnitTestProject14
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // To try this out run 
            // $env:TEST_DEBUG = $true; $env:TEST_NAME = "TestMethod1|TestMethod2"; &"<path>\vstest.console.exe" <path>\UnitTestProject14.dll"
            // in Package Manager console here in VS. It should attach debugger when the assertion in this test fails, and you shoul be able to 
            // inspect callstack with locals and autos.

            var a = 10;

            // This needs to use Assert.That.AreEqual, you can "enforce" this on all MSTests simply by 
            // regex replacing all Assert. that are not followed by That. with Assert.That.
            // Find        : Assert\.(?!That\.)
            // Replace with: Assert.That.
            // Assert.AreEqual(10, 11);
            Assert.That.AreEqual(a, 11);
        }

        [TestMethod]
        public void TestMethod2()
        {
            // or to catch any exception that happens in the test
            Run(() =>
            {
                var a = 10;

                // no .That here, it uses the standard non wrapped assertion
                Assert.AreEqual(a, 11);
            });
        }
    }

    public static class DebuggerUtil
    {
        private static bool _enabled;
        private static string[] _tests;
        private static string _vsVersion;
        private static bool _weAttached;

        static DebuggerUtil()
        {
            var debug = Environment.GetEnvironmentVariable("TEST_DEBUG");
            var debugEnabled = new[] { "YES", "TRUE", "1", "ON" }.Contains(debug?.Trim().ToUpperInvariant());

            var vsVersion = Environment.GetEnvironmentVariable("VisualStudioVersion");
            var inPackageManager = !string.IsNullOrWhiteSpace(vsVersion);


            _enabled = debugEnabled && inPackageManager;
            _vsVersion = vsVersion;
            // only break on those tests
            _tests = Environment.GetEnvironmentVariable("TEST_NAME")?.Split('|') ?? new string[0];
        }

        public static void Attach()
        {
            if (!_enabled)
                return;

            if (!_weAttached && System.Diagnostics.Debugger.IsAttached)
                return; // there is already a debugger

            if (_tests.Any())
            {
                var stop = false;
                var stack = new StackTrace();
                var frames = stack.GetFrames();

                foreach (var frame in frames)
                {
                    var methodName = frame.GetMethod().Name;
                    if (_tests.Any(t => methodName.Contains(t)))
                    {
                        stop = true;
                        break;
                    }
                }

                if (!stop)
                {
                    // there we test names and we did not match any
                    return;
                }
            }

            if (!System.Diagnostics.Debugger.IsAttached)
            {
                // grab the automation object and attach to debugger
                DTE2 dte = (DTE2)Marshal.GetActiveObject($"VisualStudio.DTE.{_vsVersion}");

                // attach to the current process 
                var id = DiagnosticsProcess.GetCurrentProcess().Id;
                var processes = dte.Debugger.LocalProcesses;
                var proc = processes.Cast<Process2>().SingleOrDefault(p => p.ProcessID == id);
                proc.Attach2();

                _weAttached = true;
            }

            // Break here. Use the Step Out (Shift + F11) to get into the test. 
            System.Diagnostics.Debugger.Break();
        }

        public static void Run(Action test)
        {
            try
            {
                test.Invoke();
            }
            catch (Exception ex)
            {
                Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
    }

    public static class AssertionExtensions
    {
        public static void AreEqual<T>(this Assert _, T expected, T actual, string message, params object[] parameters)
        {
            try
            {
                Assert.AreEqual<T>(expected, actual, message, parameters);
            }
            catch (Exception ex)
            {
                Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, object expected, object actual)
        {
            try
            {
                Assert.AreEqual(expected, actual);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, object expected, object actual, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, object expected, object actual, string message, params object[] parameters)
        {
            try
            {
                Assert.AreEqual(expected, actual, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, float expected, float actual, float delta)
        {
            try
            {
                Assert.AreEqual(expected, actual, delta);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, float expected, float actual, float delta, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual, delta, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, float expected, float actual, float delta, string message, params object[] parameters)
        {
            try
            {
                Assert.AreEqual(expected, actual, delta, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, double expected, double actual, double delta)
        {
            try
            {
                Assert.AreEqual(expected, actual,delta);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, double expected, double actual, double delta, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual,delta,message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, string expected, string actual, bool ignoreCase)
        {
            try
            {
                Assert.AreEqual(expected, actual, ignoreCase);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, string expected, string actual, bool ignoreCase, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual, ignoreCase, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, string expected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            try
            {
                Assert.AreEqual(expected, actual, ignoreCase,message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, string expected, string actual, bool ignoreCase, CultureInfo culture)
        {
            try
            {
                Assert.AreEqual(expected, actual, ignoreCase, culture);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, string expected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual, ignoreCase, culture, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, string expected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            try
            {
                Assert.AreEqual(expected, actual, ignoreCase, culture, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual<T>(this Assert _, T expected, T actual, string message)
        {
            try
            {
                Assert.AreEqual<T>(expected, actual, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual<T>(this Assert _, T expected, T actual)
        {
            try
            {
                Assert.AreEqual<T>(expected, actual);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreEqual(this Assert _, double expected, double actual, double delta, string message, params object[] parameters)
        {
            try
            {
                Assert.AreEqual(expected, actual, delta,message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, string notExpected, string actual, bool ignoreCase)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, ignoreCase);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, float notExpected, float actual, float delta, string message, params object[] parameters)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, delta, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, float notExpected, float actual, float delta, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, delta, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, float notExpected, float actual, float delta)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, delta);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, double notExpected, double actual, double delta, string message, params object[] parameters)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, delta, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, string notExpected, string actual, bool ignoreCase, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, ignoreCase, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, object notExpected, object actual, string message, params object[] parameters)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, double notExpected, double actual, double delta, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, delta, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, object notExpected, object actual, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, string notExpected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, ignoreCase,message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, string notExpected, string actual, bool ignoreCase, CultureInfo culture)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, ignoreCase, culture);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, ignoreCase, culture, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual<T>(this Assert _, T notExpected, T actual, string message, params object[] parameters)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual<T>(this Assert _, T notExpected, T actual, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual<T>(this Assert _, T notExpected, T actual)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, ignoreCase, culture, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, object notExpected, object actual)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotEqual(this Assert _, double notExpected, double actual, double delta)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, delta);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotSame(this Assert _, object notExpected, object actual, string message, params object[] parameters)
        {
            try
            {
                Assert.AreNotSame(notExpected, actual, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotSame(this Assert _, object notExpected, object actual, string message)
        {
            try
            {
                Assert.AreNotSame(notExpected, actual, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreNotSame(this Assert _, object notExpected, object actual)
        {
            try
            {
                Assert.AreNotSame(notExpected, actual);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreSame(this Assert _, object expected, object actual)
        {
            try
            {
                Assert.AreSame(expected, actual);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreSame(this Assert _, object expected, object actual, string message, params object[] parameters)
        {
            try
            {
                Assert.AreSame(expected, actual, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void AreSame(this Assert _, object expected, object actual, string message)
        {
            try
            {
                Assert.AreSame(expected, actual, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }

        public static void Fail(this Assert _, string message, params object[] parameters)
        {
            try
            {
                Assert.Fail(message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void Fail(this Assert _)
        {
            try
            {
                Assert.Fail();
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void Fail(this Assert _, string message)
        {
            try
            {
                Assert.Fail(message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void Inconclusive(this Assert _)
        {
            try
            {
                Assert.Inconclusive();
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void Inconclusive(this Assert _, string message, params object[] parameters)
        {
            try
            {
                Assert.Inconclusive(message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void Inconclusive(this Assert _, string message)
        {
            try
            {
                Assert.Inconclusive(message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsFalse(this Assert _, bool condition)
        {
            try
            {
                Assert.IsFalse(condition);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsFalse(this Assert _, bool condition, string message)
        {
            try
            {
                Assert.IsFalse(condition, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsFalse(this Assert _, bool condition, string message, params object[] parameters)
        {
            try
            {
                Assert.IsFalse(condition, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsInstanceOfType(this Assert _, object value, Type expectedType)
        {
            try
            {
                Assert.IsInstanceOfType(value, expectedType);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsInstanceOfType(this Assert _, object value, Type expectedType, string message)
        {
            try
            {
                Assert.IsInstanceOfType(value, expectedType, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsInstanceOfType(this Assert _, object value, Type expectedType, string message, params object[] parameters)
        {
            try
            {
                Assert.IsInstanceOfType(value, expectedType, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNotInstanceOfType(this Assert _, object value, Type wrongType)
        {
            try
            {
                Assert.IsNotInstanceOfType(value, wrongType);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNotInstanceOfType(this Assert _, object value, Type wrongType, string message, params object[] parameters)
        {
            try
            {
                Assert.IsNotInstanceOfType(value, wrongType, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNotInstanceOfType(this Assert _, object value, Type wrongType, string message)
        {
            try
            {
                Assert.IsNotInstanceOfType(value, wrongType, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNotNull(this Assert _, object value, string message)
        {
            try
            {
                Assert.IsNotNull(value,message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNotNull(this Assert _, object value, string message, params object[] parameters)
        {
            try
            {
                Assert.IsNotNull(value, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNotNull(this Assert _, object value)
        {
            try
            {
                Assert.IsNotNull(value);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNull(this Assert _, object value, string message)
        {
            try
            {
                Assert.IsNull(value, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNull(this Assert _, object value)
        {
            try
            {
                Assert.IsNull(value);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsNull(this Assert _, object value, string message, params object[] parameters)
        {
            try
            {
                Assert.IsNull(value, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsTrue(this Assert _, bool condition, string message)
        {
            try
            {
                Assert.IsTrue(condition, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsTrue(this Assert _, bool condition, string message, params object[] parameters)
        {
            try
            {
                Assert.IsTrue(condition, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static void IsTrue(this Assert _, bool condition)
        {
            try
            {
                Assert.IsTrue(condition);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        public static string ReplaceNullChars(this Assert _, string input)
        {
            return Assert.ReplaceNullChars(input);
        }
        public static T ThrowsException<T>(this Assert _, Action action) where T : Exception
        {
            try
            {
                return Assert.ThrowsException<T>(action);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }
        public static T ThrowsException<T>(this Assert _, Action action, string message) where T : Exception
        {
            try
            {
                return Assert.ThrowsException<T>(action, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }
        public static T ThrowsException<T>(this Assert _, Func<object> action) where T : Exception
        {
            try
            {
                return Assert.ThrowsException<T>(action);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }
        public static T ThrowsException<T>(this Assert _, Func<object> action, string message) where T : Exception
        {
            try
            {
                return Assert.ThrowsException<T>(action, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }
        public static T ThrowsException<T>(this Assert _, Func<object> action, string message, params object[] parameters) where T : Exception
        {
            try
            {
                return Assert.ThrowsException<T>(action, message,parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }
        public static T ThrowsException<T>(this Assert _, Action action, string message, params object[] parameters) where T : Exception
        {
            try
            {
                return Assert.ThrowsException<T>(action, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }

        public static Task<T> ThrowsExceptionAsync<T>(this Assert _, Func<Task> action) where T : Exception
        {
            try
            {
                return Assert.ThrowsExceptionAsync<T>(action);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }
        public static Task<T> ThrowsExceptionAsync<T>(this Assert _, Func<Task> action, string message) where T : Exception
        {
            try
            {
                return Assert.ThrowsExceptionAsync<T>(action, message);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }

        public static Task<T> ThrowsExceptionAsync<T>(this Assert _, Func<Task> action, string message, params object[] parameters) where T : Exception
        {
            try
            {
                return Assert.ThrowsExceptionAsync<T>(action, message, parameters);
            }
            catch (Exception ex)
            {
                DebuggerUtil.Attach();
                // throw and preserve stack trace
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            throw new Exception("This should be never reached");
        }
    }
}
