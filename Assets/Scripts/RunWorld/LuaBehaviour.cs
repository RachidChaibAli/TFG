using MoonSharp.Interpreter;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;

public class LuaBehaviour : MonoBehaviour
{
    public string scriptPath; // Ruta absoluta al archivo Lua (.lua)
    private Script luaScript;
    private DynValue luaOnUpdate;
    private DynValue luaOnStart;
    private DynValue luaOnTriggerEnter2D;
    private readonly string tagName = "Player";
    void Start()
    {
        if (!string.IsNullOrEmpty(scriptPath) && File.Exists(scriptPath))
        {
            luaScript = new Script();


            // Cargar y ejecutar el script
            string code = File.ReadAllText(scriptPath);
            luaScript.DoString(code);

            // Referencias a funciones Lua
            luaOnStart = luaScript.Globals.Get("onStart");
            luaOnUpdate = luaScript.Globals.Get("onUpdate");
            luaOnTriggerEnter2D = luaScript.Globals.Get("OnTriggerEnter2D");

            // Debug: reportar estado del script cargado
            bool hasOnStart = luaOnStart != null && luaOnStart.Type == DataType.Function;
            bool hasOnUpdate = luaOnUpdate != null && luaOnUpdate.Type == DataType.Function;
            bool hasOnTrigger = luaOnTriggerEnter2D != null && luaOnTriggerEnter2D.Type == DataType.Function;
            Debug.Log($"LuaBehaviour Start: scriptPath={scriptPath} | onStart={hasOnStart} | onUpdate={hasOnUpdate} | OnTriggerEnter2D={hasOnTrigger}");
            if (!hasOnTrigger)
                Debug.LogWarning($"LuaBehaviour: script {scriptPath} does not define OnTriggerEnter2D(other) or it's not a function.");



            // Exponer funciones de LuaBehaviour a Lua

            luaScript.Globals["print"] = (Action<string>)print;
            luaScript.Globals["SetPosition"] = (Action<float, float>)SetPosition;
            luaScript.Globals["SetRotation"] = (Action<float>)SetRotation;
            luaScript.Globals["SetScale"] = (Action<Vector2>)SetScale;
            luaScript.Globals["SetTag"] = (Action<string>)SetTag;
            luaScript.Globals["SetLayer"] = (Action<int>)SetLayer;
            luaScript.Globals["SetActive"] = (Action<bool>)SetActive;
            luaScript.Globals["DestroyObject"] = (Action)DestroyObject;
            luaScript.Globals["SetParent"] = (Action<GameObject>)SetParent;
            luaScript.Globals["GetAxis"] = (Func<string, float>)GetAxis;
            luaScript.Globals["GetPosition"] = (Func<DynValue>)GetPosition;
            luaScript.Globals["IsJumpPressed"] = (Func<bool>)IsJumpPressed;
            luaScript.Globals["SetInverted"] = (Action<bool>)SetInverted;

            // Llamar a onStart si existe
            if (luaOnStart.Type == DataType.Function)
                luaScript.Call(luaOnStart);
        }
        else
        {
            Debug.LogWarning($"Lua script not found at {scriptPath}");
        }
    }

    void Update()
    {
        if (luaOnUpdate != null && luaOnUpdate.Type == DataType.Function)
        {
            luaScript.Call(luaOnUpdate, (float)Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar si el objeto colisionado tiene el tag especificado
        if (!other.CompareTag(tagName))
        {
            Debug.Log($"LuaBehaviour.OnTriggerEnter2D: Ignored collision on '{gameObject.name}' with '{other.gameObject.name}' because tag '{other.gameObject.tag}' != expected '{tagName}'");
            return;
        }

        Debug.Log($"LuaBehaviour.OnTriggerEnter2D: '{gameObject.name}' collided with '{other.gameObject.name}' (tag={other.gameObject.tag})");

        if (luaOnTriggerEnter2D == null || luaOnTriggerEnter2D.Type != DataType.Function)
        {
            Debug.LogWarning($"LuaBehaviour.OnTriggerEnter2D: No Lua handler for OnTriggerEnter2D on '{gameObject.name}' (scriptPath={scriptPath})");
            return;
        }

        try
        {
            // Pass a normalized identifier to Lua: if the collider has tag Player, send the fixed id "player"
            string luaOther = other.CompareTag("Player") ? "player" : other.gameObject.name;
            Debug.Log($"LuaBehaviour.OnTriggerEnter2D: calling Lua OnTriggerEnter2D with param='{luaOther}' (original name='{other.gameObject.name}')");
            DynValue result = luaScript.Call(luaOnTriggerEnter2D, luaOther);
            Debug.Log($"LuaBehaviour.OnTriggerEnter2D: Lua returned type={result.Type} for object '{gameObject.name}'");

            if (result.Type == DataType.String)
            {
                string json = result.String;
                Debug.Log($"LuaBehaviour.OnTriggerEnter2D: Lua returned JSON: {json}");
                if (!string.IsNullOrWhiteSpace(json) && json != "{}")
                {
                    try
                    {
                        // Parse JSON manually to avoid deserialization issues and be robust to missing fields
                        var root = JObject.Parse(json);
                        var msgs = root["messages"] as JArray;
                        if (msgs != null)
                        {
                            foreach (var tk in msgs)
                            {
                                if (tk == null)
                                {
                                    Debug.LogError($"LuaBehaviour.OnTriggerEnter2D: encountered null token in messages array from script {scriptPath}");
                                    continue;
                                }

                                if (!(tk is JObject jobj))
                                {
                                    Debug.LogError($"LuaBehaviour.OnTriggerEnter2D: message token is not an object: {tk}");
                                    continue;
                                }

                                try
                                {
                                    string origin = jobj.Value<string>("origin");
                                    string target = jobj.Value<string>("target");
                                    string method = jobj.Value<string>("method");

                                    JToken contentToken = jobj["content"];
                                    object contentObj = null;
                                    if (contentToken != null && contentToken.Type != JTokenType.Null)
                                    {
                                        try
                                        {
                                            contentObj = contentToken.ToObject<object>();
                                        }
                                        catch (Exception ctEx)
                                        {
                                            Debug.LogWarning($"LuaBehaviour.OnTriggerEnter2D: failed to convert content token to object: {ctEx.Message} | token={contentToken}");
                                            contentObj = null;
                                        }
                                    }

                                    var msg = new Message
                                    {
                                        origin = origin ?? string.Empty,
                                        target = target ?? string.Empty,
                                        method = method ?? string.Empty,
                                        content = contentObj
                                    };

                                    Debug.Log($"LuaBehaviour: prepared message origin='{msg.origin}' target='{msg.target}' method='{msg.method}'");
                                    if (GameOrquestrator.Instance != null)
                                    {
                                        try
                                        {
                                            try
                                            {
                                                Debug.Log($"LuaBehaviour -> Routing message to Orchestrator: {JsonConvert.SerializeObject(msg)}");
                                            }
                                            catch (Exception serEx)
                                            {
                                                Debug.LogWarning($"LuaBehaviour: failed to serialize message for log: {serEx}");
                                            }

                                            // Call HandleMessage and capture any exception it throws
                                            try
                                            {
                                                GameOrquestrator.Instance.HandleMessage(msg);
                                            }
                                            catch (Exception handleEx)
                                            {
                                                Debug.LogError($"LuaBehaviour: GameOrquestrator.HandleMessage threw exception: {handleEx}\nMessage: {JsonConvert.SerializeObject(msg)}");
                                            }
                                        }
                                        catch (Exception exOuter)
                                        {
                                            Debug.LogError($"LuaBehaviour: unexpected error while routing message: {exOuter}");
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log($"Mensaje recibido (no routing): origin={msg.origin}, target={msg.target}, method={msg.method}");
                                    }
                                }
                                catch (Exception innerEx)
                                {
                                    Debug.LogError($"LuaBehaviour.OnTriggerEnter2D: Error processing individual message token: {innerEx.Message} | token={tk}");
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"LuaBehaviour.OnTriggerEnter2D: 'messages' array missing or empty in JSON returned by script {scriptPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"LuaBehaviour.OnTriggerEnter2D: JSON parse error for script {scriptPath}: {ex.Message}\nJSON: {json}");
                    }
                }
                else
                {
                    Debug.Log($"LuaBehaviour.OnTriggerEnter2D: Lua returned empty JSON object '{{}}' for '{gameObject.name}'");
                }
            }
            else
            {
                Debug.LogWarning($"LuaBehaviour.OnTriggerEnter2D: Expected string (JSON) from Lua handler but got {result.Type} for '{gameObject.name}'");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LuaBehaviour.OnTriggerEnter2D: Exception invoking Lua OnTriggerEnter2D for '{gameObject.name}': {ex.Message}");
        }
        return;
    }

    public void print(string mensage)
    {
        Debug.Log(mensage);
    }

    public void SetTag(string tag)
    {
        gameObject.tag = tag;
    }

    public void SetLayer(int layer)
    {
        gameObject.layer = layer;
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }

    public void SetParent(GameObject parent)
    {
        gameObject.transform.SetParent(parent.transform);
    }

    public void SetPosition(float x, float y)
    {
        // Simple flip logic: compare previous X with incoming X and set sprite flip accordingly
        float previousX = gameObject.transform.position.x;
        if (Math.Abs(x - previousX) > 0.01f)
        {
            bool shouldInvert = x < previousX; // true -> facing left
            try
            {
                var sr = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    sr.flipX = shouldInvert;
            }
            catch { }

        }


        Vector2 position = new(x, y);
        gameObject.transform.position = position;
    }

    // Exposed to Lua as SetInverted
    public void SetInverted(bool inverted)
    {
        try
        {
            var sr = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.flipX = inverted;
        }
        catch { }
    }

    public void SetRotation(float angle)
    {
        gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetScale(Vector2 scale)
    {
        gameObject.transform.localScale = new Vector3(scale.x, scale.y, 1);
    }

    public float GetAxis(string axisName)
    {
        return Input.GetAxis(axisName);
    }

    public DynValue GetPosition()
    {
        Vector3 pos = gameObject.transform.position;
        // Devuelve una tabla Lua con los valores X e Y
        var table = new Table(luaScript);
        // Proveer tanto índices numéricos como claves nombradas (x,y) para compatibilidad
        table[1] = DynValue.NewNumber(pos.x);
        table[2] = DynValue.NewNumber(pos.y);
        table.Set("x", DynValue.NewNumber(pos.x));
        table.Set("y", DynValue.NewNumber(pos.y));
        return DynValue.NewTable(table);
    }

    public bool IsJumpPressed()
    {
        return Input.GetKey(KeyCode.Space);
    }
}
