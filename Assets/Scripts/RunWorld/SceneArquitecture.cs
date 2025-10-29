using System.Collections.Generic;
using UnityEngine;

public class SceneArquitecture
{
   

    [System.Serializable]
    public class SceneData
    {
        public string SceneId;
        public float LevelWidth;
        public List<SceneObject> Objects;
    }
    
    [System.Serializable]
    public class SceneObject
    {
        public string Id;
        public string Name;
        public string Type;
        public List<float> Position;
        public float Rotation;
        public List<float> Scale;
        public string Tag;
        public int Layer;
        public ComponentsData Components;
        public string SpritePath;
        public string Script;
        public Dictionary<string, object> Metadata;
    }
    
    [System.Serializable]
    public class ComponentsData
    {
        public bool rigidbody;
        public bool collider;
        public List<float> colliderSize;
    }
    
}
