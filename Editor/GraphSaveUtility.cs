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
        private List<Edge> Edges => _targetGraphView.edges.ToList();
        private List<BaseGraphNode> Nodes => _targetGraphView.nodes.ToList().Cast<BaseGraphNode>().ToList();

        public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
        {
            return new GraphSaveUtility { _targetGraphView = targetGraphView };
        }

        public void SaveGraph(string assetPath)
        {
            var dialogueContainer = AssetDatabase.LoadAssetAtPath<DialogueContainer>(assetPath);
            bool isNew = false;
            
            if (dialogueContainer == null)
            {
                dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();
                isNew = true;
            }
            else
            {
                dialogueContainer.NodeLinks.Clear(); dialogueContainer.DialogueNodeData.Clear();
                dialogueContainer.CharacterNodeData.Clear(); dialogueContainer.PortraitNodeData.Clear();
                dialogueContainer.StillNodeData.Clear(); dialogueContainer.PanelSizeNodeData.Clear();
                dialogueContainer.EndNodeData.Clear();
                dialogueContainer.PropertyNodeData.Clear();
                dialogueContainer.ExposedProperties.Clear();
            }

            var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
            foreach (var edge in connectedPorts)
            {
                var outputNode = edge.output.node as BaseGraphNode; var inputNode = edge.input.node as BaseGraphNode;
                dialogueContainer.NodeLinks.Add(new NodeLinkData {
                    BaseNodeGuid = outputNode.GUID, PortName = edge.output.portName, TargetNodeGuid = inputNode.GUID
                });
            }

            foreach (var baseNode in Nodes)
            {
                if (baseNode is DialogueNode dialogueNode)
                {
                    var choices = new List<string>();
                    var ports = dialogueNode.outputContainer.Children().OfType<Port>().ToList();
                    for (int i = 1; i < ports.Count; i++)
                    {
                        choices.Add(ports[i].portName);
                    }

                    string portraitGuid = "";
                    var portraitEdges = dialogueNode.PortraitInputPort.connections.ToList();
                    if (portraitEdges.Count > 0 && portraitEdges[0].output.node is PortraitNode pNode)
                    {
                        portraitGuid = pNode.GUID;
                    }

                    string stillGuid = "";
                    var stillEdges = dialogueNode.StillInputPort.connections.ToList();
                    if (stillEdges.Count > 0 && stillEdges[0].output.node is StillNode sNode)
                    {
                        stillGuid = sNode.GUID;
                    }

                    string floatGuid = "";
                    var floatEdges = dialogueNode.TypingSpeedInputPort.connections.ToList();
                    if (floatEdges.Count > 0 && floatEdges[0].output.node is BaseGraphNode fNode)
                    {
                        floatGuid = fNode.GUID;
                    }

                    dialogueContainer.DialogueNodeData.Add(new DialogueNodeData {
                        NodeGUID = dialogueNode.GUID, 
                        SpeakerName = dialogueNode.SpeakerName,
                        DialogueText = dialogueNode.DialogueText, 
                        Expression = dialogueNode.Expression,
                        Position = dialogueNode.GetPosition().position, 
                        Choices = choices,
                        PortraitNodeGUID = portraitGuid,
                        StillNodeGUID = stillGuid,
                        ShowSettings = dialogueNode.ShowSettings,
                        OverrideTypingSpeed = dialogueNode.OverrideTypingSpeed,
                        TypingSpeedValue = dialogueNode.TypingSpeedField.value,
                        TypingSpeedNodeGUID = floatGuid,
                        CanSkipTyping = dialogueNode.CanSkipTyping
                    });
                }
                else if (baseNode is CharacterNode characterNode)
                {
                    var exprList = characterNode.Expressions.Select(e => new ExpressionData { Name = e.Name, Sprite = e.Sprite }).ToList();
                    dialogueContainer.CharacterNodeData.Add(new CharacterNodeData {
                        NodeGUID = characterNode.GUID, CharacterName = characterNode.CharacterName,
                        Expressions = exprList, Position = characterNode.GetPosition().position
                    });
                }
                else if (baseNode is PortraitNode portraitNode)
                {
                    dialogueContainer.PortraitNodeData.Add(new PortraitNodeData { NodeGUID = portraitNode.GUID, PortraitImage = portraitNode.PortraitImage, Position = portraitNode.GetPosition().position });
                }
                else if (baseNode is StillNode stillNode)
                {
                    dialogueContainer.StillNodeData.Add(new StillNodeData { NodeGUID = stillNode.GUID, StillImage = stillNode.StillImage, ShouldScrollStill = stillNode.ShouldScrollStill, ScrollAmount = stillNode.ScrollAmount, ScrollSpeed = stillNode.ScrollSpeed, Position = stillNode.GetPosition().position });
                }
                else if (baseNode is PanelSizeNode panelSizeNode)
                {
                    dialogueContainer.PanelSizeNodeData.Add(new PanelSizeNodeData { NodeGUID = panelSizeNode.GUID, PanelWidth = panelSizeNode.PanelWidth, Position = panelSizeNode.GetPosition().position });
                }
                else if (baseNode is EndNode endNode)
                {
                    dialogueContainer.EndNodeData.Add(new EndNodeData { NodeGUID = endNode.GUID, Position = endNode.GetPosition().position });
                }
                else if (baseNode is StartNode startNode)
                {
                    dialogueContainer.StartNodeData.Add(new StartNodeData { 
                        NodeGUID = startNode.GUID, 
                        Position = startNode.GetPosition().position 
                    });
                }
                else if (baseNode is PropertyNode propertyNode)
                {
                    dialogueContainer.PropertyNodeData.Add(new PropertyNodeData {
                        NodeGUID = propertyNode.GUID,
                        PropertyName = propertyNode.PropertyName,
                        Position = propertyNode.GetPosition().position
                    });
                }
            }

            dialogueContainer.ExposedProperties.AddRange(_targetGraphView.ExposedProperties);

            if (isNew) AssetDatabase.CreateAsset(dialogueContainer, assetPath);
            else EditorUtility.SetDirty(dialogueContainer);
            AssetDatabase.SaveAssets();
        }

        public void LoadGraph(DialogueContainer container)
        {
            if (container == null) return;
            ClearGraph(); 

            _targetGraphView.ClearBlackBoardAndData();
            foreach (var prop in container.ExposedProperties)
            {
                _targetGraphView.AddPropertyToBlackBoard(prop);
            }

            CreateNodes(container); ConnectNodes(container);

            foreach (var node in Nodes.OfType<DialogueNode>())
            {
                var data = container.DialogueNodeData.FirstOrDefault(x => x.NodeGUID == node.GUID);
                if (data != null)
                {
                    var charNodes = Nodes.OfType<CharacterNode>().ToList();
                    if (charNodes.Any(c => c.CharacterName == data.SpeakerName))
                    {
                        node.CharacterDropdown.SetValueWithoutNotify(data.SpeakerName);
                    }
                    else
                    {
                        node.CharacterDropdown.SetValueWithoutNotify("None (Custom)");
                    }

                    node.UpdateCharacterState();

                    if (!string.IsNullOrEmpty(data.Expression))
                    {
                        node.Expression = data.Expression;
                        // ★修正: ToList()を追加して、LINQのエラーが出ないようにしました
                        var dropdown = node.mainContainer.Query<DropdownField>().ToList().FirstOrDefault(d => d.label == "Expression");
                        if (dropdown != null) dropdown.SetValueWithoutNotify(data.Expression);                    
                    }

                    node.ShowSettings = data.ShowSettings;
                    node.OverrideTypingSpeed = data.OverrideTypingSpeed;
                    node.TypingSpeedToggle.SetValueWithoutNotify(data.OverrideTypingSpeed);
                    node.TypingSpeedField.SetValueWithoutNotify(data.TypingSpeedValue);
                    node.TypingSpeedValue = data.TypingSpeedValue;
                    
                    node.CanSkipTyping = data.CanSkipTyping;
                    node.CanSkipTypingToggle.SetValueWithoutNotify(data.CanSkipTyping);
                    
                    node.TypingSpeedInputPort.style.display = data.OverrideTypingSpeed ? DisplayStyle.Flex : DisplayStyle.None;
                    node.UpdateSettingsUI();
                }
            }
        }

        private void ClearGraph()
        {
            foreach (var node in Nodes)
            {
                Edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));
                _targetGraphView.RemoveElement(node);
            }
        }

        private void CreateNodes(DialogueContainer container)
        {
            foreach (var nodeData in container.DialogueNodeData)
            {
                var tempNode = _targetGraphView.CreateDialogueNode(nodeData.SpeakerName, nodeData.DialogueText, nodeData.Position);
                tempNode.GUID = nodeData.NodeGUID;
                
                if (nodeData.Choices != null)
                {
                    foreach (var choice in nodeData.Choices)
                        if (!string.IsNullOrEmpty(choice)) tempNode.AddChoicePort(choice);
                }
                _targetGraphView.AddElement(tempNode);
            }
            foreach (var data in container.CharacterNodeData)
            {
                var tempNode = new CharacterNode();
                tempNode.SetPosition(new Rect(data.Position, new Vector2(300, 150)));
                tempNode.LoadData(data.CharacterName, data.Expressions);
                tempNode.GUID = data.NodeGUID;
                _targetGraphView.AddElement(tempNode);
            }
            foreach (var data in container.PortraitNodeData)
            {
                var tempNode = new PortraitNode(); tempNode.SetPosition(new Rect(data.Position, new Vector2(200, 150)));
                tempNode.LoadData(data.PortraitImage); tempNode.GUID = data.NodeGUID; _targetGraphView.AddElement(tempNode);
            }
            foreach (var data in container.StillNodeData)
            {
                var tempNode = new StillNode(); tempNode.SetPosition(new Rect(data.Position, new Vector2(200, 150)));
                tempNode.LoadData(data.StillImage, data.ShouldScrollStill, data.ScrollAmount, data.ScrollSpeed); tempNode.GUID = data.NodeGUID; _targetGraphView.AddElement(tempNode);
            }
            foreach (var data in container.PanelSizeNodeData)
            {
                var tempNode = new PanelSizeNode(); tempNode.SetPosition(new Rect(data.Position, new Vector2(200, 150)));
                tempNode.LoadData(data.PanelWidth); tempNode.GUID = data.NodeGUID; _targetGraphView.AddElement(tempNode);
            }
            foreach (var data in container.EndNodeData)
            {
                var tempNode = new EndNode(); tempNode.SetPosition(new Rect(data.Position, new Vector2(150, 100)));
                tempNode.GUID = data.NodeGUID; _targetGraphView.AddElement(tempNode);
            }
            foreach (var data in container.StartNodeData)
            {
                var tempNode = new StartNode();
                tempNode.SetPosition(new Rect(data.Position, new Vector2(100, 100)));
                tempNode.GUID = data.NodeGUID;
                _targetGraphView.AddElement(tempNode);
            }
            foreach (var pData in container.PropertyNodeData)
            {
                var tempNode = new PropertyNode { PropertyName = pData.PropertyName, title = pData.PropertyName };
                tempNode.SetPosition(new Rect(pData.Position, new Vector2(150, 100)));
                tempNode.GUID = pData.NodeGUID;
                _targetGraphView.AddElement(tempNode);
            }
        }

        private void ConnectNodes(DialogueContainer container)
        {
            for (var i = 0; i < Nodes.Count; i++)
            {
                var connections = container.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[i].GUID).ToList();
                for (var j = 0; j < connections.Count; j++)
                {
                    var targetNodeGuid = connections[j].TargetNodeGuid;
                    var targetNode = Nodes.FirstOrDefault(x => x.GUID == targetNodeGuid);
                    if (targetNode == null) continue;
                    
                    var outputPort = Nodes[i].outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == connections[j].PortName);
                    if (outputPort != null)
                    {
                        var inputPort = targetNode.inputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portType == outputPort.portType);
                        if (inputPort != null) LinkNodes(outputPort, inputPort);
                    }
                }
            }
        }

        private void LinkNodes(Port output, Port input)
        {
            var tempEdge = new Edge { output = output, input = input };
            tempEdge?.input.Connect(tempEdge); tempEdge?.output.Connect(tempEdge);
            _targetGraphView.Add(tempEdge);
        }
    }
}