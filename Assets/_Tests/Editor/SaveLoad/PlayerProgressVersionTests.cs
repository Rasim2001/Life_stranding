using System.Reflection;
using System.Text.RegularExpressions;
using Infastructure.Data;
using Infastructure.Services.SaveLoadService;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpiderRig.Tests.Editor.SaveLoad
{
    [TestFixture]
    public class PlayerProgressVersionTests
    {
        [Test]
        public void OldSave_WithoutVersion_ReadsZeroAndWarnsOnce()
        {
            FindMembers(out FieldInfo versionField, out int currentVersion, out _, out MethodInfo deserializeProgress);
            const string json = "{\"WorldProgressData\":{},\"TaskPopupData\":{},\"AbilityData\":{}}";

            LogAssert.Expect(LogType.Warning, VersionWarning(0, currentVersion));
            PlayerProgress progress = (PlayerProgress)deserializeProgress.Invoke(null, new object[] { json });
            LogAssert.NoUnexpectedReceived();

            Assert.IsNotNull(progress);
            Assert.AreEqual(0, (int)versionField.GetValue(progress));
        }

        [Test]
        public void CurrentSave_RoundTrip_HasVersionOneAndNoLog()
        {
            FindMembers(out FieldInfo versionField, out int currentVersion, out MethodInfo serializeProgress, out MethodInfo deserializeProgress);
            PlayerProgress progress = new PlayerProgress();

            string json = (string)serializeProgress.Invoke(null, new object[] { progress });
            PlayerProgress restoredProgress = (PlayerProgress)deserializeProgress.Invoke(null, new object[] { json });
            LogAssert.NoUnexpectedReceived();

            Assert.IsNotNull(json);
            StringAssert.IsMatch("\"Version\":\\s*1\\b", json);
            Assert.IsNotNull(restoredProgress);
            Assert.AreEqual(currentVersion, (int)versionField.GetValue(restoredProgress));
        }

        [Test]
        public void FutureSave_Version2_WarnsAndKeepsData()
        {
            FindMembers(out FieldInfo versionField, out int currentVersion, out _, out MethodInfo deserializeProgress);
            const string json = "{\"Version\":2,\"WorldProgressData\":{\"EnergyDatas\":[{\"UniqueId\":\"energy-01\"}]},\"TaskPopupData\":{},\"AbilityData\":{}}";

            LogAssert.Expect(LogType.Warning, VersionWarning(2, currentVersion));
            PlayerProgress progress = (PlayerProgress)deserializeProgress.Invoke(null, new object[] { json });
            LogAssert.NoUnexpectedReceived();

            Assert.IsNotNull(progress);
            Assert.AreEqual(2, (int)versionField.GetValue(progress));
            Assert.IsNotNull(progress.WorldProgressData);
            Assert.IsNotNull(progress.WorldProgressData.EnergyDatas);
            Assert.AreEqual(1, progress.WorldProgressData.EnergyDatas.Count);
            EnergyData energy = progress.WorldProgressData.EnergyDatas[0];
            Assert.IsNotNull(energy);
            Assert.AreEqual("energy-01", energy.UniqueId);
        }

        [Test]
        public void OldSave_KeepsEnergyData()
        {
            FindMembers(out _, out int currentVersion, out _, out MethodInfo deserializeProgress);
            const string json = "{\"WorldProgressData\":{\"EnergyDatas\":[{\"UniqueId\":\"energy-01\"}]},\"TaskPopupData\":{},\"AbilityData\":{}}";

            LogAssert.Expect(LogType.Warning, VersionWarning(0, currentVersion));
            PlayerProgress progress = (PlayerProgress)deserializeProgress.Invoke(null, new object[] { json });
            LogAssert.NoUnexpectedReceived();

            Assert.IsNotNull(progress);
            Assert.IsNotNull(progress.WorldProgressData);
            Assert.IsNotNull(progress.WorldProgressData.EnergyDatas);
            Assert.AreEqual(1, progress.WorldProgressData.EnergyDatas.Count);
            EnergyData energy = progress.WorldProgressData.EnergyDatas[0];
            Assert.IsNotNull(energy);
            Assert.AreEqual("energy-01", energy.UniqueId);
        }

        private static void FindMembers(out FieldInfo versionField, out int currentVersion,
            out MethodInfo serializeProgress, out MethodInfo deserializeProgress)
        {
            versionField = typeof(PlayerProgress).GetField("Version", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(versionField, "член не найден: PlayerProgress.Version");

            FieldInfo currentVersionField = typeof(PlayerProgress).GetField("CurrentVersion", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(currentVersionField, "член не найден: PlayerProgress.CurrentVersion");

            serializeProgress = typeof(SaveLoadService).GetMethod("SerializeProgress",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(PlayerProgress) }, null);
            Assert.IsNotNull(serializeProgress, "член не найден: SaveLoadService.SerializeProgress");

            deserializeProgress = typeof(SaveLoadService).GetMethod("DeserializeProgress",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            Assert.IsNotNull(deserializeProgress, "член не найден: SaveLoadService.DeserializeProgress");

            currentVersion = (int)currentVersionField.GetValue(null);
        }

        private static Regex VersionWarning(int fileVersion, int currentVersion) =>
            new Regex($@"(?s)(?=.*\b{fileVersion}\b)(?=.*\b{currentVersion}\b).*");
    }
}
