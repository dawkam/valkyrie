using NUnit.Framework;
using System.Collections.Generic;

namespace Valkyrie.UnitTests
{
    /// <summary>
    /// Unit tests for Mansions of Madness round logic: saved phase parsing,
    /// event activation detection and horror/evade lookup by monster type.
    /// </summary>
    [TestFixture]
    public class MoMRoundTests
    {
        #region Quest.ParseSavedPhase

        [TestCase("investigator", Quest.MoMPhase.investigator)]
        [TestCase("mythos", Quest.MoMPhase.mythos)]
        [TestCase("monsters", Quest.MoMPhase.monsters)]
        [TestCase("horror", Quest.MoMPhase.horror)]
        public void ParseSavedPhase_StoredPhase_ReturnsIt(string saved, Quest.MoMPhase expected)
        {
            Assert.AreEqual(expected, Quest.ParseSavedPhase(saved, "false"));
        }

        [Test]
        public void ParseSavedPhase_StoredPhase_TakesPrecedenceOverHorrorFlag()
        {
            Assert.AreEqual(Quest.MoMPhase.mythos, Quest.ParseSavedPhase("mythos", "true"));
        }

        [Test]
        public void ParseSavedPhase_LegacySaveWithHorror_ReturnsHorror()
        {
            Assert.AreEqual(Quest.MoMPhase.horror, Quest.ParseSavedPhase(null, "true"));
        }

        [Test]
        public void ParseSavedPhase_LegacySaveWithoutHorror_ReturnsInvestigator()
        {
            Assert.AreEqual(Quest.MoMPhase.investigator, Quest.ParseSavedPhase(null, "false"));
        }

        [Test]
        public void ParseSavedPhase_NothingStored_ReturnsInvestigator()
        {
            Assert.AreEqual(Quest.MoMPhase.investigator, Quest.ParseSavedPhase(null, null));
        }

        [TestCase("garbage")]
        [TestCase("")]
        [TestCase("7")]
        public void ParseSavedPhase_InvalidPhase_FallsBackToHorrorFlag(string saved)
        {
            Assert.AreEqual(Quest.MoMPhase.horror, Quest.ParseSavedPhase(saved, "true"));
            Assert.AreEqual(Quest.MoMPhase.investigator, Quest.ParseSavedPhase(saved, "false"));
        }

        #endregion

        #region RoundControllerMoM.IsEventActivation

        [Test]
        public void IsEventActivation_SingleEvent_ReturnsTrue()
        {
            Assert.IsTrue(RoundControllerMoM.IsEventActivation(new[] { "EventMonsterActivation" }));
        }

        [Test]
        public void IsEventActivation_Null_ReturnsFalse()
        {
            Assert.IsFalse(RoundControllerMoM.IsEventActivation(null));
        }

        [Test]
        public void IsEventActivation_Empty_ReturnsFalse()
        {
            Assert.IsFalse(RoundControllerMoM.IsEventActivation(new string[0]));
        }

        [Test]
        public void IsEventActivation_MultipleEntries_ReturnsFalse()
        {
            Assert.IsFalse(RoundControllerMoM.IsEventActivation(new[] { "EventA", "EventB" }));
        }

        [TestCase("MonsterActivationCultist")]
        [TestCase("event1")]
        [TestCase("MyEvent")]
        public void IsEventActivation_NotPrefixedWithEvent_ReturnsFalse(string activation)
        {
            Assert.IsFalse(RoundControllerMoM.IsEventActivation(new[] { activation }));
        }

        #endregion

        #region MonsterData.FindForMonster

        private static readonly List<string> MonsterNames = new List<string> { "Cultist", "Riot", "Cultist", "Deep One" };

        [Test]
        public void FindForMonster_DirectMatch_ReturnsAllMatches()
        {
            List<string> result = MonsterData.FindForMonster(MonsterNames, n => n, "MonsterCultist", null);

            Assert.AreEqual(2, result.Count);
            Assert.That(result, Is.All.EqualTo("Cultist"));
        }

        [Test]
        public void FindForMonster_NoDirectMatch_UsesDerivedType()
        {
            List<string> result = MonsterData.FindForMonster(MonsterNames, n => n, "CustomMonsterBoss", "MonsterRiot");

            CollectionAssert.AreEqual(new[] { "Riot" }, result);
        }

        [Test]
        public void FindForMonster_DirectMatch_IgnoresDerivedType()
        {
            List<string> result = MonsterData.FindForMonster(MonsterNames, n => n, "MonsterDeep One", "MonsterRiot");

            CollectionAssert.AreEqual(new[] { "Deep One" }, result);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void FindForMonster_NoMatchAndNoDerivedType_ReturnsEmpty(string derivedType)
        {
            List<string> result = MonsterData.FindForMonster(MonsterNames, n => n, "CustomMonsterBoss", derivedType);

            Assert.IsEmpty(result);
        }

        [Test]
        public void FindForMonster_EmptyData_ReturnsEmpty()
        {
            List<string> result = MonsterData.FindForMonster(new List<string>(), n => n, "MonsterCultist", "MonsterRiot");

            Assert.IsEmpty(result);
        }

        #endregion
    }
}
