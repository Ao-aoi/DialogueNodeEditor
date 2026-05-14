using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace DialogueNodeEditor
{
    public class DialogueGraphWindow : EditorWindow
    {
        private DialogueGraphView _graphView;
        private string _fileName = "New Narrative";

        private void GenerateToolbar()
        {
            var toolbar = new UnityEditor.UIElements.Toolbar();

            var fileNameTextField = new TextField("File Name:");
            fileNameTextField.SetValueWithoutNotify(_fileName);
            fileNameTextField.MarkDirtyRepaint();
            fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
            toolbar.Add(fileNameTextField);

            toolbar.Add(new Button(() => SaveData()) { text = "Save Data" });
            toolbar.Add(new Button(() => LoadData()) { text = "Load Data" });

            // ボタンが押されたら新しい DialogueNode を生成するよう修正
            var createNodeBtn = new Button(() => { _graphView.CreateNode(new DialogueNode(), Vector2.zero); }) { text = "Add Node" };
            toolbar.Add(createNodeBtn);

            rootVisualElement.Add(toolbar);
        }

        private void SaveData()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                EditorUtility.DisplayDialog("Invalid file name!", "Please enter a valid file name.", "OK");
                return;
            }
            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.SaveGraph(_fileName);
        }

        private void LoadData()
        {
            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.LoadGraph(_fileName);
        }

        [MenuItem("Window/Dialogue Node Editor")]
        public static void OpenDialogueGraphWindow()
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Editor");
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }

        private void ConstructGraphView()
        {
            _graphView = new DialogueGraphView(this)
            {
                name = "Dialogue Graph"
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);

            var searchWindow = ScriptableObject.CreateInstance<DialogueSearchWindow>();
            searchWindow.Init(this, _graphView);
            _graphView.nodeCreationRequest = context => 
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }
    }
}