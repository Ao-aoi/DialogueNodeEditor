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
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheets.Add(styleSheet);

            graphViewChanged = OnGraphViewChanged;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    if (edge.input.node is DialogueNode dNode && edge.output.node is CharacterNode)
                    {
                        dNode.schedule.Execute(() => dNode.UpdateCharacterState()).StartingIn(10);
                    }
                }
            }

            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var elem in graphViewChange.elementsToRemove)
                {
                    if (elem is Edge edge)
                    {
                        if (edge.input.node is DialogueNode dNode && edge.output.node is CharacterNode)
                        {
                            dNode.schedule.Execute(() => dNode.UpdateCharacterState()).StartingIn(10);
                        }
                    }
                }
            }

            return graphViewChange;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach((port) =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    if (startPort.portType == port.portType)
                    {
                        compatiblePorts.Add(port);
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
            evt.menu.AppendAction("Add Setting/Panel Size", action => CreateNode(new PanelSizeNode(), mousePosition));
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

        // ★追加: 自動整列機能（Auto Layout）
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

            // 1. Calculate depths for Flow nodes
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

            // 2. Arrange flow nodes
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
                    
                    // Arrange connected input nodes (Settings)
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
                    
                    // Typing Speed の隠しポートも考慮
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

            // 3. Arrange unlinked nodes
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