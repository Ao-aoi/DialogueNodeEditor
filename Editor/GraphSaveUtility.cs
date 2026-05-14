using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueNodeEditor
{
    public class GraphSaveUtility
    {
        private DialogueGraphView _targetGraphView;
        private DialogueContainer _containerCache;

        private List<Edge> Edges => _targetGraphView.edges.ToList();
        // DialogueGraphNode から BaseGraphNode に変更
        private List<BaseGraphNode> Nodes => _targetGraphView.nodes.ToList().Cast<BaseGraphNode>().ToList();

        public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
        {
            return new GraphSaveUtility
            {
                _targetGraphView = targetGraphView
            };
        }

        public void SaveGraph(string fileName)
        {
            var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();

            var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
            foreach (var edge in connectedPorts)
            {
                var outputNode = edge.output.node as BaseGraphNode;
                var inputNode = edge.input.node as BaseGraphNode;

                dialogueContainer.NodeLinks.Add(new NodeLinkData
                {
                    BaseNodeGuid = outputNode.GUID,
                    PortName = edge.output.portName,
                    TargetNodeGuid = inputNode.GUID
                });
            }

            foreach (var baseNode in Nodes)
            {
                // 現時点ではDialogueNodeのみ保存（他の設定ノードの保存対応は今後の拡張課題）
                if (baseNode is DialogueNode dialogueNode)
                {
                    dialogueContainer.DialogueNodeData.Add(new DialogueNodeData
                    {
                        NodeGUID = dialogueNode.GUID,
                        SpeakerName = dialogueNode.SpeakerName,
                        DialogueText = dialogueNode.DialogueText,
                        Position = dialogueNode.GetPosition().position
                    });
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
            AssetDatabase.SaveAssets();
        }

        public void LoadGraph(string fileName)
        {
            _containerCache = Resources.Load<DialogueContainer>(fileName);
            if (_containerCache == null)
            {
                EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exist!", "OK");
                return;
            }

            ClearGraph();
            CreateNodes();
            ConnectNodes();
        }

        private void ClearGraph()
        {
            foreach (var node in Nodes)
            {
                Edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));
                _targetGraphView.RemoveElement(node);
            }
        }

        private void CreateNodes()
        {
            foreach (var nodeData in _containerCache.DialogueNodeData)
            {
                var tempNode = _targetGraphView.CreateDialogueNode(nodeData.SpeakerName, nodeData.DialogueText, nodeData.Position);
                tempNode.GUID = nodeData.NodeGUID; 
                _targetGraphView.AddElement(tempNode);
            }
        }

        private void ConnectNodes()
        {
            for (var i = 0; i < Nodes.Count; i++)
            {
                var connections = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[i].GUID).ToList();
                for (var j = 0; j < connections.Count; j++)
                {
                    var targetNodeGuid = connections[j].TargetNodeGuid;
                    var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);
                    
                    var outputPort = Nodes[i].outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == connections[j].PortName);
                    // ノード間の遷移は FlowPort 型のポート同士を繋ぐ
                    var inputPort = targetNode.inputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portType == typeof(FlowPort));

                    if (outputPort != null && inputPort != null)
                    {
                        LinkNodes(outputPort, inputPort);
                    }
                }
            }
        }

        private void LinkNodes(Port output, Port input)
        {
            var tempEdge = new Edge { output = output, input = input };
            tempEdge?.input.Connect(tempEdge);
            tempEdge?.output.Connect(tempEdge);
            _targetGraphView.Add(tempEdge);
        }
    }
}