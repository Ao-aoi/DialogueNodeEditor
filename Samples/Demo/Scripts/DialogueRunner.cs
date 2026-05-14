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
                if (dialogueUI.IsTyping) dialogueUI.SkipTyping();
                else CheckNextStep();
            }
        }

        private void PlayNode(DialogueNodeData node)
        {
            _waitingForChoice = false;

            // 立ち絵（Sprite）の検索
            Sprite characterSprite = null;
            if (!string.IsNullOrEmpty(node.SpeakerName))
            {
                var charData = dialogueData.CharacterNodeData.FirstOrDefault(c => c.CharacterName == node.SpeakerName);
                if (charData != null)
                {
                    var exprData = charData.Expressions.FirstOrDefault(e => e.Name == node.Expression);
                    if (exprData != null) characterSprite = exprData.Sprite;
                }
            }

            dialogueUI.ShowDialogue(node.SpeakerName, node.DialogueText, characterSprite);
        }

        private void CheckNextStep()
        {
            var links = dialogueData.NodeLinks.Where(x => x.BaseNodeGuid == _currentNode.NodeGUID).ToList();

            if (links.Count > 1)
            {
                _waitingForChoice = true;
                dialogueUI.SetChoices(links, OnChoiceSelected);
            }
            else if (links.Count == 1)
            {
                TransitionTo(links[0].TargetNodeGuid);
            }
            else EndDialogue();
        }

        private void OnChoiceSelected(NodeLinkData selectedLink)
        {
            dialogueUI.ClearChoices();
            TransitionTo(selectedLink.TargetNodeGuid);
        }

        private void TransitionTo(string targetGuid)
        {
            _currentNode = dialogueData.DialogueNodeData.FirstOrDefault(x => x.NodeGUID == targetGuid);
            if (_currentNode != null) PlayNode(_currentNode);
            else EndDialogue();
        }

        private void EndDialogue()
        {
            _currentNode = null;
            dialogueUI.ShowDialogue("System", "End of Demo.", null);
        }
    }
}