using MoonSharp.Interpreter;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

public class LuaBehaviour : MonoBehaviour
{
    public string scriptPath; // Ruta absoluta al archivo Lua (.lua)
    private Script luaScript;
    private DynValue luaOnUpdate;
    private DynValue luaOnStart;
    private DynValue luaOnTriggerEnter2D;
    private readonly string tagName="Player"; 
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
        if (other.CompareTag(tagName))
        {
            if (luaOnTriggerEnter2D != null && luaOnTriggerEnter2D.Type == DataType.Function)
            {
                // Puedes pasar el nombre, tag o referencia del objeto colisionado según tu integración
                DynValue result = luaScript.Call(luaOnTriggerEnter2D, other.gameObject.name);
                // Procesar el string JSON devuelto
                if (result.Type == DataType.String)
                {
                    string json = result.String;
                    if (!string.IsNullOrWhiteSpace(json) && json != "{}")
                    {
                        // Define una clase para los mensajes
                        var messageContainer = JsonConvert.DeserializeObject<MessageContainer>(json);
                        if (messageContainer != null && messageContainer.messages != null)
                        {
                            foreach (var msg in messageContainer.messages)
                            {
                                Debug.Log($"Mensaje recibido: origin={msg.origin}, target={msg.target}, method={msg.method}");
                            }
                        }
                    }
                }
            }
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
        Vector2 position = new(x, y);
        gameObject.transform.position = position;
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
        return DynValue.NewTable(new Table(luaScript)
        {
            [1] = DynValue.NewNumber(pos.x),
            [2] = DynValue.NewNumber(pos.y)
        });
    }

    public bool IsJumpPressed()
    {
        return Input.GetKey(KeyCode.Space);
    }
}
