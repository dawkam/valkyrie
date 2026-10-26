using UnityEngine;
using Assets.Scripts.Content;
using Assets.Scripts.UI;
using ValkyrieTools;

// Monster information dialog (additional rules)
public class InfoDialog
{

    public InfoDialog(Quest.Monster m)
    {
        if (m == null)
        {
            ValkyrieDebug.Log("Warning: Invalid monster type requested.");
            return;
        }

        // box with monster info
        UIElement ui = new UIElement();
        ui.SetLocation(10, 0.5f, UIScaler.GetWidthUnits() - 20, 12);


        string monsterInfoSymbolsReplace = string.Empty;
        if (m.monsterData != null && m.monsterData.info != null)
        {
            monsterInfoSymbolsReplace = EventManager.OutputSymbolReplace(m.monsterData.info.Translate());
        }

        ui.SetText(monsterInfoSymbolsReplace);
        new UIElementBorder(ui);

        string speechText = monsterInfoSymbolsReplace;

        // Unique monsters have additional info
        if (m.unique && m.uniqueText.KeyExists())
        {
            ui = new UIElement();
            ui.SetLocation(12, 13, UIScaler.GetWidthUnits() - 24, 2);
            ui.SetText(m.uniqueTitle, Color.red);
            ui.SetFontSize(UIScaler.GetMediumFont());
            new UIElementBorder(ui, Color.red);

            string uniqueText = EventManager.OutputSymbolReplace(m.uniqueText.Translate().Replace("\\n", "\n"));
            ui = new UIElement();
            ui.SetLocation(10, 15, UIScaler.GetWidthUnits() - 20, 8);
            ui.SetText(uniqueText);
            new UIElementBorder(ui, Color.red);

            speechText += "\n" + m.uniqueTitle.Translate() + "\n" + uniqueText;

            ui = new UIElement();
            ui.SetLocation(UIScaler.GetWidthUnits() - 21, 23.5f, 10, 2);
            ui.SetText(CommonStringKeys.CLOSE);
            ui.SetFontSize(UIScaler.GetMediumFont());
            ui.SetButton(onClose);
            new UIElementBorder(ui);

            ReadAloudButton.Draw(11, 23.5f, speechText);
        }
        else
        {
            ui = new UIElement();
            ui.SetLocation(UIScaler.GetWidthUnits() - 21, 13, 10, 2);
            ui.SetText(CommonStringKeys.CLOSE);
            ui.SetFontSize(UIScaler.GetMediumFont());
            ui.SetButton(onClose);
            new UIElementBorder(ui);

            ReadAloudButton.Draw(11, 13, speechText);
        }

        TextToSpeech.Speak(speechText);
    }

    // Close cleans up
    public void onClose()
    {
        TextToSpeech.Stop();
        // Clean up everything marked as 'dialog'
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(Game.DIALOG))
            Object.Destroy(go);
    }
}
