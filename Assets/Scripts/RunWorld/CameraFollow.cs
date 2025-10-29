using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target; // assign player transform or tag lookup
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10f);
    public float minX, maxX; // optional bounds
    public float minY, maxY;
    public bool useBounds = false;

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, 0f) + offset;
        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minX, maxX);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minY, maxY);
        }
        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed);
        transform.position = smoothed;
    }
}
