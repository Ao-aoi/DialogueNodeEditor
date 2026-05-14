using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace DialogueNodeEditor{
// ポートの接続制限用のダミー型
public class FlowPort {}      // ノードの遷移用
public class PortraitPort {}  // 立ち絵用
public class StillPort {}     // スチル用
public class PanelSizePort {} // パネルサイズ用

    // 共通のベースノード
    public class BaseGraphNode : Node
    {
        public string GUID;
    }

    // 1. ダイアログノード
    public class DialogueNode : BaseGraphNode
    {
        public string DialogueText;
        public string SpeakerName;

        public DialogueNode()
        {
            title = "Dialogue Node";
            style.width = 300;

            var speakerNameField = new TextField("Speaker Name");
            speakerNameField.RegisterValueChangedCallback(evt => SpeakerName = evt.newValue);
            mainContainer.Add(speakerNameField);

            var textField = new TextField("Dialogue Text") { multiline = true };
            textField.style.minHeight = 50f;
            textField.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
            mainContainer.Add(textField);

            // 【入力ポートの作成】
            // メインの遷移用入力
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FlowPort));
            inputPort.portName = "Input (Flow)";
            inputContainer.Add(inputPort);

            // 各設定用の入力ポート
            var portraitInput = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(PortraitPort));
            portraitInput.portName = "Portrait Setting";
            inputContainer.Add(portraitInput);

            var stillInput = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(StillPort));
            stillInput.portName = "Still Setting";
            inputContainer.Add(stillInput);

            var panelSizeInput = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(PanelSizePort));
            panelSizeInput.portName = "Panel Size Setting";
            inputContainer.Add(panelSizeInput);

            // デフォルトの出力ポート
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FlowPort));
            outputPort.portName = "Next";
            outputContainer.Add(outputPort);

            var addChoiceBtn = new Button(() => AddChoicePort()) { text = "Add Choice" };
            titleButtonContainer.Add(addChoiceBtn);

            RefreshExpandedState();
            RefreshPorts();
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
    }
}