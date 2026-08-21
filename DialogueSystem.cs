using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Zeigt Dialogsequenzen mit Typping-Effekt, optionalen Entscheidungen
    /// und Voice-Ausgabe. Ruft einen Callback auf, sobald die Sequenz endet.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance;

        [Header("UI")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI speakerText;
        public TextMeshProUGUI dialogueText;
        public Image speakerPortrait;
        public Button continueButton;
        public GameObject choicePanel;
        public Button choiceButtonPrefab;
        public float textSpeed = 0.05f;
        public AudioClip typingSound;

        private readonly Queue<DialogueNode> queue = new Queue<DialogueNode>();
        private Action onComplete;
        private Coroutine typingCoroutine;
        private bool isTyping;
        private DialogueNode currentNode;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        }

        public void PlaySequence(List<DialogueNode> nodes, Action onComplete)
        {
            queue.Clear();
            foreach (var n in nodes) queue.Enqueue(n);
            this.onComplete = onComplete;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            ShowNext();
        }

        void ShowNext()
        {
            if (queue.Count == 0)
            {
                EndSequence();
                return;
            }
            currentNode = queue.Dequeue();
            if (speakerText != null) speakerText.text = currentNode.speaker;
            if (speakerPortrait != null && currentNode.speakerPortrait != null)
                speakerPortrait.sprite = currentNode.speakerPortrait;
            if (currentNode.voice != null) AudioManager.Instance?.PlayVoice(currentNode.voice);

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(currentNode.text));

            if (currentNode.isChoice && currentNode.choices != null && currentNode.choices.Count > 0)
            {
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                ShowChoices(currentNode.choices);
            }
            else
            {
                if (choicePanel != null) choicePanel.SetActive(false);
                if (continueButton != null) continueButton.gameObject.SetActive(true);
            }
        }

        IEnumerator TypeText(string text)
        {
            isTyping = true;
            if (dialogueText != null) dialogueText.text = "";
            foreach (char c in text)
            {
                if (dialogueText != null) dialogueText.text += c;
                if (c != ' ' && typingSound != null) AudioManager.Instance?.PlaySFX(typingSound, 0.1f);
                yield return new WaitForSeconds(textSpeed);
            }
            isTyping = false;

            if (currentNode != null && currentNode.displayTime > 0f && !currentNode.isChoice)
            {
                yield return new WaitForSeconds(currentNode.displayTime);
                OnContinue();
            }
        }

        void OnContinue()
        {
            if (isTyping)
            {
                // Sofort fertig tippen
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                isTyping = false;
                if (dialogueText != null && currentNode != null) dialogueText.text = currentNode.text;
                return;
            }
            ShowNext();
        }

        void ShowChoices(List<ChoiceOption> choices)
        {
            if (choicePanel == null) return;
            choicePanel.SetActive(true);
            foreach (Transform child in choicePanel.transform)
                if (child.GetComponent<Button>() != choiceButtonPrefab)
                    Destroy(child.gameObject);

            foreach (var choice in choices)
            {
                Button btn = Instantiate(choiceButtonPrefab, choicePanel.transform);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = choice.text;
                ChoiceOption option = choice;
                btn.onClick.AddListener(() => OnChoice(option));
            }
        }

        void OnChoice(ChoiceOption option)
        {
            if (choicePanel != null) choicePanel.SetActive(false);
            if (!string.IsNullOrEmpty(option.consequence))
                StoryManager.Instance?.SetStoryState("choice_" + option.consequence, "true");
            ShowNext();
        }

        void EndSequence()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            var cb = onComplete;
            onComplete = null;
            cb?.Invoke();
        }

        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null
                && (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Space].wasPressedThisFrame
                    || UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Enter].wasPressedThisFrame))
            {
                if (dialoguePanel != null && dialoguePanel.activeSelf)
                    OnContinue();
            }
        }
    }
}
