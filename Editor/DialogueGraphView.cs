using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace DialogueNodeEditor
{
    public class DialogueGraphView : GraphView
    {
        public Blackboard Blackboard;
        public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
        public DialogueGraphWindow EditorWindow { get; }

        public void ClearBlackBoardAndData()
        {
            ExposedProperties.Clear();
            if (Blackboard != null)
            {
                Blackboard.Clear();
                Blackboard.Add(new BlackboardSection { title = "Exposed Properties" });
            }
        }

        public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
        {
            var localPropertyName = exposedProperty.PropertyName;
            var localPropertyValue = exposedProperty.PropertyValue;
            
            while (ExposedProperties.Any(x => x.PropertyName == localPropertyName))
                localPropertyName = $"{localPropertyName}(1)";
            
            var property = new ExposedProperty { PropertyName = localPropertyName, PropertyValue = localPropertyValue };
            ExposedProperties.Add(property);

            var container = new VisualElement();
            var blackboardField = new BlackboardField { text = property.PropertyName, typeText = "Float" };
            container.Add(blackboardField);

            var propertyValueTextField = new FloatField("Value") { value = property.PropertyValue };
            propertyValueTextField.RegisterValueChangedCallback(evt => {
                var index = ExposedProperties.FindIndex(x => x.PropertyName == property.PropertyName);
                ExposedProperties[index].PropertyValue = evt.newValue;
            });

            var blackboardRow = new BlackboardRow(blackboardField, propertyValueTextField);
            container.Add(blackboardRow);
            Blackboard.Add(container);
        }

        public DialogueGraphView(DialogueGraphWindow editorWindow)
        {
            EditorWindow = editorWindow;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheets.Add(styleSheet);

            graphViewChanged = OnGraphViewChanged;

            // ★修正: デリゲート名の誤り(canPaste)を canPasteSerializedData に修正
            serializeGraphElements = OnSerializeGraphElements;
            canPasteSerializedData = OnCanPaste; 
            unserializeAndPaste = OnUnserializeAndPaste;
        }

        // 複製 (Ctrl+D) 機能
        public void DuplicateNodes()
        {
            // ★修正: selection を GraphElement にキャスト(OfType)してから渡す
            var data = OnSerializeGraphElements(selection.OfType<GraphElement>());
            if (!string.IsNullOrEmpty(data))
            {
                OnUnserializeAndPaste("Duplicate", data);
            }
        }

        // コピー用データのシリアライズ
        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var copyData = new CopyPasteData();
            foreach (var element in elements)
            {
                if (element is DialogueNode dNode)
                {
                    var choices = dNode.outputContainer.Children().OfType<Port>().Skip(1).Select(p => p.portName).ToList();
                    copyData.DialogueNodes.Add(new DialogueNodeData {
                        SpeakerName = dNode.SpeakerName, DialogueText = dNode.DialogueText, Expression = dNode.Expression,
                        Position = dNode.GetPosition().position, Choices = choices, ShowSettings = dNode.ShowSettings,
                        OverrideTypingSpeed = dNode.OverrideTypingSpeed, TypingSpeedValue = dNode.TypingSpeedValue, CanSkipTyping = dNode.CanSkipTyping,
                        AutoAdvance = dNode.AutoAdvance
                    });
                }
                else if (element is CharacterNode cNode)
                {
                    copyData.CharacterNodes.Add(new CharacterNodeData {
                        CharacterName = cNode.CharacterName, Position = cNode.GetPosition().position,
                        Expressions = cNode.Expressions.Select(e => new ExpressionData { Name = e.Name, Sprite = e.Sprite }).ToList()
                    });
                }
                else if (element is PortraitNode pNode)
                {
                    copyData.PortraitNodes.Add(new PortraitNodeData { PortraitImage = pNode.PortraitImage, Position = pNode.GetPosition().position });
                }
                else if (element is StillNode sNode)
                {
                    copyData.StillNodes.Add(new StillNodeData { StillImage = sNode.StillImage, ShouldScrollStill = sNode.ShouldScrollStill, ScrollAmount = sNode.ScrollAmount, ScrollSpeed = sNode.ScrollSpeed, Position = sNode.GetPosition().position });
                }
                else if (element is PanelSettingNode psNode)
                {
                    copyData.PanelSizeNodes.Add(new PanelSettingNodeData { PanelWidth = psNode.PanelWidth, PanelHeight = psNode.PanelHeight, PanelSprite = psNode.PanelSprite, Position = psNode.GetPosition().position });
                }
                else if (element is PropertyNode propNode)
                {
                    copyData.PropertyNodes.Add(new PropertyNodeData { PropertyName = propNode.PropertyName, Position = propNode.GetPosition().position });
                }
            }
            return JsonUtility.ToJson(copyData);
        }

        // ペースト可能かどうかの判定
        private bool OnCanPaste(string data)
        {
            return true;
        }

        // ペーストしてノードを生成
        private void OnUnserializeAndPaste(string operationName, string data)
        {
            var copyData = JsonUtility.FromJson<CopyPasteData>(data);
            if (copyData == null) return;

            ClearSelection();
            Vector2 offset = new Vector2(50, 50); // ペースト時に少しずらす

            foreach(var d in copyData.DialogueNodes)
            {
                var node = CreateDialogueNode(d.SpeakerName, d.DialogueText, d.Position + offset);
                node.Expression = d.Expression;
                node.ShowSettings = d.ShowSettings; node.OverrideTypingSpeed = d.OverrideTypingSpeed;
                node.TypingSpeedValue = d.TypingSpeedValue; node.CanSkipTyping = d.CanSkipTyping;
                node.AutoAdvance = d.AutoAdvance;
                if(d.Choices != null) foreach(var c in d.Choices) node.AddChoicePort(c);
                node.UpdateSettingsUI();
                node.GUID = System.Guid.NewGuid().ToString();
                AddElement(node); AddToSelection(node);
            }
            foreach(var c in copyData.CharacterNodes)
            {
                var node = new CharacterNode(); node.SetPosition(new Rect(c.Position + offset, new Vector2(300, 150)));
                node.LoadData(c.CharacterName, c.Expressions); node.GUID = System.Guid.NewGuid().ToString(); AddElement(node); AddToSelection(node);
            }
            foreach(var p in copyData.PortraitNodes)
            {
                var node = new PortraitNode(); node.SetPosition(new Rect(p.Position + offset, new Vector2(200, 150))); 
                node.LoadData(p.PortraitImage); node.GUID = System.Guid.NewGuid().ToString(); AddElement(node); AddToSelection(node);
            }
            foreach(var s in copyData.StillNodes)
            {
                var node = new StillNode(); node.SetPosition(new Rect(s.Position + offset, new Vector2(200, 150))); 
                node.LoadData(s.StillImage, s.ShouldScrollStill, s.ScrollAmount, s.ScrollSpeed); node.GUID = System.Guid.NewGuid().ToString(); AddElement(node); AddToSelection(node);
            }
            foreach(var ps in copyData.PanelSizeNodes)
            {
                var node = new PanelSettingNode(); node.SetPosition(new Rect(ps.Position + offset, new Vector2(200, 150))); 
                node.LoadData(ps.PanelWidth, ps.PanelHeight, ps.PanelSprite); node.GUID = System.Guid.NewGuid().ToString(); AddElement(node); AddToSelection(node);
            }
            foreach(var prop in copyData.PropertyNodes)
            {
                var node = new PropertyNode { PropertyName = prop.PropertyName, title = prop.PropertyName }; 
                node.SetPosition(new Rect(prop.Position + offset, new Vector2(150, 100))); node.GUID = System.Guid.NewGuid().ToString(); AddElement(node); AddToSelection(node);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            // --- Edge（線）が接続されたとき ---
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    // 接続先が DialogueNode の場合
                    if (edge.input.node is DialogueNode dNode)
                    {
                        // 動的ポートの増減チェックを走らせる（少し遅らせて実行）
                        dNode.schedule.Execute(() => {
                            dNode.UpdateDynamicPorts();
                            dNode.UpdateCharacterState();
                        }).StartingIn(10);
                    }
                }
            }

            // --- 要素（EdgeやNode）が削除されたとき ---
            if (graphViewChange.elementsToRemove != null)
            {
                bool needsDropdownUpdate = false;
                List<DialogueNode> affectedNodes = new List<DialogueNode>();

                foreach (var elem in graphViewChange.elementsToRemove)
                {
                    if (elem is Edge edge)
                    {
                        if (edge.input != null && edge.input.node is DialogueNode dNode)
                        {
                            if (!affectedNodes.Contains(dNode)) affectedNodes.Add(dNode);
                        }
                    }
                    else if (elem is CharacterNode)
                    {
                        needsDropdownUpdate = true;
                    }
                }

                // 線が外されたノードに対して動的ポートの削減処理を走らせる
                foreach (var dNode in affectedNodes)
                {
                    dNode.schedule.Execute(() => {
                        dNode.UpdateDynamicPorts();
                        dNode.UpdateCharacterState();
                    }).StartingIn(10);
                }

                if (needsDropdownUpdate)
                {
                    this.schedule.Execute(() => RefreshAllDialogueNodesDropdowns()).StartingIn(50);
                }
            }

            return graphViewChange;
        }

        // ★追加: グラフ内の全 DialogueNode を探し、プルダウンを再構築するメソッド
        public void RefreshAllDialogueNodesDropdowns()
        {
            var dialogueNodes = nodes.ToList().OfType<DialogueNode>();
            foreach (var node in dialogueNodes)
            {
                node.UpdateCharacterState(); // キャラクターリストを再取得してドロップダウンを更新
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach((port) =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    // FlowPort 同士の接続はそのまま許可
                    if (startPort.portType == typeof(FlowPort) && port.portType == typeof(FlowPort))
                    {
                        compatiblePorts.Add(port);
                    }
                    // DynamicInputPort (typeof(object)) に対する接続制限
                    else if (startPort.direction == Direction.Output && port.portType == typeof(object))
                    {
                        // 出力側が FlowPort 以外（設定系ノード）なら汎用インプットに繋げてOK
                        if (startPort.portType != typeof(FlowPort))
                        {
                            compatiblePorts.Add(port);
                        }
                    }
                    else if (startPort.direction == Direction.Input && startPort.portType == typeof(object))
                    {
                        // 入力側が汎用インプットの場合、相手のOutputがFlowPort以外なら繋げてOK
                        if (port.direction == Direction.Output && port.portType != typeof(FlowPort))
                        {
                            compatiblePorts.Add(port);
                        }
                    }
                }
            });
            return compatiblePorts;
        }

        public void CreateNode(BaseGraphNode node, Vector2 position)
        {
            node.GUID = System.Guid.NewGuid().ToString();
            node.SetPosition(new Rect(position, new Vector2(200, 150)));
            AddElement(node);

            // ★追加: もし追加されたノードがCharacterNodeだったら、プルダウンを一斉更新する
            if (node is CharacterNode)
            {
                this.schedule.Execute(() => RefreshAllDialogueNodesDropdowns()).StartingIn(50);
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            var mousePosition = contentViewContainer.WorldToLocal(evt.localMousePosition);

            evt.menu.AppendAction("Add Dialogue Node", action => CreateNode(new DialogueNode(), mousePosition));
            evt.menu.AppendAction("Add Character Setting", action => CreateNode(new CharacterNode(), mousePosition));
            evt.menu.AppendAction("Add Start Node", action => CreateNode(new StartNode(), mousePosition));
            evt.menu.AppendAction("Add End Node", action => CreateNode(new EndNode(), mousePosition));
            evt.menu.AppendAction("Add Setting/Portrait", action => CreateNode(new PortraitNode(), mousePosition));
            evt.menu.AppendAction("Add Setting/Still", action => CreateNode(new StillNode(), mousePosition));
            evt.menu.AppendAction("Add Setting/Panel", action => CreateNode(new PanelSettingNode(), mousePosition));
        }

        public DialogueNode CreateDialogueNode(string speakerName, string text, Vector2 position)
        {
            var node = new DialogueNode();
            node.SpeakerName = speakerName;
            node.DialogueText = text;

            var textFields = node.mainContainer.Query<TextField>().ToList();
            if (textFields.Count >= 2)
            {
                textFields[0].value = speakerName;
                textFields[1].value = text;
            }

            node.SetPosition(new Rect(position, new Vector2(300, 150)));
            return node;
        }

        public void AutoLayoutNodes()
        {
            var allNodes = nodes.ToList().OfType<BaseGraphNode>().ToList();
            if (allNodes.Count == 0) return;

            var startNode = allNodes.OfType<StartNode>().FirstOrDefault();
            if (startNode == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "Start Node not found. Cannot auto-layout.", "OK");
                return;
            }

            var flowDepthMap = new Dictionary<BaseGraphNode, int>();
            var queue = new Queue<BaseGraphNode>();

            flowDepthMap[startNode] = 0;
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                int d = flowDepthMap[curr];

                var flowPorts = curr.outputContainer.Children().OfType<Port>().Where(p => p.portType == typeof(FlowPort) || p.portType.Name == "FlowPort");
                foreach (var port in flowPorts)
                {
                    foreach (var edge in port.connections)
                    {
                        var target = edge.input.node as BaseGraphNode;
                        if (target == null) continue;

                        int targetD = d + 1;
                        if (!flowDepthMap.ContainsKey(target) || flowDepthMap[target] < targetD)
                        {
                            flowDepthMap[target] = targetD;
                            queue.Enqueue(target);
                        }
                    }
                }
            }

            float startX = 0;
            float startY = 0;
            float xStep = 450f;
            float yStep = 300f;

            var depthGroups = flowDepthMap.GroupBy(kvp => kvp.Value).OrderBy(g => g.Key).ToList();
            var arrangedNodes = new HashSet<BaseGraphNode>();

            foreach (var group in depthGroups)
            {
                int depth = group.Key;
                var groupNodes = group.Select(kvp => kvp.Key).ToList();

                float currentY = startY;
                foreach (var node in groupNodes)
                {
                    node.SetPosition(new Rect(new Vector2(startX + depth * xStep, currentY), Vector2.zero));
                    arrangedNodes.Add(node);
                    
                    float settingY = currentY + 150f;
                    int settingCount = 0;

                    var inputPorts = node.inputContainer.Children().OfType<Port>().Where(p => p.portType != typeof(FlowPort));
                    foreach (var inPort in inputPorts)
                    {
                        foreach (var edge in inPort.connections)
                        {
                            var settingNode = edge.output.node as BaseGraphNode;
                            if (settingNode != null && !arrangedNodes.Contains(settingNode))
                            {
                                settingNode.SetPosition(new Rect(new Vector2(startX + depth * xStep - 250f, settingY + (settingCount * 120f)), Vector2.zero));
                                arrangedNodes.Add(settingNode);
                                settingCount++;
                            }
                        }
                    }
                    
                    if (node is DialogueNode dNode && dNode.TypingSpeedInputPort != null)
                    {
                        foreach (var edge in dNode.TypingSpeedInputPort.connections)
                        {
                            var settingNode = edge.output.node as BaseGraphNode;
                            if (settingNode != null && !arrangedNodes.Contains(settingNode))
                            {
                                settingNode.SetPosition(new Rect(new Vector2(startX + depth * xStep - 250f, settingY + (settingCount * 120f)), Vector2.zero));
                                arrangedNodes.Add(settingNode);
                                settingCount++;
                            }
                        }
                    }

                    currentY += yStep + (settingCount * 120f); 
                }
            }

            var unlinkedNodes = allNodes.Where(n => !arrangedNodes.Contains(n)).ToList();
            float unlinkedX = startX;
            float unlinkedY = startY - 250f;
            foreach (var node in unlinkedNodes)
            {
                node.SetPosition(new Rect(new Vector2(unlinkedX, unlinkedY), Vector2.zero));
                unlinkedX += 300f;
                if (unlinkedX > 1500f)
                {
                    unlinkedX = startX;
                    unlinkedY -= 250f;
                }
            }
        }
    }
}