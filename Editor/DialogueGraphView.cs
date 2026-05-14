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

            // グラフ内の変更（線の接続・切断など）を監視するコールバックを登録
            graphViewChanged = OnGraphViewChanged;
        }

        // ノードの接続・切断を検知してUIを更新する
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    if (edge.input.node is DialogueNode dNode && edge.output.node is CharacterNode)
                    {
                        // 接続直後に処理をスケジュールする
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
    }
}