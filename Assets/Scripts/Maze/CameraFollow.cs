using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private Transform secondaryTarget;
    [SerializeField, Min(0.1f)] private float orthographicSize = 6f;
    [SerializeField, Min(0f)] private float smoothTime = 0.18f;
    [SerializeField, Min(0f)] private float viewportPadding = 0.75f;

    private Camera sceneCamera;
    private Vector3 smoothVelocity;

    private void Awake()
    {
        sceneCamera = GetComponent<Camera>();
        sceneCamera.orthographic = true;
        sceneCamera.orthographicSize = orthographicSize;
        SetCameraRotation();
    }

    private void LateUpdate()
    {
        ResolveTargets();
        if (followTarget == null)
        {
            return;
        }

        Vector3 desiredPosition = followTarget.position;
        if (secondaryTarget != null)
        {
            Vector2 playerOffset = (Vector2)(secondaryTarget.position - followTarget.position);
            float halfHeight = sceneCamera.orthographicSize;
            float halfWidth = halfHeight * sceneCamera.aspect;
            float availableHalfWidth = Mathf.Max(0f, halfWidth - viewportPadding);
            float availableHalfHeight = Mathf.Max(0f, halfHeight - viewportPadding);

            if (Mathf.Abs(playerOffset.x) > availableHalfWidth
                || Mathf.Abs(playerOffset.y) > availableHalfHeight)
            {
                desiredPosition.x += Mathf.Clamp(playerOffset.x * 0.5f, -availableHalfWidth, availableHalfWidth);
                desiredPosition.y += Mathf.Clamp(playerOffset.y * 0.5f, -availableHalfHeight, availableHalfHeight);
            }
        }

        desiredPosition.z = -10f;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref smoothVelocity,
            smoothTime);
        SetCameraRotation();
    }

    private void ResolveTargets()
    {
        if (followTarget == null)
        {
            GameObject aiObject = GameObject.Find("AICharacter");
            if (aiObject != null)
            {
                followTarget = aiObject.transform;
            }
        }

        if (secondaryTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                secondaryTarget = playerObject.transform;
            }
        }
    }

    private void SetCameraRotation()
    {
        transform.rotation = Quaternion.identity;
    }
}
