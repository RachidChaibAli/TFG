using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static SceneArquitecture;
using MoonSharp.Interpreter;
using System; // Aseg�rate de tener MoonSharp configurado en tu proyecto

public class GameOrquestrator : MonoBehaviour
{
    // Singleton para acceso global
    public static GameOrquestrator Instance { get; private set; }
    // Camera follow fields (moved to class scope so Awake/Start/other methods can access)
    private CameraFollow camFollow;
    private string pendingCameraFollowTarget;
    private float? pendingCameraSize;
    public Dictionary<string, GameObject> objectDictionary = new();
    // Level boundary gameObjects created at runtime (start/end walls) so we can remove them when changing scenes
    private List<GameObject> levelBoundaries = new List<GameObject>();
    private int sortingOrderCounter = 0;
    public TMPro.TMP_InputField worldNameInput;
    public string WorldName = "prueba";
    public GameObject mainMenu;
    public string CurrentSceneId { get; private set; }
    public string WorldPath => Path.Combine(Application.persistentDataPath, WorldName);
    public string ScriptsPath => Path.Combine(WorldPath, "scripts");
    public string SpritesPath => Path.Combine(WorldPath, "sprites");
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantiene el orquestador entre escenas
            // Ensure DialogueManager exists at startup to avoid missing-instance issues
            DialogueManager.EnsureInstance();
        }
        else
        {
            Destroy(gameObject); // Asegura que solo haya una instancia
        }
    }

    public void InstantiateSceneObjects(string sceneId)
    {
        mainMenu.SetActive(false);
        // Destroy previously created level boundary walls (if any) before clearing objects
        try
        {
            if (levelBoundaries != null)
            {
                foreach (var lb in levelBoundaries)
                {
                    if (lb != null) Destroy(lb);
                }
                levelBoundaries.Clear();
            }
        }
        catch { }

        objectDictionary.Clear(); // Limpiar objetos previos
        CurrentSceneId = sceneId;
        sortingOrderCounter = 0; // reset per scene
        var scenePath = Path.Combine(WorldPath, $"{sceneId}");
        if (!Directory.Exists(scenePath))
        {
            Debug.LogError($"Scene folder file not found: {scenePath}");
            return;
        }
        // Cargar los datos de la escena desde el archivo JSON
        SceneData sceneData = LoadSceneData(scenePath, sceneId);

        // Extraer LevelWidth desde la arquitectura para uso en runtime (escala de fondos, límites)
        float levelWidth = 0f;
        try
        {
            string archJsonPath = Path.Combine(scenePath, $"architecture_{sceneId}.json");
            if (File.Exists(archJsonPath))
            {
                var archText = File.ReadAllText(archJsonPath);
                var archObj = JObject.Parse(archText);
                if (archObj.TryGetValue("LevelWidth", out var lwToken))
                    levelWidth = lwToken.Value<float>();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to read LevelWidth from architecture JSON: " + ex.Message);
        }
        if (levelWidth <= 0f) levelWidth = 50f; // fallback

        
        try
        {
            CreateLevelBoundaryWalls(levelWidth, sceneId);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Error creating map boundary colliders: " + ex.Message);
        }
        // Prepare camera handling: if the architecture contains a Camera object, configure existing Main Camera
        Camera mainCam = Camera.main;
        // use class-level camFollow/pendingCameraFollowTarget/pendingCameraSize so they remain available after this method
        camFollow = null;
        pendingCameraFollowTarget = null;
        pendingCameraSize = null;
        // Track placed interactable X positions to enforce minimum spacing
        List<float> placedInteractableXs = new List<float>();
        float minSpacing = 20f; // hard minimum spacing in world units

        foreach (var objData in sceneData.Objects)
        {
            // Special handling for Camera objects: configure existing Main Camera instead of creating a new one
            if (!string.IsNullOrEmpty(objData.Type) && objData.Type.Equals("Camera", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (objData.Metadata != null)
                    {
                        if (objData.Metadata.ContainsKey("followTarget"))
                        {
                            pendingCameraFollowTarget = objData.Metadata["followTarget"]?.ToString();
                        }
                        if (objData.Metadata.ContainsKey("size"))
                        {
                            try { pendingCameraSize = Convert.ToSingle(objData.Metadata["size"]); } catch { }
                        }
                    }

                    // If there is a main camera in the scene, configure it
                    if (mainCam != null)
                    {
                        // set camera position if provided
                        if (objData.Position != null && objData.Position.Count >= 2)
                        {
                            mainCam.transform.position = new Vector3(objData.Position[0], objData.Position[1], mainCam.transform.position.z);
                        }
                        if (pendingCameraSize.HasValue)
                        {
                            // Force a working camera size for playtesting
                            mainCam.orthographic = true;
                            mainCam.orthographicSize = 10f; // tamaño solicitado por el usuario (ajustado a 10 para que no se salga el fondo)
                            Debug.Log("Main camera size set to 10 (forced)");
                        }
                        // ensure CameraFollow component exists
                        camFollow = mainCam.GetComponent<CameraFollow>();
                        camFollow ??= mainCam.gameObject.AddComponent<CameraFollow>();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Error configuring Main Camera from architecture: " + ex.Message);
                }

                // do not instantiate a separate GameObject for the camera
                continue;
            }

            // 1. Crear el GameObject
            Vector2 position = new(objData.Position[0], objData.Position[1]);

            // Enforce player start X range
            if (!string.IsNullOrEmpty(objData.Type) && objData.Type.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
            {
                if (position.x < 1.0f || position.x > 3.0f)
                {
                    Debug.LogWarning($"Player start X out of recommended range ({position.x}), snapping to 2.0");
                    position.x = 2.0f;
                }
            }

            // For major interactables, enforce minimum horizontal spacing of minSpacing
            bool isMajor = false;
            try { if (!string.IsNullOrEmpty(objData.Type) && (objData.Type.Equals("NPC", System.StringComparison.OrdinalIgnoreCase) || objData.Type.Equals("Item", System.StringComparison.OrdinalIgnoreCase) || objData.Type.Equals("Door", System.StringComparison.OrdinalIgnoreCase) || objData.Type.Equals("Player", System.StringComparison.OrdinalIgnoreCase))) isMajor = true; } catch { }
            if (isMajor)
            {
                float desiredX = position.x;
                // ensure spacing by shifting right until free
                placedInteractableXs.Sort();
                foreach (var existingX in placedInteractableXs)
                {
                    if (Mathf.Abs(desiredX - existingX) < minSpacing)
                    {
                        desiredX = existingX + minSpacing;
                    }
                }
                // clamp within bounds
                if (desiredX > levelWidth - 1.0f)
                {
                    Debug.LogWarning($"Adjusted X {desiredX} exceeded level bounds; clamping to {levelWidth - 1.0f}");
                    desiredX = levelWidth - 1.0f;
                }
                position.x = desiredX;
            }

            GameObject obj = CreateGameObject(objData.Name, position);
            SetParent(obj, gameObject);

            Boolean isKinematic = true && objData.Type != "player";
            // 2. A�adir componentes seg�n el tipo
            if (objData.Components.rigidbody)
                AddRigidbody2D(obj, 1f, isKinematic);

            if (objData.Components.collider && objData.Components.colliderSize != null && objData.Components.colliderSize.Count == 2)
            {
                // Determine whether collider should be a trigger. Default: true for interactive triggers, but player must never be a trigger.
                bool colliderIsTrigger = true;
                try
                {
                    // If metadata contains an explicit isTrigger flag, respect it
                    if (objData.Metadata != null && objData.Metadata.ContainsKey("isTrigger"))
                    {
                        colliderIsTrigger = Convert.ToBoolean(objData.Metadata["isTrigger"]);
                    }
                }
                catch { }

                // Ensure player colliders are not triggers
                if (!string.IsNullOrEmpty(objData.Type) && objData.Type.Equals("Player", StringComparison.OrdinalIgnoreCase))
                    colliderIsTrigger = false;
                if (objData.Id != null && objData.Id.Equals("player", StringComparison.OrdinalIgnoreCase))
                    colliderIsTrigger = false;

                AddBoxCollider2D(obj, new Vector2(objData.Components.colliderSize[0], objData.Components.colliderSize[1]), colliderIsTrigger);
            }

            // 3. Escala y rotaci�n
            if (objData.Scale != null && objData.Scale.Count == 2)
                SetScale(obj, new Vector2(objData.Scale[0], objData.Scale[1]));
            SetRotation(obj, objData.Rotation);

            // 4. Tag y layer
            // Determinar etiqueta del objeto: prioridad a objData.Tag si existe, si no usar Id comparado sin distinci�n de may�sc/min
            if (!string.IsNullOrEmpty(objData.Tag))
            {
                SetTag(obj, objData.Tag);
            }
            else if (objData.Id != null && objData.Id.Equals("player", System.StringComparison.OrdinalIgnoreCase))
            {
                SetTag(obj, "Player");
                obj.GetComponent<BoxCollider2D>().isTrigger = false;
            }
            else
            {
                SetTag(obj, "Untagged");

            }
            SetLayer(obj, objData.Layer);

            // 5. Sprite
            if (!string.IsNullOrEmpty(objData.SpritePath))
            {
                // determine background vs standard for depth ordering
                bool isBackground = false;
                try
                {
                    if (!string.IsNullOrEmpty(objData.Type) && objData.Type.ToLower().Contains("background")) isBackground = true;
                    if (!isBackground && !string.IsNullOrEmpty(objData.SpritePath) && objData.SpritePath.ToLower().Contains("background")) isBackground = true;
                    if (!isBackground && objData.Layer < 0) isBackground = true;
                }
                catch { }

                string spriteFile = Path.Combine(SpritesPath, sceneId, "png", objData.SpritePath);
                if (File.Exists(spriteFile))
                {
                    SetDynamicSprite(obj, spriteFile);
                }
                else
                {
                    // try absolute path
                    if (File.Exists(objData.SpritePath))
                        SetDynamicSprite(obj, objData.SpritePath);
                    else
                        Debug.LogWarning($"Sprite file not found for object {objData.Id}: {spriteFile}");
                }

                // After sprite assigned, set sorting order and z-depth
                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (isBackground)
                    {
                        sr.sortingOrder = -100;
                        // Ensure background covers the full level width by scaling or centering
                        try
                        {
                            float spriteWorldWidth = sr.bounds.size.x;
                            if (spriteWorldWidth > 0f && levelWidth > 0f)
                            {
                                // Desired width should extend 10 units on both sides by default (paddingUnits = 10)
                                float paddingUnits = 10f;
                                float desiredWidth = levelWidth + (paddingUnits * 2f);
                                // If metadata specifies paddingUnits or paddingPercent, prefer that
                                try
                                {
                                    if (objData.Metadata != null)
                                    {
                                        if (objData.Metadata.ContainsKey("paddingUnits"))
                                        {
                                            float pu = Convert.ToSingle(objData.Metadata["paddingUnits"]);
                                            if (pu >= 0f) { paddingUnits = pu; desiredWidth = levelWidth + (paddingUnits * 2f); }
                                        }
                                        else if (objData.Metadata.ContainsKey("paddingPercent"))
                                        {
                                            float pp = Convert.ToSingle(objData.Metadata["paddingPercent"]);
                                            if (pp > 0f) { desiredWidth = levelWidth * (1f + pp / 100f); }
                                        }
                                    }
                                }
                                catch { }

                                float scaleFactor = desiredWidth / spriteWorldWidth;
                                // apply uniform horizontal scaling while preserving Y scale
                                obj.transform.localScale = new Vector3(obj.transform.localScale.x * scaleFactor, obj.transform.localScale.y * scaleFactor, 1f);
                            }
                            // center background on level center after scaling
                            obj.transform.position = new Vector3(levelWidth / 2.0f, obj.transform.position.y, 10f);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning("Failed to auto-scale background: " + ex.Message);
                            obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, 10f);
                        }
                    }
                    else
                    {
                        sortingOrderCounter++;
                        sr.sortingOrder = sortingOrderCounter;
                        obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, 0f);
                    }
                }
            }

            // 6. Asignar script Lua si existe
            if (!string.IsNullOrEmpty(objData.Script) && objData.Script != "null")
            {
                // Assign script path from the scripts folder per scene. For player or other objects, use the same convention.
                string scriptFile = Path.Combine(ScriptsPath, sceneId, objData.Script);
                if (File.Exists(scriptFile))
                {

                    // Asignar el script al GameObject como un componente LuaScript (debes tener un script que maneje esto)
                    LuaBehaviour luaComponent = obj.AddComponent<LuaBehaviour>(); // Aseg�rate de tener un script C# que maneje la l�gica 
                    luaComponent.scriptPath = scriptFile; // Asigna la ruta del script Lua
                }
                else
                {
                    Debug.LogWarning($"Lua script file not found: {scriptFile}");
                }
            }



            // 7. Guardar referencia
            objectDictionary[objData.Id] = obj;
            // Track major interactable X positions for spacing enforcement
            try
            {
                if (!string.IsNullOrEmpty(objData.Type) && (objData.Type.Equals("NPC", System.StringComparison.OrdinalIgnoreCase) || objData.Type.Equals("Item", System.StringComparison.OrdinalIgnoreCase) || objData.Type.Equals("Door", System.StringComparison.OrdinalIgnoreCase) || objData.Type.Equals("Player", System.StringComparison.OrdinalIgnoreCase)))
                {
                    placedInteractableXs.Add(obj.transform.position.x);
                }
            }
            catch { }

        }

        // After all objects are instantiated, if we had a camera follow target name, resolve it to an object
        try
        {
            if (!string.IsNullOrEmpty(pendingCameraFollowTarget) && camFollow != null)
            {
                // prefer explicit id in objectDictionary
                if (objectDictionary.ContainsKey(pendingCameraFollowTarget))
                {
                    camFollow.target = objectDictionary[pendingCameraFollowTarget].transform;
                }
                else
                {
                    // fallback: try to find object tagged Player
                    foreach (var kv in objectDictionary)
                    {
                        if (kv.Value != null && kv.Value.CompareTag("Player"))
                        {
                            camFollow.target = kv.Value.transform;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error assigning camera follow target: " + ex.Message);
        }
    }


    // Instancia un GameObject y le a�ade los componentes solicitados
    public GameObject CreateGameObject(string name, Vector2 position)
    {
        GameObject obj = new(name);
        obj.transform.position = position;
        return obj;
    }

    public void RegisterTrigger(GameObject origin, GameObject target, string triggerType)
    {
        Debug.Log($"Trigger: {triggerType} - Origin: {origin.name}, Target: {target.name}");
    }

    // A�ade un Rigidbody2D configurado para top-down (sin gravedad)
    public Rigidbody2D AddRigidbody2D(GameObject obj, float mass = 1f, bool isKinematic = false)
    {
        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Top-down: sin gravedad
        rb.mass = mass;
        rb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        return rb;
    }

    // A�ade un BoxCollider2D (puedes a�adir otros tipos seg�n lo necesites)
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

        // 3. A�ade o reutiliza el SpriteRenderer y asigna el sprite
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
        // Apply position but clamp player to ground (y = 0) to avoid infinite falling approach due to numeric integration
        Vector3 pos = obj.transform.position;
        float newX = position.x;
        float newY = position.y;
        pos.x = newX;
        pos.y = newY;
        try
        {
            if (obj != null && obj.CompareTag("Player"))
            {
                // If player is very close to ground, snap to exact ground value to avoid never reaching 0
                if (pos.y <= 0.05f)
                {
                    pos.y = 0f;
                }
                // Prevent player sinking below ground
                if (pos.y < 0f) pos.y = 0f;

            }
        }
        catch { }

        obj.transform.position = pos;
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


    // Handle messages generated by Lua scripts (via MessageContainer -> Message)
    public void HandleMessage(Message msg)
    {
        if (msg == null)
        {
            Debug.LogWarning("HandleMessage called with null msg");
            return;
        }

        Debug.Log($"HandleMessage: origin={msg.origin}, target={msg.target}, method={msg.method}");

        try
        {
            try
            {
                string full = JsonConvert.SerializeObject(msg);
                Debug.Log($"HandleMessage full JSON: {full}");
            }
            catch (Exception serEx)
            {
                Debug.LogWarning($"HandleMessage: failed to serialize message for logging: {serEx.Message}");
            }

            // Dialogue start
            // Pickup handling: accept several method names that scripts may use
            if (!string.IsNullOrEmpty(msg.method) && (msg.method.Equals("Pickup", StringComparison.OrdinalIgnoreCase) || msg.method.Equals("PickUp", StringComparison.OrdinalIgnoreCase) || msg.method.Equals("PickItem", StringComparison.OrdinalIgnoreCase) || msg.method.Equals("PickUpItem", StringComparison.OrdinalIgnoreCase) || msg.method.Equals("PickupItem", StringComparison.OrdinalIgnoreCase)))
            {
                // origin is expected to be the item id
                string originId = msg.origin;
                if (string.IsNullOrEmpty(originId))
                {
                    Debug.LogWarning("HandleMessage: Pickup message missing origin id");
                }
                else
                {
                    try
                    {
                        // Ensure DialogueManager exists for notifications
                        DialogueManager.EnsureInstance();
                        string displayName = originId;
                        if (objectDictionary.ContainsKey(originId) && objectDictionary[originId] != null)
                        {
                            var go = objectDictionary[originId];
                            if (!string.IsNullOrEmpty(go.name)) displayName = go.name;
                            // Destroy the gameobject to simulate pickup
                            DestroyObject(go);
                        }
                        // Remove from dictionary
                        if (objectDictionary.ContainsKey(originId)) objectDictionary.Remove(originId);

                        // Show transient pickup notification
                        if (DialogueManager.Instance != null)
                        {
                            DialogueManager.Instance.ShowNotification($"{displayName} recogido!", 2.0f);
                        }
                        else
                        {
                            Debug.Log($"Pickup: {displayName} recogido!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("HandleMessage Pickup failed: " + ex.Message);
                    }
                }
                // handled pickup, return early
                return;
            }
            if (!string.IsNullOrEmpty(msg.method) && (msg.method == "StartDialogue" || msg.method == "StartConversation"))
            {
                // Validate CurrentSceneId
                if (string.IsNullOrEmpty(CurrentSceneId))
                {
                    Debug.LogWarning($"HandleMessage: CurrentSceneId is null or empty while handling dialogue request from {msg.origin}");
                    return;
                }

                // Load dialogue file for the origin NPC: {origin}_dialog.json in the current scene folder
                string scenePath = Path.Combine(WorldPath, CurrentSceneId);
                string dialogPath = Path.Combine(scenePath, $"{msg.origin}_dialog.json");
                // Ensure DialogueManager exists (user requested robust creation)
                DialogueManager.EnsureInstance();
                Debug.Log($"Looking for dialogue file at: {dialogPath} (CurrentSceneId={CurrentSceneId})");
                if (File.Exists(dialogPath))
                {
                    string json = File.ReadAllText(dialogPath);
                    // Expecting a JSON array of lines [{"id":"...","text":"...","type":"npc"}, ...]
                    DialogueLine[] lines = null;
                    try
                    {
                        lines = JsonConvert.DeserializeObject<DialogueLine[]>(json);
                    }
                    catch (Exception deserEx)
                    {
                        Debug.LogError($"HandleMessage: Failed to deserialize dialogue file {dialogPath}: {deserEx.Message}");
                    }

                    if (lines != null && lines.Length > 0)
                    {
                        if (DialogueManager.Instance != null)
                        {
                            try
                            {
                                DialogueManager.Instance.ShowDialogue(lines);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"HandleMessage: DialogueManager.ShowDialogue threw: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("HandleMessage: DialogueManager.Instance is null — cannot show dialogue.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Dialogue file empty or invalid: {dialogPath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Dialogue file not found: {dialogPath}");
                    // Ensure DialogueManager exists so subsequent calls don't NRE
                    DialogueManager.EnsureInstance();
                }
            }
            else if (!string.IsNullOrEmpty(msg.method) && msg.method == "nextScene")
            {
                // Request to load next scene: try to advance to the next numbered scene (scene_1 -> scene_2)
                Debug.Log("Received nextScene request from: " + msg.origin);

                // First notify DialogueManager if present (keeps existing hook)
                if (DialogueManager.Instance != null)
                {
                    try
                    {
                        DialogueManager.Instance.OnNextSceneRequested();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"HandleMessage: DialogueManager.OnNextSceneRequested threw: {ex.Message}");
                    }
                }

                // Attempt to determine the next scene id numerically based on CurrentSceneId
                try
                {
                    if (string.IsNullOrEmpty(CurrentSceneId))
                    {
                        Debug.LogWarning("HandleMessage.nextScene: CurrentSceneId is empty — cannot determine next scene.");
                    }
                    else
                    {
                        // Expecting format like "scene_1"
                        var parts = CurrentSceneId.Split('_');
                        int currentNum = -1;
                        if (parts.Length >= 2 && int.TryParse(parts[1], out currentNum))
                        {
                            int nextNum = currentNum + 1;
                            string nextId = $"scene_{nextNum}";
                            string nextPath = Path.Combine(WorldPath, nextId);
                            if (Directory.Exists(nextPath))
                            {
                                Debug.Log($"HandleMessage.nextScene: Loading next scene {nextId}");
                                InstantiateSceneObjects(nextId);
                            }
                            else
                            {
                                Debug.LogWarning($"HandleMessage.nextScene: Next scene folder not found: {nextPath} — no more scenes to load.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"HandleMessage.nextScene: Could not parse numeric suffix from CurrentSceneId='{CurrentSceneId}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"HandleMessage.nextScene: unexpected error: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"HandleMessage: Unhandled method '{msg.method}' from origin '{msg.origin}'");
            }
        }
        catch (Exception exAll)
        {
            Debug.LogError($"HandleMessage: Unexpected exception: {exAll}\nMessage: {JsonConvert.SerializeObject(msg)}");
        }
    }

    // Simple DTO for dialogue lines (matches generateDialogPrompt)
    public class DialogueLine
    {
        public string id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
    }
    public SceneData LoadSceneData(string scenePath, string sceneId)
    {
        string jsonPath = Path.Combine(scenePath, $"architecture_{sceneId}.json");
        string json = File.ReadAllText(jsonPath);
        return JsonConvert.DeserializeObject<SceneData>(json);
    }

    // Create solid vertical boundary walls at the start (x=0) and end (x=levelWidth) of the level.
    // Walls are non-trigger BoxCollider2D objects that block movement. They are stored in `levelBoundaries`
    // so they can be destroyed when changing scenes.
    public void CreateLevelBoundaryWalls(float levelWidth, string sceneId)
    {
        try
        {
            // Left wall (start) - make it very tall so it covers far above and below the viewport
            var left = CreateGameObject($"{sceneId}_wall_start", new Vector2(0f, 0f));
            // Use an extremely tall collider to ensure coverage above/below (adjust if needed)
            AddBoxCollider2D(left, new Vector2(0.5f, 200f), false);
            // Add a static Rigidbody2D so physics interactions are handled consistently and objects don't pass through
            try
            {
                var lrb = left.AddComponent<Rigidbody2D>();
                lrb.bodyType = RigidbodyType2D.Static;
            }
            catch { }
            left.tag = "LevelBoundary";
            SetParent(left, gameObject);
            levelBoundaries.Add(left);
            objectDictionary[$"{sceneId}_wall_start"] = left;

            // Right wall (end)
            var right = CreateGameObject($"{sceneId}_wall_end", new Vector2(levelWidth, 0f));
            AddBoxCollider2D(right, new Vector2(0.5f, 200f), false);
            try
            {
                var rrb = right.AddComponent<Rigidbody2D>();
                rrb.bodyType = RigidbodyType2D.Static;
            }
            catch { }
            right.tag = "LevelBoundary";
            SetParent(right, gameObject);
            levelBoundaries.Add(right);
            objectDictionary[$"{sceneId}_wall_end"] = right;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CreateLevelBoundaryWalls failed: " + ex.Message);
        }
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
