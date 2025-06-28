using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameOrquestrator : MonoBehaviour
{
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

    // Añade un SpriteRenderer y asigna el sprite
    public SpriteRenderer AddSpriteRenderer(GameObject obj, Sprite sprite)
    {
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        return sr;
    }

    // Ejemplo de método general para añadir cualquier componente con parámetros
    public Component AddComponentByType(GameObject obj, string componentType, object parameters = null)
    {
        switch (componentType)
        {
            case "Rigidbody2D":
                return AddRigidbody2D(obj);
            case "BoxCollider2D":
                return AddBoxCollider2D(obj, Vector2.one);
            // Agrega más tipos según necesidad
            default:
                return obj.AddComponent(System.Type.GetType(componentType));
        }
    }

    // Ejemplo de llamada desde Lua o desde otro sistema
    public void ConfigureFromLua(GameObject obj, Dictionary<string, object> config)
    {
        if (config.ContainsKey("rigidbody") && (bool)config["rigidbody"])
            AddRigidbody2D(obj);

        if (config.ContainsKey("collider") && (bool)config["collider"])
        {
            var sizeList = config["colliderSize"] as List<object>;
            if (sizeList != null && sizeList.Count == 2)
            {
                float x = System.Convert.ToSingle(sizeList[0]);
                float y = System.Convert.ToSingle(sizeList[1]);
                AddBoxCollider2D(obj, new Vector2(x, y));
            }
        }
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
}
