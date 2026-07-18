// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextToggleIsOnTransition.cs" company="Exit Games GmbH">
// </copyright>
// <summary>
//  Use this on Button texts to have some color transition on the text as well without corrupting button's behaviour.
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExitGames.UtilityScripts
{
    /// <summary>
    ///     Use this on toggles texts to have some color transition on the text depending on the isOnState.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TextToggleIsOnTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Text _text;
        public Color HoverOffColor = Color.black;
        public Color HoverOnColor = Color.black;

        private bool isHover;
        public Color NormalOffColor = Color.black;

        public Color NormalOnColor = Color.white;

        public Toggle toggle;

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHover = true;
            _text.color = toggle.isOn ? HoverOnColor : HoverOffColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHover = false;
            _text.color = toggle.isOn ? NormalOnColor : NormalOffColor;
        }

        public void OnEnable()
        {
            _text = GetComponent<Text>();

            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        public void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(OnValueChanged);
        }

        public void OnValueChanged(bool isOn)
        {
            _text.color = isOn ? isHover ? HoverOnColor : HoverOffColor : isHover ? NormalOnColor : NormalOffColor;
        }
    }
}