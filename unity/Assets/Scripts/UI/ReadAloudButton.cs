using Assets.Scripts.Content;

namespace Assets.Scripts.UI
{
    // Button that reads the given text aloud again, only shown when reading aloud is enabled
    public static class ReadAloudButton
    {
        public const float WIDTH = 6f;

        private static readonly StringKey READ_ALOUD = new StringKey("val", "READ_ALOUD");

        public static void Draw(float x, float y, string text, string tag = "")
        {
            if (!TextToSpeech.IsEnabled() || TextToSpeech.ToSpeechText(text).Length == 0) return;

            UIElement ui = new UIElement(tag);
            ui.SetLocation(x, y, WIDTH, 2);
            ui.SetText(READ_ALOUD);
            ui.SetFontSize(UIScaler.GetSmallFont());
            ui.SetButton(delegate { TextToSpeech.Speak(text); });
            new UIElementBorder(ui);
        }
    }
}
