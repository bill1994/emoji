// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Time Picker Clock", 100)]
    public class DialogTimePickerClock : MonoBehaviour, IDragHandler, IPointerClickHandler
	{
        #region Private Variables

        [SerializeField] private DialogTimePicker m_TimePicker = null;

		private Vector2 _ClockLocalPosition;

        #endregion

        #region Unity Functions

        protected virtual void Start()
		{
			Init();
		}

        public void OnDrag(PointerEventData eventData)
        {
            HandleData(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleData(eventData);
        }

        #endregion

        #region Helper Functions

        public void Init()
		{
			_ClockLocalPosition = new Vector2(transform.localPosition.x, transform.localPosition.y);
		}

		private void HandleData(PointerEventData eventData)
		{
            Vector2 eventDataLocalPosition = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out eventDataLocalPosition);

            Vector2 clickPosition = eventDataLocalPosition - _ClockLocalPosition;
			float degreeAngle = Mathf.Rad2Deg * Mathf.Atan(clickPosition.y / clickPosition.x);

			if (clickPosition.x < 0) degreeAngle += 180;
			m_TimePicker.SetAngle(degreeAngle);
		}

        #endregion
    }
}
