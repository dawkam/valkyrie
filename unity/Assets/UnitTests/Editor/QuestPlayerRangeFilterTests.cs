using NUnit.Framework;
using Assets.Scripts.Content;

namespace Valkyrie.UnitTests
{
    /// <summary>
    /// Unit tests for QuestData.Quest.SupportsPlayerCount used by the scenario list player filter.
    /// A scenario matches when the chosen player count is within its min/max players.
    /// </summary>
    [TestFixture]
    public class QuestPlayerRangeFilterTests
    {
        [TestCase(1, 5)]
        [TestCase(3, 3)]
        [TestCase(10, 10)]
        public void SupportsPlayerCount_FilterDisabled_AlwaysMatches(int questMin, int questMax)
        {
            Assert.IsTrue(QuestData.Quest.SupportsPlayerCount(questMin, questMax, 0));
        }

        [TestCase(1, 5)]
        [TestCase(3, 3)]
        [TestCase(3, 5)]
        [TestCase(1, 3)]
        public void SupportsPlayerCount_CountWithinQuestRange_Matches(int questMin, int questMax)
        {
            Assert.IsTrue(QuestData.Quest.SupportsPlayerCount(questMin, questMax, 3));
        }

        [TestCase(4, 5)]
        [TestCase(1, 2)]
        public void SupportsPlayerCount_CountOutsideQuestRange_DoesNotMatch(int questMin, int questMax)
        {
            Assert.IsFalse(QuestData.Quest.SupportsPlayerCount(questMin, questMax, 3));
        }
    }
}
