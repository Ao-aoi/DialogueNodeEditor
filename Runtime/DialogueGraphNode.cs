using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace DialogueNodeEditor{
public class DialogueGraphNode : Node
{
    public string GUID;
    public string DialogueText;
    public string SpeakerName;
    public float PanelWidth = 230f;
    public bool NodeShouldScrollStill = false;

    // ノード生成時にUI（テキストボックスなど）を構築する
    public DialogueGraphNode()
    {
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
    }
}
}