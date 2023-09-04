using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WebShooters
{
    public class WebModeButton : Button
    {
        // Start is called before the first frame update
        public WebSelection webSelection;
        public WebShooterMode shooterMode;

        public WebShooterMono acitveMono;


        protected override void Awake()
        {
            base.Awake();
            Debug.Log($"Web Mode Button Made!");
        }

        private IEnumerator CenterFadeOut()
        {
            var center = webSelection.center.GetComponent<Image>();
            while(center.color.a > 0)
            {
                center.color = center.color - new Color(0, 0, 0, 0.05f);
                yield return new WaitForSeconds(0.01f);
            }
            webSelection.selectionManager.UpdateSelection(shooterMode);
            webSelection.activeFadeOut = null;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            Debug.Log($"{shooterMode.Name} was clicked!");
            acitveMono.currentMode = shooterMode;

        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            webSelection.normalWebs.onClick.RemoveAllListeners();
            webSelection.normalWebs.gameObject.SetActive(false);
            webSelection.activeFadeOut = StartCoroutine(CenterFadeOut());
        }


        public Color colorReset(Color c, float DesiredAlpha)
        {
            Color color;

            color = new Color(c.r, c.g, c.b, DesiredAlpha);
            return color;
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (webSelection.activeFadeOut != null)
            {
                StopCoroutine(webSelection.activeFadeOut);
                webSelection.activeFadeOut = null;       
            }
            var e = webSelection.center.GetComponent<Image>();
            e.color = colorReset(e.color, 0.75f);
            webSelection.selectionManager.gameObject.SetActive(false);
            webSelection.normalWebs.gameObject.SetActive(true);
            webSelection.normalWebs.onClick.AddListener(acitveMono.ModeReset);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
