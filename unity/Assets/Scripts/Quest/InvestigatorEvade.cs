using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Content;
using Assets.Scripts.UI;
using ValkyrieTools;

// Window with Investigator evade information
public class InvestigatorEvade {
    Quest.Monster m;
    string text;

    public InvestigatorEvade(Quest.Monster monster)
    {
        m = monster;
        Game game = Game.Get();

        QuestMonster qm = m.monsterData as QuestMonster;
        if (qm != null && game.CurrentQuest.qd.components.ContainsKey(qm.cMonster.evadeEvent))
        {
            game.CurrentQuest.eManager.monsterImage = m;
            game.CurrentQuest.eManager.QueueEvent(qm.cMonster.evadeEvent);
        }
        else
        {
            PickEvade(m);
        }
    }

    public void PickEvade(Quest.Monster m)
    {
        Game game = Game.Get();
        QuestMonster qm = m.monsterData as QuestMonster;
        List<EvadeData> evades = MonsterData.FindForMonster(game.cd.Values<EvadeData>(), ed => ed.monster,
            m.monsterData.sectionName, qm?.derivedType);

        if (evades.Count == 0)
        {
            ValkyrieDebug.Log("Warning: No evade found for monster: " + m.monsterData.sectionName);
            return;
        }

        text = evades[Random.Range(0, evades.Count)].text.Translate().Replace("{0}", m.monsterData.name.Translate());

        game.CurrentQuest.log.Add(new Quest.LogEntry(text.Replace("\n", "\\n")));

        Draw();
        TextToSpeech.Speak(text);
    }

    public void Draw()
    {
        Destroyer.Dialog();
        UIElement ui = new UIElement();
        ui.SetLocation(10, 0.5f, UIScaler.GetWidthUnits() - 20, 8);
        ui.SetText(text);
        new UIElementBorder(ui);
        ReadAloudButton.Draw(UIScaler.GetWidthUnits() - 14.5f, 0.5f, text);

        ui = new UIElement();
        ui.SetLocation(UIScaler.GetHCenter(-6f), 9, 12, 2);
        if (m.damage == m.GetHealth())
        {
            ui.SetText(CommonStringKeys.FINISHED, Color.grey);
            new UIElementBorder(ui, Color.grey);
        }
        else
        {
            ui.SetText(CommonStringKeys.FINISHED);
            ui.SetButton(delegate { TextToSpeech.Stop(); Destroyer.Dialog(); });
            new UIElementBorder(ui);
        }
        ui.SetFontSize(UIScaler.GetMediumFont());

        MonsterDialogMoM.DrawMonster(m, true);
    }
}
