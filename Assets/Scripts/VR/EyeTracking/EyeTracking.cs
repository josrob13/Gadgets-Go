using UnityEngine;

public class EyeTracking : MonoBehaviour
{
    [Tooltip("Asigna aquí el componente OVREyeGaze que pusiste en el CenterEyeAnchor")]
    public OVREyeGaze eyeGaze;

    private void Update()
    {
        // 1. Verificamos que el componente exista y el tracking esté activo en el hardware
        if (eyeGaze == null || !eyeGaze.EyeTrackingEnabled) return;

        // 2. La confianza (Confidence) va de 0 a 1. Solo procesamos la física si el sensor está seguro.
        if (eyeGaze.Confidence > 0.5f)
        {
            // 3. Creamos un rayo desde la posición del ojo hacia su vector 'forward' (hacia donde mira)
            Ray gazeRay = new Ray(eyeGaze.transform.position, eyeGaze.transform.forward);
            
            // 4. Lanzamos el Raycast. Recuerda añadir un LayerMask en el futuro para mayor optimización.
            if (Physics.Raycast(gazeRay, out RaycastHit hit))
            {
                // Debug.Log($"Estás mirando directamente a: {hit.collider.gameObject.name}");
                
                // Dibuja una línea verde en la vista de escena (Scene view) para facilitar el debugging
                Debug.DrawRay(gazeRay.origin, gazeRay.direction * hit.distance, Color.green);
            }
        }
    }
}
