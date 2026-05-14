using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DialogueNodeEditor.Demo
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;
        public Image portraitImage; // Canvas上で作成したImage(UI)を割り当ててください

        [Header("Choice Settings")]
        public Transform choiceContainer;
        public GameObject choiceButtonPrefab;

        [Header("Typing Settings")]
        public float typingSpeed = 0.05f;

        [Header("Animation Settings")]
        public float slideInDuration = 0.3f;
        public Vector2 portraitHiddenPosition = new Vector2(-500, 0); // 画面外（左側など）
        public Vector2 portraitVisiblePosition = new Vector2(0, 0);   // 通常の表示位置

        private Coroutine _typingCoroutine;
        private Coroutine _slideCoroutine;
        private string _currentFullText = "";
        private string _lastSpeaker = "";

        public bool IsTyping { get; private set; }

        public void ShowDialogue(string speakerName, string text, Sprite portraitSprite)
        {
            ClearChoices();
            
            speakerNameText.text = speakerName;
            _currentFullText = text;
            
            // ポートレート（立ち絵）の表示制御とアニメーション
            if (portraitSprite != null)
            {
                portraitImage.gameObject.SetActive(true);
                portraitImage.sprite = portraitSprite;

                // 違うキャラクターに変わった場合のみスライドインさせる
                if (_lastSpeaker != speakerName)
                {
                    if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
                    _slideCoroutine = StartCoroutine(SlideInPortrait());
                }
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
            
            _lastSpeaker = speakerName;

            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypeTextRoutine(text));
        }

        private IEnumerator SlideInPortrait()
        {
            RectTransform rect = portraitImage.rectTransform;
            rect.anchoredPosition = portraitHiddenPosition;

            float elapsedTime = 0f;
            while (elapsedTime < slideInDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / slideInDuration);
                
                // 少し減速しながらスライドさせる (EaseOut)
                t = 1f - Mathf.Pow(1f - t, 3f); 
                rect.anchoredPosition = Vector2.Lerp(portraitHiddenPosition, portraitVisiblePosition, t);
                yield return null;
            }
            rect.anchoredPosition = portraitVisiblePosition;
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

        public void SetChoices(List<NodeLinkData> links, Action<NodeLinkData> onChoiceSelected)
        {
            ClearChoices();
            foreach (var link in links)
            {
                var buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
                var btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = link.PortName;

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
        public void HideDialogue()
        {
            // DialogueUIがアタッチされているオブジェクト（Canvas等）を非表示にする
            gameObject.SetActive(false);
        }
    }
}