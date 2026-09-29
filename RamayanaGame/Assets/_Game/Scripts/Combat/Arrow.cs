using UnityEngine;

namespace Ramayana.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Arrow : MonoBehaviour
    {
        [SerializeField] float speed = 20f;
        [SerializeField] float lifetime = 2f;
        [SerializeField] int damage = 10;

        GameObject owner;

        public void Launch(int direction, GameObject shooter)
        {
            owner = shooter;
            var rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(direction * speed, 0f);
            var s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * direction, s.y, s.z);
            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && other.transform.IsChildOf(owner.transform)) return;

            if (other.TryGetComponent<IArrowTarget>(out var target))
            {
                if (target.OnArrowHit(damage)) Destroy(gameObject);
            }
            else if (!other.isTrigger)
            {
                Destroy(gameObject);
            }
        }
    }
}
