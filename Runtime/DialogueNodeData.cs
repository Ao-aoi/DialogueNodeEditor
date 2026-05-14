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
        public Vector2 Position;
    }

    [Serializable]
    public class CharacterNodeData
    {
        public string NodeGUID;
        public string CharacterName;
        public List<string> ExpressionList;
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