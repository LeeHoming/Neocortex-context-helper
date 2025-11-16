using UnityEngine;
using UnityEngine.EventSystems;

namespace NeocortexSpeechContext
{
    /// <summary>
    /// Helper component that routes UI pointer events to the voice input controller so students can swap in their own button visuals easily.
    /// </summary>
    public class VoiceInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private VoiceInputModeController controller;

        public void Initialise(VoiceInputModeController target)
        {
            controller = target;
        }

        public void Cleanup()
        {
            controller = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (controller == null)
            {
                return;
            }

            if (controller.CurrentMode == VoiceInputModeController.ListeningMode.PushToTalk)
            {
                controller.HandleButtonPress();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (controller == null)
            {
                return;
            }

            if (controller.CurrentMode == VoiceInputModeController.ListeningMode.PushToTalk)
            {
                controller.HandleButtonRelease();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller == null)
            {
                return;
            }

            if (controller.CurrentMode != VoiceInputModeController.ListeningMode.PushToTalk)
            {
                controller.HandleButtonPress();
            }
        }
    }
}
