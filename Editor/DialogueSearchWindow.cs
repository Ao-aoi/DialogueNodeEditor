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

    // 検索ウィンドウのメニュー構造を作成
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeGroupEntry(new GUIContent("Dialogue"), 1),
            new SearchTreeEntry(new GUIContent("Dialogue Node"))
            {
                userData = new DialogueGraphNode(), level = 2
            }
        };
        return tree;
    }

    // 項目が選択されたときの処理
    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        // マウス座標をGraphViewのローカル座標系に変換
        var windowMousePosition = _window.rootVisualElement.ChangeCoordinatesTo(
            _window.rootVisualElement.parent, 
            context.screenMousePosition - _window.position.position);
        var graphMousePosition = _graphView.contentViewContainer.WorldToLocal(windowMousePosition);

        switch (SearchTreeEntry.userData)
        {
            case DialogueGraphNode _:
                _graphView.CreateNode("Dialogue Node", graphMousePosition);
                return true;
        }
        return false;
    }
}
}