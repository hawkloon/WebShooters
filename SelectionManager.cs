using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WebShooters
{
    public class SelectionManager : MonoBehaviour
    {
        // Start is called before the first frame update
        public Text Name;
        public Text Description;
        public Image Icon;
        void Start()
        {
            
        }

        public void UpdateSelection(WebShooterMode mode)
        {
            gameObject.SetActive(true);
            Name.text = mode.Name;
            Description.text = mode.Description;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
