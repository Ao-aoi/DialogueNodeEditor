using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueNodeEditor
{
    [Serializable]
    public class DialogueNodeData
    {
        public string NodeGUID;
        public string SpeakerName;
        public string DialogueText;
        public string Expression; // 追加: 選択された表情名
        public Vector2 Position;
        public List<string> Choices = new List<string>(); // 追加: 選択肢ポート名リスト
    }

    [Serializable]
    public class ExpressionData
    {
        public string Name;
        public Sprite Sprite;
    }

    [Serializable]
    public class CharacterNodeData
    {
        public string NodeGUID;
        public string CharacterName;
        public List<ExpressionData> Expressions = new List<ExpressionData>(); // 変更: Spriteも保存する
        public Vector2 Position;
    }

    [Serializable]
    public class PortraitNodeData
    {
        public string NodeGUID;
        public Sprite PortraitImage;
        public Vector2 Position;
    }

    [Serializable]
    public class StillNodeData
    {
        public string NodeGUID;
        public Sprite StillImage;
        public bool ShouldScrollStill;
        public float ScrollAmount;
        public float ScrollSpeed;
        public Vector2 Position;
    }

    [Serializable]
    public class PanelSizeNodeData
    {
        public string NodeGUID;
        public float PanelWidth;
        public Vector2 Position;
    }
}