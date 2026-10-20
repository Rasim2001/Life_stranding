using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SpiderRig.Editor.TestRunRecorder
{
    [InitializeOnLoad]
    public static class TestRunRecorder
    {
        private static readonly Callbacks _callbacks = new Callbacks();

        static TestRunRecorder()
        {
            TestRunnerApi.RegisterTestCallback(_callbacks);
            Application.logMessageReceived += OnLogMessage;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        }

        private static void BeforeAssemblyReload()
        {
            try
            {
                TestRunnerApi.UnregisterTestCallback(_callbacks);
                Application.logMessageReceived -= OnLogMessage;
                CompilationPipeline.compilationStarted -= OnCompilationStarted;
                CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
                AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // shortcut: logs during domain reload are missed; add an external sink if calibration requires them.
        // shortcut: runs before the recorder's first successful compilation are missed; compile before running tests.
        private sealed class State : ScriptableSingleton<State>
        {
            [SerializeField] public bool Recording;
            [SerializeField] public bool CompilationBlocked;
            [SerializeField] public string RecordPath;
            [SerializeField] public string StartedAt;
            [SerializeField] public List<LogLine> ConsoleErrors = new List<LogLine>();
            [SerializeField] public List<string> CompileErrors = new List<string>();
        }

        private static void StartRecording()
        {
            State state = State.instance;
            string directory = Path.Combine(Application.dataPath, "..", "Logs", "TestRuns",
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directory);
            state.RecordPath = Path.Combine(directory, "record.json");
            state.StartedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            state.ConsoleErrors.Clear();
            state.CompilationBlocked = EditorUtility.scriptCompilationFailed;
            state.Recording = true;
            WriteRecord(CreateRecord("running"));
        }

        private static TestRunRecord CreateRecord(string status)
        {
            State state = State.instance;
            return new TestRunRecord
            {
                status = status,
                startedAt = state.StartedAt,
                consoleErrors = state.ConsoleErrors
            };
        }

        private static void WriteRecord(TestRunRecord record)
        {
            string path = State.instance.RecordPath;
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(record, true));
            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            try
            {
                State state = State.instance;
                if (!state.Recording || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert))
                    return;

                // shortcut: background-thread errors are missed; add a synchronized buffer if they must be recorded.
                string[] stackLines = stackTrace.Split('\n');
                state.ConsoleErrors.Add(new LogLine
                {
                    type = type.ToString(),
                    message = message,
                    stack = string.Join("\n", stackLines, 0, Math.Min(5, stackLines.Length))
                });
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void OnCompilationStarted(object context)
        {
            try
            {
                State.instance.CompileErrors.Clear();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            try
            {
                foreach (CompilerMessage message in messages)
                {
                    if (message.type == CompilerMessageType.Error)
                        State.instance.CompileErrors.Add($"{message.file}({message.line}): {message.message}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void CollectTests(ITestResultAdaptor result, List<TestLine> tests)
        {
            if (!result.Test.IsSuite)
            {
                tests.Add(new TestLine
                {
                    fullName = result.Test.FullName,
                    status = result.TestStatus.ToString(),
                    message = result.Message,
                    durationSec = result.Duration
                });
                return;
            }

            foreach (ITestResultAdaptor child in result.Children)
                CollectTests(child, tests);
        }

        private sealed class Callbacks : IErrorCallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                try
                {
                    StartRecording();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    State state = State.instance;
                    if (!state.Recording)
                        return;

                    TestRunRecord record = CreateRecord("finished");
                    CollectTests(result, record.tests);
                    record.durationSec = (DateTime.UtcNow - DateTime.Parse(state.StartedAt,
                        CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)).TotalSeconds;
                    record.summary = new Summary
                    {
                        total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount,
                        passed = result.PassCount,
                        failed = result.FailCount,
                        skipped = result.SkipCount,
                        inconclusive = result.InconclusiveCount
                    };
                    if (state.CompilationBlocked || EditorUtility.scriptCompilationFailed)
                    {
                        record.status = "blocked";
                        record.compileErrors = state.CompileErrors;
                    }
                    else if (record.tests.Count == 0)
                    {
                        record.status = "empty";
                    }

                    WriteRecord(record);
                    state.Recording = false;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            public void OnError(string message)
            {
                try
                {
                    if (!State.instance.Recording)
                        StartRecording();

                    TestRunRecord record = CreateRecord("error");
                    record.runError = message;
                    record.compileErrors = State.instance.CompileErrors;
                    WriteRecord(record);
                    State.instance.Recording = false;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
