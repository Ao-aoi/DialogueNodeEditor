using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;
namespace DialogueNodeEditor
{
public class DialogueGraphView : GraphView
{
    public DialogueGraphView(DialogueGraphWindow editorWindow)
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        // 背景のグリッドを追加
        Insert(0, new GridBackground());

        // ノードのドラッグやパン操作を有効化
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // スタイルシート（CSS）で背景の見た目をShaderGraph風の暗いグリッドにする（任意設定）
        var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
        styleSheets.Add(styleSheet);
    }

    // ノード同士を繋げるルールの設定
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach((port) =>
        {
            // 同じノード同士、または同じ入出力方向（Input同士など）は繋げない
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    // ノード生成関数
    public void CreateNode(string nodeName, Vector2 position = default)
    {
        var dialogueNode = new DialogueGraphNode
        {
            title = nodeName,
            DialogueText = "New Text",
            GUID = System.Guid.NewGuid().ToString()
        };

        // 入力ポート（左側：前のノードから）
        var inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        dialogueNode.inputContainer.Add(inputPort);

        // 出力ポート（右側：次のノードへ）
        var nextNodePort = GeneratePort(dialogueNode, Direction.Output, Port.Capacity.Single);
        nextNodePort.portName = "Next Node";
        dialogueNode.outputContainer.Add(nextNodePort);

        var addChoiceButton = new Button(() => dialogueNode.AddChoicePort()) { text = "Add Choice" };
        dialogueNode.titleButtonContainer.Add(addChoiceButton);

        // デフォルトの「次へ(Next)」ポートを追加
        var defaultNextPort = dialogueNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        defaultNextPort.portName = "Next (Default)";
        dialogueNode.outputContainer.Add(defaultNextPort);
        
        // UIを更新してグラフに配置
        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
        
        // 引数の position を使って位置を設定
        dialogueNode.SetPosition(new Rect(position, new Vector2(300, 150)));

        AddElement(dialogueNode);
    }

    private Port GeneratePort(DialogueGraphNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float)); 
        // typeof(float)はダミーです。見た目の色を変えるために任意の型を使えます。
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        
        // "Add Dialogue Node" というメニューを追加
        evt.menu.AppendAction("Add Dialogue Node", action => 
        {
            // マウスのローカル座標を取得してノード生成
            var mousePosition = contentViewContainer.WorldToLocal(action.eventInfo.mousePosition);
            CreateNode("Dialogue Node", mousePosition);
        });
    }

    public DialogueGraphNode CreateDialogueNode(string speakerName, string text, Vector2 position)
    {
        var node = new DialogueGraphNode
        {
            title = "Dialogue Node",
            SpeakerName = speakerName,
            DialogueText = text,
            GUID = System.Guid.NewGuid().ToString()
        };

        // UIフィールドにも値を反映させる処理が必要な場合はここで行います
        // （DialogueGraphNodeのコンストラクタでフィールドを作成しているため、値の同期処理を追加するとより良くなります）

        var inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        node.inputContainer.Add(inputPort);

        var nextNodePort = GeneratePort(node, Direction.Output, Port.Capacity.Single);
        nextNodePort.portName = "Next Node";
        node.outputContainer.Add(nextNodePort);

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(position, new Vector2(300, 150)));

        return node;
    }
}
}