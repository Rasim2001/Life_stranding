using System;
using System.Collections.Generic;

namespace SpiderRig.Editor.TestRunRecorder
{
    // Field names follow the record.json schema used by JsonUtility.
    [Serializable]
    public class TestRunRecord
    {
        public int schema = 1;
        public string status;
        public string startedAt;
        public double durationSec;
        public Summary summary = new Summary();
        public List<TestLine> tests = new List<TestLine>();
        public List<LogLine> consoleErrors = new List<LogLine>();
        public List<string> compileErrors = new List<string>();
        public string runError;
    }

    [Serializable]
    public class TestLine
    {
        public string fullName;
        public string status;
        public string message;
        public double durationSec;
    }

    [Serializable]
    public class LogLine
    {
        public string type;
        public string message;
        public string stack;
    }

    [Serializable]
    public class Summary
    {
        public int total;
        public int passed;
        public int failed;
        public int skipped;
        public int inconclusive;
    }
}
