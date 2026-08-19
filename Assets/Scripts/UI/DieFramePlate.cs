using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Keeps a flat plate sitting under a die.
    ///
    /// It tracks the die's position but deliberately not its rotation — the die tumbles when it
    /// rolls, and a frame that tumbled with it would read as part of the die rather than as the
    /// surface it lands on. Each die gets its own plate instead of the pair sharing one tray.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class DieFramePlate : MonoBehaviour
    {
        public Transform die;

        [Tooltip("Plate size relative to the die's own scale.")]
        public float scaleMultiplier = 1.9f;

        [Tooltip("How far behind the die the plate sits, in world units.")]
        public float depthOffset = 0.35f;

        private void LateUpdate() { Apply(); }

        public void Apply()
        {
            if (die == null) return;

            Vector3 p = die.position;
            transform.position = new Vector3(p.x, p.y, p.z + depthOffset);
            transform.rotation = Quaternion.identity;

            // The die's own scale drives the plate, so both stay in step when the camera framer
            // resizes the dice for a different aspect ratio.
            float s = Mathf.Max(die.lossyScale.x, 0.0001f) * scaleMultiplier;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
