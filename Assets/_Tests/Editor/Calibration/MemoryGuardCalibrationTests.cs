using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpiderRig.Tests.Editor.Calibration
{
    [TestFixture]
    [Category("Calibration")]
    public class MemoryGuardCalibrationTests
    {
        private const int BlockBytes = 16 * 1024 * 1024;
        private const int MaxBlocks = 48;
        private readonly List<IntPtr> _buffers = new List<IntPtr>(MaxBlocks);
        private string _markersPath;

        [UnityTest, Explicit, Category("Manual")]
        public IEnumerator ObservePlayBaseline()
        {
            yield return new EnterPlayMode();
            StartMarkers();
            Mark("baseline-start");
            yield return WaitForSeconds(30);
            Mark("baseline-end");
            yield return new ExitPlayMode();
        }

        [UnityTest, Explicit, Category("Manual")]
        public IEnumerator GrowsMemoryUntilStopped()
        {
            yield return new EnterPlayMode();
            StartMarkers();
            yield return WaitForSeconds(20);
            Mark("load-start");
            Stopwatch timer = Stopwatch.StartNew();
            while (_buffers.Count < MaxBlocks && timer.Elapsed.TotalSeconds < 45 && EditorApplication.isPlaying)
            {
                AddBlock();
                yield return WaitForSeconds(0.5);
            }
            Mark("fallback");
            FreeBlocks();
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
            Assert.Fail("Memory calibration reached its bounded fallback; external Play stop was not demonstrated.");
        }

        [UnityTest, Explicit, Category("Manual")]
        public IEnumerator BusyMainThreadUntilFiniteDeadline()
        {
            yield return new EnterPlayMode();
            StartMarkers();
            yield return WaitForSeconds(20);
            Mark("busy-start");
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.Elapsed.TotalSeconds < 8)
            {
                if (_buffers.Count < MaxBlocks)
                    AddBlock();
            }
            Mark("busy-end");
            yield return WaitForSeconds(15);
            Mark("fallback");
            FreeBlocks();
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
            Assert.Fail("Memory calibration did not leave Play within busy-end + 15 seconds.");
        }

        [UnityTearDown]
        public IEnumerator ReleaseMemoryAndExitPlay()
        {
            FreeBlocks();
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        private void AddBlock()
        {
            IntPtr block = Marshal.AllocHGlobal(BlockBytes);
            _buffers.Add(block);
            for (int i = 0; i < BlockBytes; i += 4096)
                Marshal.WriteByte(block, i, 1);
        }

        private void FreeBlocks()
        {
            foreach (IntPtr block in _buffers)
                Marshal.FreeHGlobal(block);
            _buffers.Clear();
        }

        private static IEnumerator WaitForSeconds(double seconds)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.Elapsed.TotalSeconds < seconds && EditorApplication.isPlaying)
                yield return null;
        }

        private void StartMarkers()
        {
            string directory = Path.Combine(Application.dataPath, "..", "Logs", "MemoryStress",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            _markersPath = Path.Combine(directory, "markers.jsonl");
        }

        private void Mark(string phase)
        {
            Marker marker = new Marker
            {
                Phase = phase,
                Utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                FrameCount = Time.frameCount,
                BytesHeld = (long)_buffers.Count * BlockBytes
            };
            File.AppendAllText(_markersPath, JsonUtility.ToJson(marker) + Environment.NewLine);
        }

        [Serializable]
        private sealed class Marker
        {
            public string Phase;
            public string Utc;
            public int FrameCount;
            public long BytesHeld;
        }
    }
}
