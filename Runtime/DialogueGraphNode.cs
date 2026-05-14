using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
namespace DialogueNodeEditor{
public class DialogueGraphNode : Node
{
    public string GUID;
    public string DialogueText;
    public string SpeakerName;
    public float PanelWidth = 230f;
    public bool NodeShouldScrollStill;
    public Sprite StillImage;       // スチル設定
    public Sprite PortraitImage;    // 立ち絵設定
    public Sprite BackgroundImage;  // ダイアログ背景

    // ノード生成時にUI（テキストボックスなど）を構築する
    public DialogueGraphNode()
    {
        style.width = 300;
        // 1. スピーカー名の入力フィールド
        var speakerNameField = new TextField("Speaker Name");
        speakerNameField.RegisterValueChangedCallback(evt => SpeakerName = evt.newValue);
        mainContainer.Add(speakerNameField);

        // 2. テキスト本文の入力フィールド（複数行）
        var textField = new TextField("Dialogue Text");
        textField.multiline = true;
        textField.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
        textField.style.minHeight = 50f; // ShaderGraphのプレビューっぽく広げる
        mainContainer.Add(textField);

        // 3. パネル横幅などの細かいパラメータ
        var panelWidthField = new FloatField("Panel Width");
        panelWidthField.value = PanelWidth;
        panelWidthField.RegisterValueChangedCallback(evt => PanelWidth = evt.newValue);
        mainContainer.Add(panelWidthField);

        var scrollToggle = new Toggle("Should Scroll Still");
        scrollToggle.value = NodeShouldScrollStill;
        scrollToggle.RegisterValueChangedCallback(evt => NodeShouldScrollStill = evt.newValue);
        mainContainer.Add(scrollToggle);

        var propertiesFoldout = new Foldout() { text = "Node Properties" };

        // 背景設定 (ObjectFieldを使用するとテクスチャをドラッグ&ドロップできるようになります)
        var bgField = new ObjectField("Background") { objectType = typeof(Sprite), value = BackgroundImage };
        bgField.RegisterValueChangedCallback(evt => BackgroundImage = evt.newValue as Sprite);
        propertiesFoldout.Add(bgField);

        // スチル設定
        var stillField = new ObjectField("Still Image") { objectType = typeof(Sprite), value = StillImage };
        stillField.RegisterValueChangedCallback(evt => StillImage = evt.newValue as Sprite);
        propertiesFoldout.Add(stillField);

        // 立ち絵設定
        var portraitField = new ObjectField("Portrait") { objectType = typeof(Sprite), value = PortraitImage };
        portraitField.RegisterValueChangedCallback(evt => PortraitImage = evt.newValue as Sprite);
        propertiesFoldout.Add(portraitField);

        // boolean変数の設定 (Toggleを使用)
        scrollToggle = new Toggle("Scroll Still Image") { value = NodeShouldScrollStill };
        scrollToggle.RegisterValueChangedCallback(evt => NodeShouldScrollStill = evt.newValue);
        propertiesFoldout.Add(scrollToggle);

        // extensionContainer に入れると、ノード下部に綺麗に区切られて表示されます
        extensionContainer.Add(propertiesFoldout);
    }

    public void AddChoicePort(string choiceText = "New Choice")
    {
        // 新しい出力ポートを生成（typeof(bool)はポートの色を変えるためのダミー型です）
        var generatedPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        generatedPort.portName = ""; // デフォルトの名前は消す

        // 選択肢のテキストを入力するフィールドをポートに埋め込む
        var textField = new TextField() { value = choiceText };
        textField.style.minWidth = 100;
        
        // 削除ボタン (×ボタン)
        var deleteButton = new Button(() => RemoveChoicePort(generatedPort)) { text = "X" };

        // ポート内にテキスト枠と削除ボタンを横並びで配置
        generatedPort.contentContainer.Add(textField);
        generatedPort.contentContainer.Add(deleteButton);

        outputContainer.Add(generatedPort);
        RefreshExpandedState(); // ノードの見た目を再構築
        RefreshPorts();
    }

    // ポートの削除処理
    private void RemoveChoicePort(Port port)
    {
        // ※本格的に実装する場合は、ここに「繋がっている線を消す」処理も追加します
        outputContainer.Remove(port);
        RefreshExpandedState();
        RefreshPorts();
    }
}
}