using NUnit.Framework;

namespace Valkyrie.UnitTests
{
    [TestFixture]
    public class PiperSpeechTests
    {
        [Test]
        public void JsonEscape_EscapesQuotesBackslashesAndControlCharacters()
        {
            Assert.AreEqual("say \\\"hi\\\" C:\\\\dir\\u0009tab\\u000a", PiperSpeech.JsonEscape("say \"hi\" C:\\dir\ttab\n"));
        }

        [Test]
        public void JsonEscape_KeepsNonAsciiText()
        {
            Assert.AreEqual("Zażółć gęślą jaźń", PiperSpeech.JsonEscape("Zażółć gęślą jaźń"));
        }

        [Test]
        public void BuildRequest_CreatesJsonLineWithTextAndOutputFile()
        {
            string request = PiperSpeech.BuildRequest("The \"door\" opens.", @"C:\Temp\speech_3.wav");

            Assert.AreEqual("{\"text\":\"The \\\"door\\\" opens.\",\"output_file\":\"C:\\\\Temp\\\\speech_3.wav\"}", request);
        }
    }
}
