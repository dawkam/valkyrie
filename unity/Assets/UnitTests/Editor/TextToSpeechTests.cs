using NUnit.Framework;

namespace Valkyrie.UnitTests
{
    [TestFixture]
    public class TextToSpeechTests
    {
        [Test]
        public void ToSpeechText_NullOrEmpty_ReturnsEmpty()
        {
            Assert.AreEqual("", TextToSpeech.ToSpeechText(null));
            Assert.AreEqual("", TextToSpeech.ToSpeechText(""));
        }

        [Test]
        public void ToSpeechText_PlainText_IsUnchanged()
        {
            Assert.AreEqual("The door creaks open.", TextToSpeech.ToSpeechText("The door creaks open."));
        }

        [Test]
        public void ToSpeechText_RemovesRichTextTags()
        {
            Assert.AreEqual("A dark room.", TextToSpeech.ToSpeechText("<b>A</b> <color=#FF0000>dark</color> room."));
        }

        [Test]
        public void ToSpeechText_ReplacesNamedSymbolsWithTheirName()
        {
            Assert.AreEqual("Test will to resist.", TextToSpeech.ToSpeechText("Test {will} to resist."));
        }

        [Test]
        public void ToSpeechText_RemovesOtherMarkers()
        {
            Assert.AreEqual("Use the item.", TextToSpeech.ToSpeechText("Use {MAD01} the {var:item} item."));
        }

        [Test]
        public void ToSpeechText_JoinsLinesAndCollapsesWhitespace()
        {
            Assert.AreEqual("First line. Second line. Third line.", TextToSpeech.ToSpeechText("  First line.\nSecond line.\\nThird   line.  "));
        }

        [Test]
        public void GetLanguageCode_KnownLanguage_ReturnsIsoCode()
        {
            Assert.AreEqual("pl", TextToSpeech.GetLanguageCode("Polish"));
            Assert.AreEqual("de", TextToSpeech.GetLanguageCode("German"));
            Assert.AreEqual("uk", TextToSpeech.GetLanguageCode("Ukrainian"));
        }

        [Test]
        public void BuildSpeakCommand_EncodesSsmlAsBase64AndUsesIdAndLanguageCode()
        {
            string ssml = "<speak>Zażółć 'gęślą' jaźń; $evil</speak>";
            string command = TextToSpeech.BuildSpeakCommand(42, "pl", ssml);

            StringAssert.Contains(System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ssml)), command);
            StringAssert.Contains("StartsWith('pl')", command);
            StringAssert.Contains("'SPEECH 42 '", command);
            StringAssert.Contains("'ERROR 42 '", command);
            StringAssert.DoesNotContain(ssml, command);
            StringAssert.DoesNotContain("\n", command);
            StringAssert.DoesNotContain("__", command.Replace("__VOICE_LANGUAGE__", ""));
        }

        [Test]
        public void BuildSsml_AddsPausesBetweenSentencesAndEscapesXml()
        {
            string ssml = TextToSpeech.BuildSsml("The door opens. Something <moves> & waits! Run?");

            StringAssert.StartsWith("<speak ", ssml);
            StringAssert.EndsWith("</speak>", ssml);
            StringAssert.Contains("The door opens.<break time='600ms'/>Something &lt;moves&gt; &amp; waits!<break time='600ms'/>Run?", ssml);
        }

        [Test]
        public void SplitSentences_SplitsAfterSentenceEndings()
        {
            Assert.AreEqual(new[] { "The door opens.", "Something waits!", "Run?Now…", "Go" }, TextToSpeech.SplitSentences("The door opens. Something waits! Run?Now… Go"));
        }

        [Test]
        public void TryReadWav_Pcm16WithExtraChunks_ReturnsSamples()
        {
            byte[] wav = BuildWav(new short[] { 0, 16384, -32768, 32767 }, 2, 16000, 18);

            Assert.IsTrue(TextToSpeech.TryReadWav(wav, out float[] samples, out int channels, out int sampleRate));
            Assert.AreEqual(2, channels);
            Assert.AreEqual(16000, sampleRate);
            Assert.AreEqual(new[] { 0f, 0.5f, -1f, 32767f / 32768f }, samples);
        }

        [Test]
        public void TryReadWav_InvalidData_ReturnsFalse()
        {
            Assert.IsFalse(TextToSpeech.TryReadWav(null, out _, out _, out _));
            Assert.IsFalse(TextToSpeech.TryReadWav(new byte[] { 1, 2, 3 }, out _, out _, out _));
            Assert.IsFalse(TextToSpeech.TryReadWav(System.Text.Encoding.ASCII.GetBytes("RIFF0000WAVEjunk"), out _, out _, out _));
        }

        private static byte[] BuildWav(short[] samples, short channels, int sampleRate, int fmtSize)
        {
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(0);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(fmtSize);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(new byte[fmtSize - 16]);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("LIST"));
                writer.Write(3);
                writer.Write(new byte[] { 1, 2, 3, 0 });
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2);
                foreach (short sample in samples)
                {
                    writer.Write(sample);
                }
                return stream.ToArray();
            }
        }

        [Test]
        public void GetLanguageCode_UnknownOrNullLanguage_ReturnsEnglish()
        {
            Assert.AreEqual("en", TextToSpeech.GetLanguageCode("Klingon"));
            Assert.AreEqual("en", TextToSpeech.GetLanguageCode(null));
        }
    }
}
