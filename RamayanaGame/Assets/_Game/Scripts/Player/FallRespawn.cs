using UnityEngine;

namespace Ramayana.Player
{
    // Returns the player to the last safe ground position after falling into a gap.
    [RequireComponent(typeof(Rigidbody2D))]
    public class FallRespawn : MonoBehaviour
    {
        [SerializeField] float killY = -8f;
        [SerializeField] float edgeMargin = 1.2f;

        Rigidbody2D rb;
        Vector2 lastSafe;
        readonly ContactPoint2D[] contacts = new ContactPoint2D[4];

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            lastSafe = rb.position;
        }

        void FixedUpdate()
        {
            int n = rb.GetContacts(contacts);
            for (int i = 0; i < n; i++)
            {
                var col = contacts[i].collider;
                // Only remember spots well inside a platform so we don't respawn on the edge.
                if (contacts[i].normal.y > 0.6f && col != null
                    && rb.position.x > col.bounds.min.x + edgeMargin && rb.position.x < col.bounds.max.x - edgeMargin)
                {
                    lastSafe = rb.position;
                    break;
                }
            }

            if (rb.position.y < killY)
            {
                rb.position = lastSafe + Vector2.up * 0.5f;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}
