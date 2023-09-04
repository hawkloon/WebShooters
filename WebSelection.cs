using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyButtons;
using ThunderRoad;

namespace WebShooters
{
    public class WebSelection : MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject center;

        public SelectionManager selectionManager;
        public GameObject anchorPointsParent;
        public GameObject TemplateButton;

        public Coroutine activeFadeOut;

        public Transform[] anchorPoints;
        public Button normalWebs;
        void Start()
        {
            selectionManager.gameObject.SetActive(false);
            anchorPoints = anchorPointsParent.transform.GetComponentsInChildren<Transform>();
            Debug.Log($"{anchorPoints.Length} anchors found!");
            foreach (Transform transform in anchorPoints)
            {
                Debug.Log(transform.gameObject.name);
            }
            PointerInputModule.SetUICameraToAllCanvas();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
