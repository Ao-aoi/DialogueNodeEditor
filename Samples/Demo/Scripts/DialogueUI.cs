using System.Collections;
using UnityEngine;
using TMPro; // TextMeshProを使用

namespace DialogueNodeEditor.Demo
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;

        [Header("Settings")]
        public float typingSpeed = 0.05f; // 1文字表示されるまでの秒数

        private Coroutine _typingCoroutine;
        private string _currentFullText = "";

        // タイピング中かどうか判定するプロパティ
        public bool IsTyping { get; private set; }

        /// <summary>
        /// ダイアログを表示し、タイピングアニメーションを開始する
        /// </summary>
        public void ShowDialogue(string speakerName, string text)
        {
            speakerNameText.text = speakerName;
            _currentFullText = text;
            
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }
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

        /// <summary>
        /// タイピングアニメーションをスキップし、全文字を即座に表示する
        /// </summary>
        public void SkipTyping()
        {
            if (IsTyping)
            {
                if (_typingCoroutine != null)
                {
                    StopCoroutine(_typingCoroutine);
                }
                dialogueText.text = _currentFullText;
                IsTyping = false;
            }
        }
    }
}