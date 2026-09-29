using Ramayana.Player;
using UnityEngine;

namespace Ramayana.CameraSystem
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] PlayerController2D target;
        [SerializeField] Vector2 offset = new(0f, 1.5f);
        [SerializeField] float lookAhead = 2.5f;
        [SerializeField] float smoothTime = 0.2f;
        [SerializeField] float minY = 0f;

        Vector3 velocity;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 p = target.transform.position;
            var goal = new Vector3(p.x + offset.x + lookAhead * target.Facing, Mathf.Max(minY, p.y + offset.y), transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
