using UnityEngine;

namespace Ramayana.Data
{
    // One version of a character, e.g. Rama "Young Prince" or Rama "Exile".
    [CreateAssetMenu(menuName = "Ramayana/Character Form", fileName = "NewCharacterForm")]
    public class CharacterForm : ScriptableObject
    {
        public string characterId = "rama";
        public string formName = "Young Prince";
        public Sprite portrait;
        public RuntimeAnimatorController animator;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float jumpForce = 13f;

        [Header("Combat")]
        public bool canShoot = true;
        public int maxHealth = 100;
    }
}
