using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Content;
using Assets.Scripts.UI;
using ValkyrieTools;

// Window with monster horror check information
public class HorrorCheck {
    public HorrorCheck(Quest.Monster m)
    {
        Game game = Game.Get();

        QuestMonster qm = m.monsterData as QuestMonster;
        if (qm != null && game.CurrentQuest.qd.components.ContainsKey(qm.cMonster.horrorEvent))
        {
            game.CurrentQuest.eManager.monsterImage = m;
            game.CurrentQuest.eManager.QueueEvent(qm.cMonster.horrorEvent);
        }
        else
        {
            PickHorror(m);
        }
    }

    public void PickHorror(Quest.Monster m)
    {
        Game game = Game.Get();
        QuestMonster qm = m.monsterData as QuestMonster;
        List<HorrorData> horrors = MonsterData.FindForMonster(game.cd.Values<HorrorData>(), hd => hd.monster,
            m.monsterData.sectionName, qm?.derivedType);

        if (horrors.Count == 0)
        {
            ValkyrieDebug.Log("Warning: No horror check found for monster: " + m.monsterData.sectionName);
            return;
        }
        Draw(horrors[Random.Range(0, horrors.Count)], m);
    }

    protected void Draw(HorrorData horror, Quest.Monster m)
    {
        Game game = Game.Get();
        Destroyer.Dialog();

        string text = horror.text.Translate().Replace("{0}", m.monsterData.name.Translate());
        UIElement ui = new UIElement();
        ui.SetLocation(UIScaler.GetHCenter(-14), 0.5f, 28, 8);
        ui.SetText(text);
        new UIElementBorder(ui);

        game.CurrentQuest.log.Add(new Quest.LogEntry(text.Replace("\n", "\\n")));

        ui = new UIElement();
        ui.SetLocation(UIScaler.GetHCenter(-6f), 9, 12, 2);
        ui.SetText(CommonStringKeys.FINISHED);
        ui.SetFontSize(UIScaler.GetMediumFont());
        ui.SetButton(Destroyer.Dialog);
        new UIElementBorder(ui);

        MonsterDialogMoM.DrawMonster(m, true);
    }
}
