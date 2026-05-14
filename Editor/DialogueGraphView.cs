using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace DialogueNodeEditor
{
    public class DialogueGraphView : GraphView
    {
        public DialogueGraphView(DialogueGraphWindow editorWindow)
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheets.Add(styleSheet);
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

        // Publicに変更して外部からノードを生成できるようにする
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
            evt.menu.AppendAction("Add Setting/Portrait", action => CreateNode(new PortraitNode(), mousePosition));
            evt.menu.AppendAction("Add Setting/Still", action => CreateNode(new StillNode(), mousePosition));
            evt.menu.AppendAction("Add Setting/Panel Size", action => CreateNode(new PanelSizeNode(), mousePosition));
        }

        // GraphSaveUtilityのロード処理で呼ばれる
        public DialogueNode CreateDialogueNode(string speakerName, string text, Vector2 position)
        {
            var node = new DialogueNode();
            node.SpeakerName = speakerName;
            node.DialogueText = text;

            // UIコンポーネント（TextField）にロードした値を反映
            var textFields = node.mainContainer.Query<TextField>().ToList();
            if (textFields.Count >= 2)
            {
                textFields[0].value = speakerName;
                textFields[1].value = text;
            }

            node.SetPosition(new Rect(position, new Vector2(300, 150)));
            return node;
        }
    }
}