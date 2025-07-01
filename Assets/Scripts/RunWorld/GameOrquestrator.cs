using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static SceneArquitecture;
using MoonSharp.Interpreter;
using System; // Asegúrate de tener MoonSharp configurado en tu proyecto

public class GameOrquestrator : MonoBehaviour
{
    // Singleton para acceso global
    public static GameOrquestrator Instance { get; private set; }
    public Dictionary<string, GameObject> objectDictionary = new();
    public TMPro.TMP_InputField worldNameInput; 
    public string WorldName = "prueba";
    public GameObject mainMenu;
    public string WorldPath => Path.Combine(Application.persistentDataPath, WorldName);
    public string ScriptsPath => Path.Combine(WorldPath, "scripts");
    public string SpritesPath => Path.Combine(WorldPath, "sprites");
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantiene el orquestador entre escenas
        }
        else
        {
            Destroy(gameObject); // Asegura que solo haya una instancia
        }
    }

    public void InstantiateSceneObjects(string sceneId)
    {
        mainMenu.SetActive(false);
        objectDictionary.Clear(); // Limpiar objetos previos
        var scenePath = Path.Combine(WorldPath, $"{sceneId}");
        if (!Directory.Exists(scenePath))
        {
            Debug.LogError($"Scene folder file not found: {scenePath}");
            return;
        }
        // Cargar los datos de la escena desde el archivo JSON
        SceneData sceneData = LoadSceneData(scenePath, sceneId);
        foreach (var objData in sceneData.Objects)
        {
            // 1. Crear el GameObject
            Vector2 position = new(objData.Position[0], objData.Position[1]);
            GameObject obj = CreateGameObject(objData.Name, position);
            SetParent(obj, gameObject);

            Boolean isKinematic = true && objData.Type != "player";
            // 2. Añadir componentes según el tipo
            if (objData.Components.rigidbody)
                AddRigidbody2D(obj, 1f, isKinematic); 

            if (objData.Components.collider && objData.Components.colliderSize != null && objData.Components.colliderSize.Count == 2)
                AddBoxCollider2D(obj, new Vector2(objData.Components.colliderSize[0], objData.Components.colliderSize[1]), true);

            // 3. Escala y rotación
            if (objData.Scale != null && objData.Scale.Count == 2)
                SetScale(obj, new Vector2(objData.Scale[0], objData.Scale[1]));
            SetRotation(obj, objData.Rotation);

            // 4. Tag y layer
            if (objData.Id == "Player")
            {
                SetTag(obj, "Player");
            }
            else
            {
                SetTag(obj, "Untagged");
            }
            SetLayer(obj, objData.Layer);

            // 5. Sprite
            if (!string.IsNullOrEmpty(objData.SpritePath))
            {
                    
                SetDynamicSprite(obj, Path.Combine(WorldPath, objData.SpritePath + ".png"), 100f);


                if (objData.SpritePath != "null")
                {
                    string spriteFullPath = objData.SpritePath;
                    if (File.Exists(spriteFullPath)) 
                        SetDynamicSprite(obj, spriteFullPath);
                }
            }

            // 6. Asignar script Lua si existe
            if (!string.IsNullOrEmpty(objData.Script) && objData.Script != "null")
            {
                string scriptFile = Path.Combine(ScriptsPath, sceneId, objData.Script + ".lua");
                if (File.Exists(scriptFile))
                {
                    
                    // Asignar el script al GameObject como un componente LuaScript (debes tener un script que maneje esto)
                    LuaBehaviour luaComponent = obj.AddComponent<LuaBehaviour>(); // Asegúrate de tener un script C# que maneje la lógica 
                    luaComponent.scriptPath = scriptFile; // Asigna la ruta del script Lua
                }
                else
                {
                    Debug.LogWarning($"Lua script file not found: {scriptFile}");
                }
            }

            

            // 7. Guardar referencia
            objectDictionary[objData.Id] = obj;

        }
    }


    // Instancia un GameObject y le añade los componentes solicitados
    public GameObject CreateGameObject(string name, Vector2 position)
    {
        GameObject obj = new(name);
        obj.transform.position = position;
        return obj;
    }

    // Añade un Rigidbody2D configurado para top-down (sin gravedad)
    public Rigidbody2D AddRigidbody2D(GameObject obj, float mass = 1f, bool isKinematic = false)
    {
        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Top-down: sin gravedad
        rb.mass = mass;
        rb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        return rb;
    }

    // Añade un BoxCollider2D (puedes añadir otros tipos según lo necesites)
    public BoxCollider2D AddBoxCollider2D(GameObject obj, Vector2 size, bool isTrigger = false)
    {
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = isTrigger;
        return collider;
    }

    public void SetDynamicSprite(GameObject obj, byte[] pngBytes, float pixelsPerUnit = 100f)
    {
        // 1. Crea la textura desde los bytes del PNG
        Texture2D texture = new(2, 2);
        texture.LoadImage(pngBytes);

        // 2. Crea el sprite desde la textura
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );

        // 3. Añade o reutiliza el SpriteRenderer y asigna el sprite
        if (!obj.TryGetComponent<SpriteRenderer>(out var sr))
            sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
    }


    public void SetDynamicSprite(GameObject obj, string filePath, float pixelsPerUnit = 100f)
    {
        byte[] pngBytes = File.ReadAllBytes(filePath);
        SetDynamicSprite(obj, pngBytes, pixelsPerUnit);
    }

    public void SetTag(GameObject obj, string tag)
    {
        obj.tag = tag;
    }

    public void SetLayer(GameObject obj, int layer)
    {
        obj.layer = layer;
    }

    public void SetActive(GameObject obj, bool active)
    {
        obj.SetActive(active);
    }

    public void DestroyObject(GameObject obj)
    {
        GameObject.Destroy(obj);
    }

    public void SetParent(GameObject obj, GameObject parent)
    {
        obj.transform.SetParent(parent.transform);
    }

    public void SetPosition(GameObject obj, Vector2 position)
    {
        obj.transform.position = position;
    }

    public void SetRotation(GameObject obj, float angle)
    {
        obj.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetScale(GameObject obj, Vector2 scale)
    {
        obj.transform.localScale = new Vector3(scale.x, scale.y, 1);
    }
    public float GetAxis(string axisName)
    {
        return Input.GetAxis(axisName);
    }
    public void RegisterTrigger(GameObject origin, GameObject target, string triggerType)
    {
        // Aquí puedes implementar la lógica para manejar los triggers
        // Por ejemplo, podrías enviar un evento a un sistema de eventos o manejarlo directamente
        Debug.Log($"Trigger: {triggerType} - Origin: {origin.name}, Target: {target.name}");
    }
    public SceneData LoadSceneData(string scenePath, string sceneId)
    {
        string jsonPath = Path.Combine(scenePath, $"architecture_{sceneId}.json");
        string json = File.ReadAllText(jsonPath);
        return JsonConvert.DeserializeObject<SceneData>(json);
    }

    public void ExposeFunctionsToLua(Script luaScript)
    {
        // Exponer funciones C# a Lua
        luaScript.Globals["SetPosition"] = (Action<GameObject, Vector2>)SetPosition;
        luaScript.Globals["DestroyObject"] = (Action<GameObject>)DestroyObject;
        luaScript.Globals["SetActive"] = (Action<GameObject, bool>)SetActive;
        luaScript.Globals["GetAxis"] = (Func<string, float>)GetAxis;
        luaScript.Globals["RegisterTrigger"] = (Action<GameObject, GameObject, string>)RegisterTrigger;
        luaScript.Globals["InstantiateSceneObjects"] = (Action<string>)InstantiateSceneObjects;
        luaScript.Globals["SetTag"] = (Action<GameObject, string>)SetTag;
        luaScript.Globals["SetLayer"] = (Action<GameObject, int>)SetLayer;
    }
}
