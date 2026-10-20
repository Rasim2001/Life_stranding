using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace SpiderRig.Tests.Editor.Calibration
{
    [TestFixture]
    [Category("Calibration")]
    public class RecorderCalibrationTests
    {
        [Test]
        public void Passes()
        {
            Assert.Pass();
        }

        [Test]
        [Explicit]
        [Category("Manual")]
        public void Fails_WithReadableMessage()
        {
            Assert.Fail("Calibration: заведомо падающий тест");
        }

        [UnityTest]
        [Explicit]
        [Category("Manual")]
        public IEnumerator EntersAndExitsPlayMode()
        {
            yield return new EnterPlayMode();
            Assert.IsTrue(EditorApplication.isPlaying);
            yield return new ExitPlayMode();
            Assert.IsFalse(EditorApplication.isPlaying);
        }
    }
}
