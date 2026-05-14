using UnityEditor;
using UnityEditor.Callbacks; // 追加：OnOpenAsset用
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace DialogueNodeEditor
{
    public class DialogueGraphWindow : EditorWindow
    {
        private DialogueGraphView _graphView;
        private string _currentAssetPath = ""; // 現在開いているファイルのパス
        private Label _pathLabel;

        [MenuItem("Window/Dialogue Node Editor")]
        public static void OpenDialogueGraphWindow()
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Editor");
        }

        // ① プロジェクトウィンドウでダブルクリックされたときの処理
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var container = EditorUtility.InstanceIDToObject(instanceID) as DialogueContainer;
            if (container != null)
            {
                var window = GetWindow<DialogueGraphWindow>();
                window.titleContent = new GUIContent("Dialogue Editor");
                window.LoadGraphFromAsset(container);
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();

            // ③ Ctrl+S (Macは Cmd+S) で保存するイベントの登録
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Ctrl (または Command) + S を検知
            if (evt.actionKey && evt.keyCode == KeyCode.S)
            {
                SaveData();
                evt.StopPropagation(); // イベントのバブルアップを止める
            }
            else if (evt.keyCode == KeyCode.Space)
            {
                var searchWindow = ScriptableObject.CreateInstance<DialogueSearchWindow>();
                searchWindow.Init(this, _graphView);
                SearchWindow.Open(new SearchWindowContext(evt.originalMousePosition), searchWindow);
                evt.StopPropagation();
            }
        }

        private void GenerateToolbar()
        {
            var toolbar = new UnityEditor.UIElements.Toolbar();

            // 現在のファイルパスを表示するラベル
            _pathLabel = new Label("Unsaved Graph");
            _pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _pathLabel.style.width = 250;
            toolbar.Add(_pathLabel);

            toolbar.Add(new Button(() => SaveData()) { text = "Save" });
            toolbar.Add(new Button(() => SaveDataAs()) { text = "Save As..." });
            toolbar.Add(new Button(() => LoadDataDialog()) { text = "Load" });
            toolbar.Add(new Button(() => { _graphView.CreateNode(new DialogueNode(), Vector2.zero); }) { text = "Add Node" });

            rootVisualElement.Add(toolbar);
        }

        // ④ エクスプローラーを開いて保存
        private void SaveDataAs()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Dialogue Graph", "New Narrative", "asset", "Please enter a file name to save the dialogue graph to.");
            if (string.IsNullOrEmpty(path)) return;

            _currentAssetPath = path;
            UpdatePathLabel();

            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.SaveGraph(_currentAssetPath);
        }

        // 上書き保存（パスがなければSaveAsを実行）
        private void SaveData()
        {
            if (string.IsNullOrEmpty(_currentAssetPath))
            {
                SaveDataAs();
            }
            else
            {
                var saveUtility = GraphSaveUtility.GetInstance(_graphView);
                saveUtility.SaveGraph(_currentAssetPath);
            }
        }

        // ロード画面（ダイアログ）を開く
        private void LoadDataDialog()
        {
            string path = EditorUtility.OpenFilePanel("Load Dialogue Graph", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Application.dataPathを基準にAssetsからの相対パスに変換
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            var container = AssetDatabase.LoadAssetAtPath<DialogueContainer>(path);
            if (container != null)
            {
                LoadGraphFromAsset(container);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Selected file is not a Dialogue Container.", "OK");
            }
        }

        // コンテナからロードする処理
        public void LoadGraphFromAsset(DialogueContainer container)
        {
            if (_graphView == null) return; // ウィンドウが開いていない場合のフェイルセーフ

            _currentAssetPath = AssetDatabase.GetAssetPath(container);
            UpdatePathLabel();

            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.LoadGraph(container);
        }

        private void UpdatePathLabel()
        {
            if (_pathLabel != null)
            {
                _pathLabel.text = string.IsNullOrEmpty(_currentAssetPath) ? "Unsaved Graph" : _currentAssetPath;
            }
        }

        private void ConstructGraphView()
        {
            _graphView = new DialogueGraphView(this) { name = "Dialogue Graph" };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);

            var searchWindow = ScriptableObject.CreateInstance<DialogueSearchWindow>();
            searchWindow.Init(this, _graphView);
            _graphView.nodeCreationRequest = context => 
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        
            _graphView.schedule.Execute(() => {
                if (_graphView.nodes.ToList().Count == 0)
                {
                    _graphView.CreateNode(new EndNode(), new Vector2(600, 200));
                }
            }).StartingIn(50);
        }
    }
}