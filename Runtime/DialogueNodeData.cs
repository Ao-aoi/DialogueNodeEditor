using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueNodeEditor
{
    [Serializable]
    public class ExposedProperty
    {
        public string PropertyName = "NewFloat";
        public float PropertyValue = 0.05f;
    }

    [Serializable]
    public class PropertyNodeData
    {
        public string NodeGUID;
        public string PropertyName;
        public Vector2 Position;
    }

    [Serializable]
    public class DialogueNodeData
    {        
        public string DialogueText;
        public string NodeGUID;
        public string SpeakerName;

        public string Expression;
        public Vector2 Position;
        public List<string> Choices = new List<string>();
        
        public string PortraitNodeGUID;
        public string StillNodeGUID;

        public bool ShowSettings;
        public bool OverrideTypingSpeed;
        public float TypingSpeedValue;
        public string TypingSpeedNodeGUID;
        public bool CanSkipTyping = true;
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
        public List<ExpressionData> Expressions = new List<ExpressionData>();
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

    [Serializable]
    public class StartNodeData
    {
        public string NodeGUID;
        public Vector2 Position;
    }

    // ★追加: コピー＆ペースト用のデータ構造
    [Serializable]
    public class CopyPasteData
    {
        public List<DialogueNodeData> DialogueNodes = new List<DialogueNodeData>();
        public List<CharacterNodeData> CharacterNodes = new List<CharacterNodeData>();
        public List<PortraitNodeData> PortraitNodes = new List<PortraitNodeData>();
        public List<StillNodeData> StillNodes = new List<StillNodeData>();
        public List<PanelSizeNodeData> PanelSizeNodes = new List<PanelSizeNodeData>();
        public List<PropertyNodeData> PropertyNodes = new List<PropertyNodeData>();
    }
}