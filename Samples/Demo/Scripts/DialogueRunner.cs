using System.Linq;
using UnityEngine;
using DialogueNodeEditor; // Runtimeのネームスペース

namespace DialogueNodeEditor.Demo
{
    public class DialogueRunner : MonoBehaviour
    {
        [Header("References")]
        public DialogueContainer dialogueData; // エディタで作成した保存データ
        public DialogueUI dialogueUI;

        private DialogueNodeData _currentNode;

        private void Start()
        {
            if (dialogueData == null || dialogueData.DialogueNodeData.Count == 0)
            {
                Debug.LogWarning("ダイアログデータが設定されていないか、空です。");
                return;
            }

            // デモ用として、ノードリストの最初の要素をスタート地点とする
            _currentNode = dialogueData.DialogueNodeData[0];
            PlayNode(_currentNode);
        }

        private void Update()
        {
            // マウスクリック（または画面タップ）で進行
            if (Input.GetMouseButtonDown(0))
            {
                if (dialogueUI.IsTyping)
                {
                    // タイピング中ならスキップして全文字表示
                    dialogueUI.SkipTyping();
                }
                else
                {
                    // 既に全文字表示されていれば次のノードへ
                    GoToNextNode();
                }
            }
        }

        private void PlayNode(DialogueNodeData node)
        {
            dialogueUI.ShowDialogue(node.SpeakerName, node.DialogueText);
        }

        private void GoToNextNode()
        {
            if (_currentNode == null) return;

            // 現在のノードを起点（BaseNodeGuid）としているリンクを探す
            var link = dialogueData.NodeLinks.FirstOrDefault(x => x.BaseNodeGuid == _currentNode.NodeGUID);
            
            if (link != null)
            {
                // リンク先のノード（TargetNodeGuid）を探して次のノードに設定
                _currentNode = dialogueData.DialogueNodeData.FirstOrDefault(x => x.NodeGUID == link.TargetNodeGuid);
                
                if (_currentNode != null)
                {
                    PlayNode(_currentNode);
                }
                else
                {
                    EndDialogue();
                }
            }
            else
            {
                EndDialogue();
            }
        }

        private void EndDialogue()
        {
            _currentNode = null;
            dialogueUI.ShowDialogue("システム", "（ダイアログ終了）");
            Debug.Log("ダイアログが終了しました。");
        }
    }
}