using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueNodeEditor
{
    public class DialogueSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView _graphView;
        private EditorWindow _window;

        public void Init(EditorWindow window, DialogueGraphView graphView)
        {
            _window = window;
            _graphView = graphView;
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
                new SearchTreeEntry(new GUIContent("Panel Size Setting")) { userData = new PanelSizeNode(), level = 2 },
                
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
                return true;
            }
            return false;
        }
    }
}