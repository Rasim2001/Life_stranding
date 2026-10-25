using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SpiderRig.Tests.Editor.SpiderStates
{
    [TestFixture]
    public class BaseGatesSourceTests
    {
        [Test]
        public void Grounded_Jump_HasNoSkillGate()
        {
            string source = ReadSource("Scripts/SpiderController/StateMachine/States/Ground/GroundedState.cs");

            StringAssert.DoesNotContain("ProductType.JumpSkillProduct", source,
                "гейт прыжка не снят: GroundedState");
            StringAssert.IsMatch(@"if \(InputService\.JumpPressed && Data\.CurrentEnergyFillAmount > 0 && !Data\.IsStandingUpAfterFalling\) ?\{? ?StateMachine\.SwitchState<JumpingState>\(\);", source,
                "ветка прыжка повреждена: GroundedState");
        }

        [Test]
        public void Running_FastRun_HasNoSkillGate()
        {
            string source = ReadSource("Scripts/SpiderController/StateMachine/States/Ground/RunningState.cs");

            StringAssert.DoesNotContain("ProductType.FastRunSkillProduct", source,
                "гейт ускорения не снят: RunningState");
            StringAssert.IsMatch(@"else if \(IsFastRunPressed\(\)\) StateMachine\.SwitchState<FastRunningState>\(\);", source,
                "ветка ускорения повреждена: RunningState");
        }

        [Test]
        public void Slowdown_FastRun_HasNoSkillGate()
        {
            string source = ReadSource("Scripts/SpiderController/StateMachine/States/Ground/SlowdownState.cs");

            StringAssert.DoesNotContain("ProductType.FastRunSkillProduct", source,
                "гейт ускорения не снят: SlowdownState");
            StringAssert.IsMatch(@"else if \(IsFastRunPressed\(\)\) StateMachine\.SwitchState<FastRunningState>\(\);", source,
                "ветка ускорения повреждена: SlowdownState");
        }

        [Test]
        public void OtherSkillGates_StayClosed()
        {
            string grounded = ReadSource("Scripts/SpiderController/StateMachine/States/Ground/GroundedState.cs");
            StringAssert.Contains("IsExploredAbility(ProductType.JerkSkillProduct)", grounded,
                "гейт снят лишним: GroundedState.cs ProductType.JerkSkillProduct");
            StringAssert.Contains("IsExploredAbility(ProductType.TerrainScanSkillProduct)", grounded,
                "гейт снят лишним: GroundedState.cs ProductType.TerrainScanSkillProduct");

            string airborn = ReadSource("Scripts/SpiderController/StateMachine/States/Airborn/AirbornState.cs");
            StringAssert.Contains("IsExploredAbility(ProductType.JerkSkillProduct)", airborn,
                "гейт снят лишним: AirbornState.cs ProductType.JerkSkillProduct");

            string magnet = ReadSource("Scripts/SpiderController/Magnet/MagnetSkill.cs");
            StringAssert.Contains("IsExploredAbility(ProductType.MagnetSkillProduct)", magnet,
                "гейт снят лишним: MagnetSkill.cs ProductType.MagnetSkillProduct");

            string freezing = ReadSource("Scripts/Infastructure/Services/Magnet/MagnetFreezingService.cs");
            Assert.AreEqual(2, Regex.Matches(freezing,
                    Regex.Escape("IsExploredAbility(ProductType.MagnetSkillProduct)")).Count,
                "гейт снят лишним: MagnetFreezingService.cs ProductType.MagnetSkillProduct");
        }

        private static string ReadSource(string relativePath)
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, relativePath));
            return Regex.Replace(text, @"\s+", " ");
        }
    }
}
