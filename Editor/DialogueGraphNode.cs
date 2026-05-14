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
            foreach(var edge in _outputPort.connections)
            {
                if(edge.input.node is DialogueNode dNode) dNode.UpdateCharacterState();
            }
        }
    }

    public class DialogueNode : BaseGraphNode
    {
        public string DialogueText;
        public string SpeakerName;
        public string Expression;

        public Port CharacterInputPort;
        private TextField _speakerNameField;
        private DropdownField _expressionDropdown;

        public DialogueNode()
        {
            title = "Dialogue Node";
            style.width = 300;

            _speakerNameField = new TextField("Speaker Name");
            _speakerNameField.RegisterValueChangedCallback(evt => SpeakerName = evt.newValue);
            mainContainer.Add(_speakerNameField);

            _expressionDropdown = new DropdownField("Expression", new List<string>(), 0);
            _expressionDropdown.style.display = DisplayStyle.None;
            _expressionDropdown.RegisterValueChangedCallback(evt => Expression = evt.newValue);
            mainContainer.Add(_expressionDropdown);

            var textField = new TextField("Dialogue Text") { multiline = true };
            textField.style.minHeight = 50f;
            textField.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
            mainContainer.Add(textField);

            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FlowPort));
            inputPort.portName = "Input (Flow)";
            inputContainer.Add(inputPort);

            CharacterInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(CharacterPort));
            CharacterInputPort.portName = "Character";
            inputContainer.Add(CharacterInputPort);

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            outputPort.portName = "Next";
            outputContainer.Add(outputPort);

            var addChoiceBtn = new Button(() => AddChoicePort()) { text = "Add Choice" };
            titleButtonContainer.Add(addChoiceBtn);

            RefreshExpandedState(); RefreshPorts();
        }

        public void UpdateCharacterState()
        {
            var edges = CharacterInputPort.connections.ToList();
            if (edges.Count > 0 && edges[0].output.node is CharacterNode charNode)
            {
                _speakerNameField.style.display = DisplayStyle.None;
                SpeakerName = charNode.CharacterName;
                
                _expressionDropdown.style.display = DisplayStyle.Flex;
                var exprNames = charNode.Expressions.Select(e => e.Name).ToList();
                _expressionDropdown.choices = exprNames;
                
                if (exprNames.Count > 0 && !exprNames.Contains(Expression)) Expression = exprNames[0];
                _expressionDropdown.SetValueWithoutNotify(Expression);
            }
            else
            {
                _speakerNameField.style.display = DisplayStyle.Flex;
                _speakerNameField.value = SpeakerName; 
                _expressionDropdown.style.display = DisplayStyle.None;
                Expression = "";
            }
        }

        public void AddChoicePort(string choiceText = "New Choice")
        {
            var generatedPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            generatedPort.portName = choiceText; // 名前をポートに反映
            
            var textField = new TextField() { value = choiceText };
            textField.style.minWidth = 100;
            textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);

            var deleteButton = new Button(() => RemoveChoicePort(generatedPort)) { text = "X" };
            generatedPort.contentContainer.Add(textField); generatedPort.contentContainer.Add(deleteButton);
            outputContainer.Add(generatedPort);
            RefreshExpandedState(); RefreshPorts();
        }

        private void RemoveChoicePort(Port port)
        {
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
}