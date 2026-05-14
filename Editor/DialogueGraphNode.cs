using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace DialogueNodeEditor{
    public class FlowPort {}      // ノードの遷移用
    public class PortraitPort {}  // 立ち絵用
    public class StillPort {}     // スチル用
    public class PanelSizePort {} // パネルサイズ用
    public class CharacterPort {} // キャラクター設定用

    // 共通のベースノード
    public class BaseGraphNode : Node
    {
        public string GUID;

        public BaseGraphNode()
        {
            // ノードのサイズ変更（リサイズ）を可能にする
            capabilities |= Capabilities.Resizable;

            // 背景色を不透明（アルファ値 1.0）に設定する
            mainContainer.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 1f));
            titleContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f, 1f));
            extensionContainer.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
        }
    }

    // 新規追加：キャラクターノード
    public class CharacterNode : BaseGraphNode
    {
        public string CharacterName = "New Character";
        public List<string> ExpressionList = new List<string>();
        private VisualElement _expressionContainer;
        private Port _outputPort;

        public CharacterNode()
        {
            title = "Character Setting";
            style.width = 300;

            var nameField = new TextField("Character Name") { value = CharacterName };
            nameField.RegisterValueChangedCallback(evt => {
                CharacterName = evt.newValue;
                NotifyConnections(); // 名前が変わったら接続先のダイアログノードに通知
            });
            mainContainer.Add(nameField);

            var addExprBtn = new Button(() => AddExpression("New Expression")) { text = "Add Expression" };
            mainContainer.Add(addExprBtn);

            _expressionContainer = new VisualElement();
            mainContainer.Add(_expressionContainer);

            _outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(CharacterPort));
            _outputPort.portName = "Character Output";
            outputContainer.Add(_outputPort);

            // 初期状態としてNormalをひとつ入れておく
            AddExpression("Normal");

            RefreshExpandedState();
            RefreshPorts();
        }
        public void LoadData(string name, List<string> expressions)
        {
            CharacterName = name;
            var nameField = mainContainer.Query<TextField>().First();
            nameField.SetValueWithoutNotify(name); // イベントを多重発火させない

            _expressionContainer.Clear();
            ExpressionList.Clear();

            foreach (var expr in expressions)
            {
                AddExpression(expr);
            }
        }

        private void AddExpression(string exprName)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2, marginBottom = 2 } };
            
            var nameField = new TextField() { value = exprName, style = { width = 100 } };
            var spriteField = new ObjectField() { objectType = typeof(Sprite), style = { flexGrow = 1 } };
            var delBtn = new Button(() => { 
                _expressionContainer.Remove(row); 
                ExpressionList.Remove(nameField.value); 
                NotifyConnections();
            }) { text = "X" };

            nameField.RegisterValueChangedCallback(evt => {
                int idx = ExpressionList.IndexOf(evt.previousValue);
                if (idx >= 0) ExpressionList[idx] = evt.newValue;
                NotifyConnections();
            });

            ExpressionList.Add(exprName);

            row.Add(nameField);
            row.Add(spriteField);
            row.Add(delBtn);
            _expressionContainer.Add(row);

            NotifyConnections();
        }

        // 接続されているすべてのDialogueNodeに更新を通知する
        public void NotifyConnections()
        {
            foreach(var edge in _outputPort.connections)
            {
                if(edge.input.node is DialogueNode dNode)
                {
                    dNode.UpdateCharacterState();
                }
            }
        }
    }

    // ダイアログノード
    public class DialogueNode : BaseGraphNode
    {
        public string DialogueText;
        public string SpeakerName;

        public Port CharacterInputPort;
        private TextField _speakerNameField;
        private DropdownField _expressionDropdown;

        public DialogueNode()
        {
            title = "Dialogue Node";
            style.width = 300;

            // スピーカー名入力欄
            _speakerNameField = new TextField("Speaker Name");
            _speakerNameField.RegisterValueChangedCallback(evt => SpeakerName = evt.newValue);
            mainContainer.Add(_speakerNameField);

            // キャラクターノード接続時用の表情プルダウン（初期状態は非表示）
            _expressionDropdown = new DropdownField("Expression", new List<string>(), 0);
            _expressionDropdown.style.display = DisplayStyle.None;
            mainContainer.Add(_expressionDropdown);

            // ダイアログ入力欄
            var textField = new TextField("Dialogue Text") { multiline = true };
            textField.style.minHeight = 50f;
            textField.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
            mainContainer.Add(textField);

            // 入力ポート
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FlowPort));
            inputPort.portName = "Input (Flow)";
            inputContainer.Add(inputPort);

            // キャラクター入力ポート
            CharacterInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(CharacterPort));
            CharacterInputPort.portName = "Character";
            inputContainer.Add(CharacterInputPort);

            // 出力ポート
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            outputPort.portName = "Next";
            outputContainer.Add(outputPort);

            var addChoiceBtn = new Button(() => AddChoicePort()) { text = "Add Choice" };
            titleButtonContainer.Add(addChoiceBtn);

            RefreshExpandedState();
            RefreshPorts();
        }

        // キャラクターノードが繋がれた／外された時、または値が更新された時に呼ばれる処理
        public void UpdateCharacterState()
        {
            var edges = CharacterInputPort.connections.ToList();
            if (edges.Count > 0)
            {
                // キャラクターノードが繋がっている場合
                if (edges[0].output.node is CharacterNode charNode)
                {
                    // 1. 手入力のスピーカー名を隠して自動セット
                    _speakerNameField.style.display = DisplayStyle.None;
                    SpeakerName = charNode.CharacterName;
                    
                    // 2. 表情プルダウンを表示してリストを同期
                    _expressionDropdown.style.display = DisplayStyle.Flex;
                    _expressionDropdown.choices = charNode.ExpressionList;
                    
                    // もし現在の選択肢がリストに無ければ最初のものをセットする
                    if (_expressionDropdown.choices.Count > 0 && !_expressionDropdown.choices.Contains(_expressionDropdown.value))
                    {
                        _expressionDropdown.value = _expressionDropdown.choices[0];
                    }
                }
            }
            else
            {
                // キャラクターノードが外された場合、手入力に戻す
                _speakerNameField.style.display = DisplayStyle.Flex;
                _speakerNameField.value = SpeakerName; // 現在保持している名前をセット
                _expressionDropdown.style.display = DisplayStyle.None;
            }
        }

        public void AddChoicePort(string choiceText = "New Choice")
        {
            var generatedPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            generatedPort.portName = "";
            var textField = new TextField() { value = choiceText };
            textField.style.minWidth = 100;
            var deleteButton = new Button(() => RemoveChoicePort(generatedPort)) { text = "X" };
            generatedPort.contentContainer.Add(textField);
            generatedPort.contentContainer.Add(deleteButton);
            outputContainer.Add(generatedPort);
            RefreshExpandedState();
            RefreshPorts();
        }

        private void RemoveChoicePort(Port port)
        {
            outputContainer.Remove(port);
            RefreshExpandedState();
            RefreshPorts();
        }
    }

    // 2. 立ち絵設定ノード
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
            outputPort.portName = "Output";
            outputContainer.Add(outputPort);

            RefreshExpandedState();
            RefreshPorts();
        }
        public void LoadData(Sprite sprite)
        {
            PortraitImage = sprite;
            var objField = mainContainer.Query<ObjectField>().First();
            objField.SetValueWithoutNotify(sprite);
        }
    }

    // 3. スチル設定ノード
    public class StillNode : BaseGraphNode
    {
        public Sprite StillImage;
        public bool ShouldScrollStill;
        public float ScrollAmount;
        public float ScrollSpeed;

        public StillNode()
        {
            title = "Still Setting";
            var stillField = new ObjectField("Still Image") { objectType = typeof(Sprite) };
            stillField.RegisterValueChangedCallback(evt => StillImage = evt.newValue as Sprite);
            mainContainer.Add(stillField);

            var scrollToggle = new Toggle("Should Scroll");
            scrollToggle.RegisterValueChangedCallback(evt => ShouldScrollStill = evt.newValue);
            mainContainer.Add(scrollToggle);

            var scrollAmountField = new FloatField("Scroll Amount");
            scrollAmountField.RegisterValueChangedCallback(evt => ScrollAmount = evt.newValue);
            mainContainer.Add(scrollAmountField);

            var scrollSpeedField = new FloatField("Scroll Speed");
            scrollSpeedField.RegisterValueChangedCallback(evt => ScrollSpeed = evt.newValue);
            mainContainer.Add(scrollSpeedField);

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(StillPort));
            outputPort.portName = "Output";
            outputContainer.Add(outputPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        public void LoadData(Sprite sprite, bool scroll, float amount, float speed)
        {
            StillImage = sprite;
            ShouldScrollStill = scroll;
            ScrollAmount = amount;
            ScrollSpeed = speed;

            mainContainer.Query<ObjectField>().First().SetValueWithoutNotify(sprite);
            mainContainer.Query<Toggle>().First().SetValueWithoutNotify(scroll);
            var floatFields = mainContainer.Query<FloatField>().ToList();
            floatFields[0].SetValueWithoutNotify(amount);
            floatFields[1].SetValueWithoutNotify(speed);
        }
    }

    // 4. パネルサイズ設定ノード
    public class PanelSizeNode : BaseGraphNode
    {
        public float PanelWidth = 230f;

        public PanelSizeNode()
        {
            title = "Panel Size Setting";
            var panelWidthField = new FloatField("Panel Width") { value = PanelWidth };
            panelWidthField.RegisterValueChangedCallback(evt => PanelWidth = evt.newValue);
            mainContainer.Add(panelWidthField);

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(PanelSizePort));
            outputPort.portName = "Output";
            outputContainer.Add(outputPort);

            RefreshExpandedState();
            RefreshPorts();
        }
        public void LoadData(float width)
        {
            PanelWidth = width;
            mainContainer.Query<FloatField>().First().SetValueWithoutNotify(width);
        }
    }
}