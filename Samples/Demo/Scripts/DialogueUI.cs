using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Button用
using TMPro;

namespace DialogueNodeEditor.Demo
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;
        
        [Header("Choice Settings")]
        public Transform choiceContainer; // ボタンを並べる親要素
        public GameObject choiceButtonPrefab; // ボタンのプレハブ

        [Header("Typing Settings")]
        public float typingSpeed = 0.05f;

        private Coroutine _typingCoroutine;
        private string _currentFullText = "";
        public bool IsTyping { get; private set; }

        public void ShowDialogue(string speakerName, string text)
        {
            // 新しいセリフを表示する前に古いボタンを消す
            ClearChoices();
            
            speakerNameText.text = speakerName;
            _currentFullText = text;
            
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypeTextRoutine(text));
        }

        private IEnumerator TypeTextRoutine(string text)
        {
            IsTyping = true;
            dialogueText.text = "";
            foreach (char c in text.ToCharArray())
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
            IsTyping = false;
        }

        public void SkipTyping()
        {
            if (IsTyping)
            {
                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
                dialogueText.text = _currentFullText;
                IsTyping = false;
            }
        }

        // 選択肢ボタンを生成する
        public void SetChoices(List<NodeLinkData> links, Action<NodeLinkData> onChoiceSelected)
        {
            ClearChoices();
            foreach (var link in links)
            {
                var buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
                // ボタンのテキストを設定（PortNameを選択肢のラベルとして使用）
                var btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = link.PortName;

                // クリック時のイベント登録
                buttonObj.GetComponent<Button>().onClick.AddListener(() => onChoiceSelected(link));
            }
        }

        public void ClearChoices()
        {
            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}