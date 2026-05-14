using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueNodeEditor
{
    [Serializable]
    public class DialogueNodeData
    {        
        public string DialogueText;
        public string NodeGUID;
        public string SpeakerName;

        public string Expression; // 追加: 選択された表情名
        public Vector2 Position;
        public List<string> Choices = new List<string>(); // 追加: 選択肢ポート名リスト
        
        public string PortraitNodeGUID;
        public string StillNodeGUID;
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
        public string CharacterName;
        public string NodeGUID;
        public List<ExpressionData> Expressions = new List<ExpressionData>(); // 変更: Spriteも保存する
        public Vector2 Position;
    }

    [Serializable]
    public class PortraitNodeData
    {
        public Sprite PortraitImage;
        public string NodeGUID;
        public Vector2 Position;
    }

    [Serializable]
    public class StillNodeData
    {        
        public Sprite StillImage;
        public string NodeGUID;
        public bool ShouldScrollStill;
        public float ScrollAmount;
        public float ScrollSpeed;
        public Vector2 Position;
    }

    [Serializable]
    public class PanelSizeNodeData
    {   
        public float PanelWidth;
        public string NodeGUID;
        public Vector2 Position;
    }

    [Serializable]
    public class EndNodeData
    {
        public string NodeGUID;
        public Vector2 Position;
    }
}