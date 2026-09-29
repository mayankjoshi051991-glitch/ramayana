using Ramayana.Combat;
using Ramayana.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ramayana.Player
{
    // Keyboard/gamepad bindings are here; on-screen touch controls drive the virtual gamepad.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] CharacterForm form;
        [SerializeField] Arrow arrowPrefab;
        [SerializeField] Transform bowPoint;
        [SerializeField] float shootCooldown = 0.35f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBufferTime = 0.12f;

        public CharacterForm Form => form;
        public int Facing { get; private set; } = 1;

        Rigidbody2D rb;
        InputAction move, jump, shoot;
        float lastGroundedTime = -1f;
        float lastJumpPressTime = -1f;
        float nextShotTime;
        readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x");

            jump = new InputAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Keyboard>/w");
            jump.AddBinding("<Gamepad>/buttonSouth");

            shoot = new InputAction("Shoot", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/f");
            shoot.AddBinding("<Keyboard>/j");
            shoot.AddBinding("<Gamepad>/buttonWest");
        }

        void OnEnable() { move.Enable(); jump.Enable(); shoot.Enable(); }
        void OnDisable() { move.Disable(); jump.Disable(); shoot.Disable(); }
        void OnDestroy() { move.Dispose(); jump.Dispose(); shoot.Dispose(); }

        public void SetForm(CharacterForm newForm) => form = newForm;

        void Update()
        {
            if (jump.WasPressedThisFrame()) lastJumpPressTime = Time.time;
            if (shoot.WasPressedThisFrame()) TryShoot();

            float x = move.ReadValue<float>();
            if (Mathf.Abs(x) > 0.2f)
            {
                Facing = x > 0 ? 1 : -1;
                var s = transform.localScale;
                transform.localScale = new Vector3(Mathf.Abs(s.x) * Facing, s.y, s.z);
            }
        }

        void FixedUpdate()
        {
            if (IsGrounded()) lastGroundedTime = Time.time;

            Vector2 v = rb.linearVelocity;
            v.x = move.ReadValue<float>() * form.moveSpeed;

            bool jumpBuffered = Time.time - lastJumpPressTime <= jumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            if (jumpBuffered && canJump)
            {
                v.y = form.jumpForce;
                lastJumpPressTime = -1f;
                lastGroundedTime = -1f;
            }
            rb.linearVelocity = v;
        }

        bool IsGrounded()
        {
            int n = rb.GetContacts(contacts);
            for (int i = 0; i < n; i++)
                if (contacts[i].normal.y > 0.6f) return true;
            return false;
        }

        void TryShoot()
        {
            if (!form.canShoot || arrowPrefab == null || Time.time < nextShotTime) return;
            nextShotTime = Time.time + shootCooldown;
            var arrow = Instantiate(arrowPrefab, bowPoint ? bowPoint.position : transform.position, Quaternion.identity);
            arrow.Launch(Facing, gameObject);
        }
    }
}
