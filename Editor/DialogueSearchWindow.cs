using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace DialogueNodeEditor
{
    public class DialogueSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView _graphView;
        private EditorWindow _window;
        private Port _sourcePort; // ★追加: ドラッグ元のポートを保持

        public void Init(EditorWindow window, DialogueGraphView graphView, Port sourcePort)
        {
            _window = window;
            _graphView = graphView;
            _sourcePort = sourcePort; // ★追加
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
                
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 1),
                new SearchTreeEntry(new GUIContent("Dialogue Node")) { userData = new DialogueNode(), level = 2 },
                new SearchTreeEntry(new GUIContent("Character Setting")) { userData = new CharacterNode(), level = 2 },
                new SearchTreeEntry(new GUIContent("Start Node")) { userData = new StartNode(), level = 2 },
                new SearchTreeEntry(new GUIContent("End Node")) { userData = new EndNode(), level = 2 },
                
                new SearchTreeGroupEntry(new GUIContent("Settings"), 1),
                new SearchTreeEntry(new GUIContent("Portrait Setting")) { userData = new PortraitNode(), level = 2 },
                new SearchTreeEntry(new GUIContent("Still Setting")) { userData = new StillNode(), level = 2 },
                new SearchTreeEntry(new GUIContent("Panel Setting")) { userData = new PanelSettingNode(), level = 2 },
                
                new SearchTreeGroupEntry(new GUIContent("Properties"), 1)
            };

            foreach (var prop in _graphView.ExposedProperties)
            {
                tree.Add(new SearchTreeEntry(new GUIContent(prop.PropertyName)) 
                { 
                    userData = new PropertyNode { PropertyName = prop.PropertyName, title = prop.PropertyName }, 
                    level = 2 
                });
            }
            
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var windowMousePosition = _window.rootVisualElement.ChangeCoordinatesTo(
                _window.rootVisualElement.parent, 
                context.screenMousePosition - _window.position.position);
            var graphMousePosition = _graphView.contentViewContainer.WorldToLocal(windowMousePosition);

            if (SearchTreeEntry.userData is BaseGraphNode node)
            {
                _graphView.CreateNode(node, graphMousePosition);

                // ★追加: 引っ張ってきた線があれば自動で繋ぐ
                if (_sourcePort != null)
                {
                    Port targetPort = null;
                    if (_sourcePort.direction == Direction.Output)
                    {
                        // 引っ張ってきたポートがOutputなら、新しいノードのInputを探す
                        targetPort = node.inputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portType == _sourcePort.portType);
                    }
                    else
                    {
                        // 引っ張ってきたポートがInputなら、新しいノードのOutputを探す
                        targetPort = node.outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portType == _sourcePort.portType);
                    }

                    if (targetPort != null)
                    {
                        var newEdge = new Edge { 
                            output = _sourcePort.direction == Direction.Output ? _sourcePort : targetPort, 
                            input = _sourcePort.direction == Direction.Input ? _sourcePort : targetPort 
                        };
                        newEdge.input.Connect(newEdge);
                        newEdge.output.Connect(newEdge);
                        _graphView.AddElement(newEdge);
                    }
                }

                return true;
            }
            return false;
        }
    }
}