using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    private Transform cameraTransform;
    private Coroutine _activeShake;

    private void Awake()
    {
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    public void DoShake(float duration, float magnitude)
    {
        if (cameraTransform == null) return;

        if (_activeShake != null)
            StopCoroutine(_activeShake);
        _activeShake = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cameraTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPos;
        _activeShake = null;
    }
}
