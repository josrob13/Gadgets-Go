using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    private Coroutine panCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // Adjusts camera position and rotation
    public void SetTransform(Vector3 pos, Vector3 eulerRot)
    {
        transform.position = pos;
        transform.rotation = Quaternion.Euler(eulerRot);
    }

    // Starts infinite panning
    public void StartPan(float panAngle, float panSpeed)
    {
        // Si ya hay uno activo, lo paramos antes
        if (panCoroutine != null)
            StopCoroutine(panCoroutine);

        panCoroutine = StartCoroutine(PanRoutine(panAngle, panSpeed));
    }

    // Stops panning
    public void StopPan()
    {
        if (panCoroutine != null)
        {
            StopCoroutine(panCoroutine);
            panCoroutine = null;
        }
    }

    private IEnumerator PanRoutine(float angle, float speed)
    {
        // Initial rotation
        Quaternion baseRot = transform.rotation;

        float t = 0f;
        while (true)
        {
            // USES THE SINE FUNCTION TO CREATE A SMOOTH PANNING EFFECT
            float offset = Mathf.Sin(t * speed) * angle;
            transform.rotation = baseRot * Quaternion.Euler(0f, offset, 0f);

            t += Time.deltaTime;
            yield return null;
        }
    }
}
