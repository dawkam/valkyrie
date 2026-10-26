using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.Content;
using Assets.Scripts.UI;

// In quest volume settings, opened from the game menu
public class GameOptionsMenu
{
    private static readonly StringKey OPTIONS = new StringKey("val", "OPTIONS");
    private static readonly StringKey MUSIC = new StringKey("val", "MUSIC");
    private static readonly StringKey EFFECTS = new StringKey("val", "EFFECTS");

    private const float WIDTH = 16;
    private const float SLIDER_INSET = 0.3f;

    public static void Draw()
    {
        Game game = Game.Get();
        float x = (UIScaler.GetWidthUnits() - WIDTH) / 2;

        UIElement ui = new UIElement();
        ui.SetLocation(x, 5, WIDTH, 17);
        new UIElementBorder(ui);

        ui = new UIElement();
        ui.SetLocation(x, 5.5f, WIDTH, 2);
        ui.SetText(OPTIONS);
        ui.SetFont(game.gameType.GetHeaderFont());
        ui.SetFontSize(UIScaler.GetLargeFont());
        ui.SetPaperLabel();

        DrawVolumeSlider(MUSIC, x + 1, 8, game.audioControl.musicVolume, SetMusicVolume, false);
        DrawVolumeSlider(EFFECTS, x + 1, 12.5f, game.audioControl.effectVolume, SetEffectVolume, true);

        ui = new UIElement();
        ui.SetLocation((UIScaler.GetWidthUnits() - 10) / 2, 18.5f, 10, 2);
        ui.SetText(CommonStringKeys.BACK);
        ui.SetBGColor(new Color(0.03f, 0.0f, 0f));
        ui.SetFontSize(UIScaler.GetMediumFont());
        ui.SetFont(game.gameType.GetHeaderFont());
        ui.SetButton(delegate { Destroyer.Dialog(); GameMenu.Draw(); });
        new UIElementBorder(ui);
    }

    private static void DrawVolumeSlider(StringKey label, float x, float y, float volume, UnityEngine.Events.UnityAction<float> onChange, bool playTestOnRelease)
    {
        Game game = Game.Get();
        float width = WIDTH - 2;

        UIElement ui = new UIElement();
        ui.SetLocation(x, y, width, 1.5f);
        ui.SetText(label);
        ui.SetFont(game.gameType.GetHeaderFont());
        ui.SetFontSize(UIScaler.GetMediumFont());
        ui.SetPaperLabel();

        ui = new UIElement();
        ui.SetLocation(x, y + 1.5f, width, 2);
        new UIElementBorder(ui);

        GameObject sliderObj = new GameObject("volumeSlider");
        sliderObj.tag = Game.DIALOG;
        sliderObj.transform.SetParent(game.uICanvas.transform);
        // Transparent image makes the whole bar clickable, not only the filled part
        sliderObj.AddComponent<Image>().color = Color.clear;
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, (y + 1.5f + SLIDER_INSET) * UIScaler.GetPixelsPerUnit(), (2 - 2 * SLIDER_INSET) * UIScaler.GetPixelsPerUnit());
        sliderRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, (x + SLIDER_INSET) * UIScaler.GetPixelsPerUnit(), (width - 2 * SLIDER_INSET) * UIScaler.GetPixelsPerUnit());

        GameObject fill = new GameObject("volumeFill");
        fill.tag = Game.DIALOG;
        fill.transform.SetParent(sliderObj.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = UIElement.paperTextColor;
        fillImage.raycastTarget = false;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.fillRect.offsetMin = Vector2.zero;
        slider.fillRect.offsetMax = Vector2.zero;

        slider.value = volume;
        slider.onValueChanged.AddListener(onChange);

        if (playTestOnRelease)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerUp;
            entry.callback.AddListener(delegate { game.audioControl.PlayTest(); });
            sliderObj.AddComponent<EventTrigger>().triggers.Add(entry);
        }
    }

    private static void SetMusicVolume(float volume)
    {
        Game game = Game.Get();
        game.config.data.Add("UserConfig", "music", volume.ToString());
        game.config.Save();
        game.audioControl.audioSource.volume = volume;
        game.audioControl.musicVolume = volume;
    }

    private static void SetEffectVolume(float volume)
    {
        Game game = Game.Get();
        game.config.data.Add("UserConfig", "effects", volume.ToString());
        game.config.Save();
        game.audioControl.effectVolume = volume;
    }
}
