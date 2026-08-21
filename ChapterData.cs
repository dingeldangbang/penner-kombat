using System;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    // ===== Datenmodelle für den Story-Modus =====

    /// <summary>Eine Dialogzeile mit Sprecher, Text, Porträt und optionaler Stimme.</summary>
    [Serializable]
    public class DialogueNode
    {
        public string id;
        public string speaker;
        public string text;
        public Sprite speakerPortrait;
        public AudioClip voice;
        public float displayTime = -1f;   // -1 = warten auf Bestätigung
        public bool isChoice;
        public List<ChoiceOption> choices;
    }

    /// <summary>Eine Entscheidungsmöglichkeit innerhalb eines Dialogs.</summary>
    [Serializable]
    public class ChoiceOption
    {
        public string text;
        public string consequence;   // z.B. "d" für Ende D "LEERGUT LIBRE"
    }

    /// <summary>Ein Kampf innerhalb eines Kapitels.</summary>
    [Serializable]
    public class StoryFight
    {
        public string opponentId;
        public string arenaId;
        public int difficulty = 1;
        public bool isBoss;
        public string winDialogueId;
        public string loseDialogueId;
    }

    /// <summary>Ein vollständiges Kapitel (Dialoge + Kämpfe + Weiterschaltung).</summary>
    [Serializable]
    public class StoryChapter
    {
        public string id;
        public string chapterName;
        public string description;
        public int chapterIndex;
        public Sprite background;
        public AudioClip music;
        public List<DialogueNode> dialogues = new List<DialogueNode>();
        public List<StoryFight> fights = new List<StoryFight>();
        public string nextChapterId;
        public bool isFinalChapter;
    }

    /// <summary>ScriptableObject-Wrapper für ein Kapitel (Editor-freundlich).</summary>
    [CreateAssetMenu(fileName = "ChapterData", menuName = "PennerKombat/ChapterData")]
    public class ChapterData : ScriptableObject
    {
        public StoryChapter chapter;
    }
}
