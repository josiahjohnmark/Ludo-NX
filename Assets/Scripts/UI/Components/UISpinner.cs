using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>Spins its transform continuously. Uses unscaled time so it keeps turning while paused.</summary>
    public class UISpinner : MonoBehaviour
    {
        public float degreesPerSecond = 360f;

        private void Update()
        {
            transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
