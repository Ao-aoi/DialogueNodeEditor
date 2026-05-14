using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueNodeEditor.Demo
{
    public class DialogueRunner : MonoBehaviour
    {
        public DialogueContainer dialogueData;
        public DialogueUI dialogueUI;

        private DialogueNodeData _currentNode;
        private bool _waitingForChoice = false;

        private void Start()
        {
            if (dialogueData == null || dialogueData.DialogueNodeData.Count == 0) return;
            
            _currentNode = dialogueData.DialogueNodeData[0];
            PlayNode(_currentNode);
        }

        private void Update()
        {
            if (_waitingForChoice) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (dialogueUI.IsTyping)
                {
                    dialogueUI.SkipTyping();
                }
                else
                {
                    CheckNextStep();
                }
            }
        }

        private void PlayNode(DialogueNodeData node)
        {
            _waitingForChoice = false;
            dialogueUI.ShowDialogue(node.SpeakerName, node.DialogueText);
        }

        private void CheckNextStep()
        {
            // 現在のノードからの全リンクを取得
            var links = dialogueData.NodeLinks.Where(x => x.BaseNodeGuid == _currentNode.NodeGUID).ToList();

            if (links.Count > 1)
            {
                // 選択肢が複数ある場合：ボタンを表示して入力を待つ
                _waitingForChoice = true;
                dialogueUI.SetChoices(links, OnChoiceSelected);
            }
            else if (links.Count == 1)
            {
                // リンクが1つだけの場合：そのまま次へ（クリック進行）
                TransitionTo(links[0].TargetNodeGuid);
            }
            else
            {
                // リンクがない場合：終了
                EndDialogue();
            }
        }

        private void OnChoiceSelected(NodeLinkData selectedLink)
        {
            dialogueUI.ClearChoices();
            TransitionTo(selectedLink.TargetNodeGuid);
        }

        private void TransitionTo(string targetGuid)
        {
            _currentNode = dialogueData.DialogueNodeData.FirstOrDefault(x => x.NodeGUID == targetGuid);
            if (_currentNode != null)
            {
                PlayNode(_currentNode);
            }
            else
            {
                EndDialogue();
            }
        }

        private void EndDialogue()
        {
            _currentNode = null;
            dialogueUI.ShowDialogue("System", "End of Demo.");
        }
    }
}