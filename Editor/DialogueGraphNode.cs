using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace DialogueNodeEditor{
    public class FlowPort {}
    public class PortraitPort {}
    public class StillPort {}
    public class PanelSizePort {}
    public class CharacterPort {}
    public class FloatPort {}
    
    public class PropertyNode : BaseGraphNode
    {
        public string PropertyName;
        public PropertyNode()
        {
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(FloatPort));
            outputPort.portName = "Output";
            outputContainer.Add(outputPort);
            RefreshExpandedState(); RefreshPorts();
        }
    }
    
    public class BaseGraphNode : Node
    {
        public string GUID;
        public BaseGraphNode()
        {
            capabilities |= Capabilities.Resizable;
            mainContainer.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 1f));
            titleContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f, 1f));
            extensionContainer.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
        }
    }

    public class CharacterNode : BaseGraphNode
    {
        public string CharacterName = "New Character";
        public List<ExpressionData> Expressions = new List<ExpressionData>();
        private VisualElement _expressionContainer;
        private Port _outputPort;

        public CharacterNode()
        {
            title = "Character Setting";
            style.width = 300;

            var nameField = new TextField("Character Name") { value = CharacterName };
            nameField.RegisterValueChangedCallback(evt => {
                CharacterName = evt.newValue;
                NotifyConnections();
            });
            mainContainer.Add(nameField);

            var addExprBtn = new Button(() => AddExpression(new ExpressionData { Name = "New Expression" })) { text = "Add Expression" };
            mainContainer.Add(addExprBtn);

            _expressionContainer = new VisualElement();
            mainContainer.Add(_expressionContainer);

            _outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(CharacterPort));
            _outputPort.portName = "Character Output";
            outputContainer.Add(_outputPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        public void LoadData(string name, List<ExpressionData> expressions)
        {
            CharacterName = name;
            mainContainer.Query<TextField>().First().SetValueWithoutNotify(name);
            _expressionContainer.Clear();
            Expressions.Clear();

            if (expressions == null || expressions.Count == 0)
            {
                AddExpression(new ExpressionData { Name = "Normal" });
            }
            else
            {
                foreach (var expr in expressions) AddExpression(expr);
            }
        }

        private void AddExpression(ExpressionData data)
        {
            var exprData = new ExpressionData { Name = data.Name, Sprite = data.Sprite };
            Expressions.Add(exprData);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2, marginBottom = 2 } };
            var nameField = new TextField() { value = exprData.Name, style = { width = 100 } };
            var spriteField = new ObjectField() { objectType = typeof(Sprite), value = exprData.Sprite, style = { flexGrow = 1 } };
            var delBtn = new Button(() => { 
                _expressionContainer.Remove(row); Expressions.Remove(exprData); NotifyConnections();
            }) { text = "X" };

            nameField.RegisterValueChangedCallback(evt => { exprData.Name = evt.newValue; NotifyConnections(); });
            spriteField.RegisterValueChangedCallback(evt => { exprData.Sprite = evt.newValue as Sprite; NotifyConnections(); });

            row.Add(nameField); row.Add(spriteField); row.Add(delBtn);
            _expressionContainer.Add(row);
            NotifyConnections();
        }

        public void NotifyConnections()
        {
            var graphView = this.GetFirstAncestorOfType<DialogueGraphView>();
            if (graphView != null)
            {
                foreach (var node in graphView.nodes.ToList().OfType<DialogueNode>())
                {
                    node.UpdateCharacterState();
                }
            }
        }
    }

    public class DialogueNode : BaseGraphNode
    {
        public string DialogueText;
        public string SpeakerName;
        public string Expression;

        public Port CharacterInputPort;
        public Port PortraitInputPort;
        public Port StillInputPort;

        private TextField _speakerNameField;
        private DropdownField _expressionDropdown;
        public DropdownField CharacterDropdown;

        public bool ShowSettings = false;
        public bool OverrideTypingSpeed = false;
        private VisualElement _settingsContainer;
        public Toggle TypingSpeedToggle;
        public FloatField TypingSpeedField;
        public float TypingSpeedValue = 0.05f;
        public Port TypingSpeedInputPort;

        public bool CanSkipTyping = true;
        public Toggle CanSkipTypingToggle;

        public DialogueNode()
        {
            title = "Dialogue Node";
            style.width = 300;
            var nodeBorder = this.Q("node-border");
            if (nodeBorder != null) nodeBorder.style.flexGrow = 1;
            
            var contents = this.Q("contents");
            if (contents != null) contents.style.flexGrow = 1;
            
            var top = this.Q("top");
            if (top != null) top.style.flexGrow = 1;

            mainContainer.style.flexGrow = 1;

            CharacterDropdown = new DropdownField("Character", new List<string> { "None (Custom)" }, "None (Custom)");
            CharacterDropdown.RegisterValueChangedCallback(evt => UpdateCharacterState());
            mainContainer.Add(CharacterDropdown);

            _speakerNameField = new TextField("Speaker Name");
            _speakerNameField.RegisterValueChangedCallback(evt => SpeakerName = evt.newValue);
            mainContainer.Add(_speakerNameField);

            _expressionDropdown = new DropdownField("Expression", new List<string>(), 0);
            _expressionDropdown.style.display = DisplayStyle.None;
            _expressionDropdown.RegisterValueChangedCallback(evt => Expression = evt.newValue);
            mainContainer.Add(_expressionDropdown);

            var textField = new TextField() { multiline = true };
            textField.style.minHeight = 50f;
            textField.style.flexGrow = 1;
            
            var inputElement = textField.Q("unity-text-input");
            if (inputElement != null)
            {
                inputElement.style.flexGrow = 1;
                inputElement.style.unityTextAlign = TextAnchor.UpperLeft;
                inputElement.style.whiteSpace = WhiteSpace.Normal;
            }

            textField.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
            mainContainer.Add(textField);

            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FlowPort));
            inputPort.portName = "Input (Flow)";
            inputContainer.Add(inputPort);

            CharacterInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(CharacterPort));
            CharacterInputPort.portName = "Character";
            inputContainer.Add(CharacterInputPort);

            PortraitInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(PortraitPort));
            PortraitInputPort.portName = "Portrait";
            inputContainer.Add(PortraitInputPort);

            StillInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(StillPort));
            StillInputPort.portName = "Still";
            inputContainer.Add(StillInputPort);

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            outputPort.portName = "Next";
            outputContainer.Add(outputPort);

            var addChoiceBtn = new Button(() => AddChoicePort()) { text = "Add Choice" };
            titleButtonContainer.Add(addChoiceBtn);
            
            CreateSettingsUI();
            RefreshExpandedState(); RefreshPorts();
        }

        private void CreateSettingsUI()
        {
            _settingsContainer = new VisualElement();
            _settingsContainer.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
            _settingsContainer.style.paddingTop = 5; _settingsContainer.style.paddingBottom = 5;
            _settingsContainer.style.paddingLeft = 5; _settingsContainer.style.paddingRight = 5;
            _settingsContainer.style.display = DisplayStyle.None;

            var title = new Label("Advanced Settings") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            _settingsContainer.Add(title);

            CanSkipTypingToggle = new Toggle("Can Skip Typing") { value = CanSkipTyping };
            CanSkipTypingToggle.RegisterValueChangedCallback(evt => CanSkipTyping = evt.newValue);
            _settingsContainer.Add(CanSkipTypingToggle);

            var speedRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            
            TypingSpeedToggle = new Toggle("Type Speed");
            TypingSpeedToggle.RegisterValueChangedCallback(evt => {
                OverrideTypingSpeed = evt.newValue;
                TypingSpeedInputPort.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            speedRow.Add(TypingSpeedToggle);

            TypingSpeedInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(FloatPort));
            TypingSpeedInputPort.portName = ""; 
            TypingSpeedInputPort.style.display = DisplayStyle.None;

            TypingSpeedField = new FloatField() { value = TypingSpeedValue, style = { width = 40 } };
            TypingSpeedField.RegisterValueChangedCallback(evt => { TypingSpeedValue = evt.newValue; });
            TypingSpeedInputPort.contentContainer.Add(TypingSpeedField);

            speedRow.Add(TypingSpeedInputPort);
            _settingsContainer.Add(speedRow);
            
            extensionContainer.Add(_settingsContainer);
        }

        public void UpdateCharacterState()
        {
            var graphView = this.GetFirstAncestorOfType<DialogueGraphView>();
            List<CharacterNode> charNodes = new List<CharacterNode>();
            if (graphView != null) charNodes = graphView.nodes.ToList().OfType<CharacterNode>().ToList();

            var charNames = new List<string> { "None (Custom)" };
            charNames.AddRange(charNodes.Select(c => c.CharacterName));
            CharacterDropdown.choices = charNames;

            if (!charNames.Contains(CharacterDropdown.value)) CharacterDropdown.SetValueWithoutNotify("None (Custom)");

            var edges = CharacterInputPort.connections.ToList();
            if (edges.Count > 0 && edges[0].output.node is CharacterNode connectedCharNode)
            {
                CharacterDropdown.style.display = DisplayStyle.None;
                _speakerNameField.style.display = DisplayStyle.None;
                SpeakerName = connectedCharNode.CharacterName;
                
                _expressionDropdown.style.display = DisplayStyle.Flex;
                var exprNames = connectedCharNode.Expressions.Select(e => e.Name).ToList();
                _expressionDropdown.choices = exprNames;
                if (exprNames.Count > 0 && !exprNames.Contains(Expression)) Expression = exprNames[0];
                _expressionDropdown.SetValueWithoutNotify(Expression);
            }
            else
            {
                CharacterDropdown.style.display = DisplayStyle.Flex;
                
                if (CharacterDropdown.value == "None (Custom)")
                {
                    _speakerNameField.style.display = DisplayStyle.Flex;
                    _speakerNameField.value = SpeakerName; 
                    _expressionDropdown.style.display = DisplayStyle.None;
                    Expression = "";
                }
                else
                {
                    _speakerNameField.style.display = DisplayStyle.None;
                    SpeakerName = CharacterDropdown.value;
                    
                    var selectedCharNode = charNodes.FirstOrDefault(c => c.CharacterName == CharacterDropdown.value);
                    if (selectedCharNode != null)
                    {
                        _expressionDropdown.style.display = DisplayStyle.Flex;
                        var exprNames = selectedCharNode.Expressions.Select(e => e.Name).ToList();
                        _expressionDropdown.choices = exprNames;
                        if (exprNames.Count > 0 && !exprNames.Contains(Expression)) Expression = exprNames[0];
                        _expressionDropdown.SetValueWithoutNotify(Expression);
                    }
                }
            }
        }

        public void AddChoicePort(string choiceText = "New Choice")
        {
            var generatedPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            generatedPort.portName = choiceText;
            
            var textField = new TextField() { value = choiceText };
            textField.style.minWidth = 100;
            textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);

            var deleteButton = new Button(() => RemoveChoicePort(generatedPort)) { text = "X" };
            generatedPort.contentContainer.Add(textField); generatedPort.contentContainer.Add(deleteButton);
            outputContainer.Add(generatedPort);
            RefreshExpandedState(); RefreshPorts();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction(ShowSettings ? "Hide Advanced Settings" : "Show Advanced Settings", a => ToggleSettings());
            // ★追加: タイピング設定の一括適用
            evt.menu.AppendAction("Apply Typing Settings to All Nodes", a => ApplyTypingSettingsToAll());
        }

        // ★追加: 設定一括適用メソッド
        private void ApplyTypingSettingsToAll()
        {
            var graphView = this.GetFirstAncestorOfType<DialogueGraphView>();
            if (graphView != null)
            {
                var allDialogueNodes = graphView.nodes.ToList().OfType<DialogueNode>();
                int count = 0;
                foreach (var node in allDialogueNodes)
                {
                    if (node == this) continue;

                    node.CanSkipTyping = this.CanSkipTyping;
                    node.CanSkipTypingToggle.SetValueWithoutNotify(this.CanSkipTyping);
                    
                    node.OverrideTypingSpeed = this.OverrideTypingSpeed;
                    node.TypingSpeedToggle.SetValueWithoutNotify(this.OverrideTypingSpeed);
                    
                    node.TypingSpeedValue = this.TypingSpeedField.value;
                    node.TypingSpeedField.SetValueWithoutNotify(this.TypingSpeedField.value);
                    
                    node.TypingSpeedInputPort.style.display = this.OverrideTypingSpeed ? DisplayStyle.Flex : DisplayStyle.None;
                    count++;
                }
                UnityEditor.EditorUtility.DisplayDialog("Applied", $"Applied typing settings to {count} nodes.", "OK");
            }
        }

        public void ToggleSettings()
        {
            ShowSettings = !ShowSettings;
            UpdateSettingsUI();
        }

        public void UpdateSettingsUI()
        {
            _settingsContainer.style.display = ShowSettings ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshExpandedState();
        }

        private void RemoveChoicePort(Port port)
        {
            if (port.connections.Any())
            {
                var graphView = port.GetFirstAncestorOfType<DialogueGraphView>();
                if (graphView != null)
                {
                    var edgesToDelete = port.connections.ToList();
                    graphView.DeleteElements(edgesToDelete);
                }
            }
            outputContainer.Remove(port);
            RefreshExpandedState(); RefreshPorts();
        }
    }

    public class PortraitNode : BaseGraphNode
    {
        public Sprite PortraitImage;
        public PortraitNode()
        {
            title = "Portrait Setting";
            var portraitField = new ObjectField("Portrait") { objectType = typeof(Sprite) };
            portraitField.RegisterValueChangedCallback(evt => PortraitImage = evt.newValue as Sprite);
            mainContainer.Add(portraitField);
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(PortraitPort));
            outputPort.portName = "Output"; outputContainer.Add(outputPort);
            RefreshExpandedState(); RefreshPorts();
        }
        public void LoadData(Sprite sprite)
        {
            PortraitImage = sprite;
            mainContainer.Query<ObjectField>().First().SetValueWithoutNotify(sprite);
        }
    }

    public class StillNode : BaseGraphNode
    {
        public Sprite StillImage; public bool ShouldScrollStill; public float ScrollAmount, ScrollSpeed;
        public StillNode()
        {
            title = "Still Setting";
            var stillField = new ObjectField("Still Image") { objectType = typeof(Sprite) };
            stillField.RegisterValueChangedCallback(evt => StillImage = evt.newValue as Sprite);
            mainContainer.Add(stillField);
            var scrollToggle = new Toggle("Should Scroll"); scrollToggle.RegisterValueChangedCallback(evt => ShouldScrollStill = evt.newValue); mainContainer.Add(scrollToggle);
            var scrollAmountField = new FloatField("Scroll Amount"); scrollAmountField.RegisterValueChangedCallback(evt => ScrollAmount = evt.newValue); mainContainer.Add(scrollAmountField);
            var scrollSpeedField = new FloatField("Scroll Speed"); scrollSpeedField.RegisterValueChangedCallback(evt => ScrollSpeed = evt.newValue); mainContainer.Add(scrollSpeedField);
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(StillPort));
            outputPort.portName = "Output"; outputContainer.Add(outputPort);
            RefreshExpandedState(); RefreshPorts();
        }
        public void LoadData(Sprite sprite, bool scroll, float amount, float speed)
        {
            StillImage = sprite; ShouldScrollStill = scroll; ScrollAmount = amount; ScrollSpeed = speed;
            mainContainer.Query<ObjectField>().First().SetValueWithoutNotify(sprite);
            mainContainer.Query<Toggle>().First().SetValueWithoutNotify(scroll);
            var floatFields = mainContainer.Query<FloatField>().ToList();
            floatFields[0].SetValueWithoutNotify(amount); floatFields[1].SetValueWithoutNotify(speed);
        }
    }

    public class PanelSizeNode : BaseGraphNode
    {
        public float PanelWidth = 230f;
        public PanelSizeNode()
        {
            title = "Panel Size Setting";
            var panelWidthField = new FloatField("Panel Width") { value = PanelWidth };
            panelWidthField.RegisterValueChangedCallback(evt => PanelWidth = evt.newValue); mainContainer.Add(panelWidthField);
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(PanelSizePort));
            outputPort.portName = "Output"; outputContainer.Add(outputPort);
            RefreshExpandedState(); RefreshPorts();
        }
        public void LoadData(float width)
        {
            PanelWidth = width; mainContainer.Query<FloatField>().First().SetValueWithoutNotify(width);
        }
    }

    public class EndNode : BaseGraphNode
    {
        public EndNode()
        {
            title = "End Node";
            style.width = 150;

            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FlowPort));
            inputPort.portName = "Input";
            inputContainer.Add(inputPort);

            RefreshExpandedState();
            RefreshPorts();
        }
    }

    public class StartNode : BaseGraphNode
    {
        public StartNode()
        {
            title = "Start";
            style.width = 100;

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            outputPort.portName = "Start";
            outputContainer.Add(outputPort);

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}