using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Utilities
{
    public class FollowMouseSprite : MonoBehaviour
    {
        [SerializeField] private Image followMouseImage;
        [SerializeField] private Sprite clicked, unclicked;
        [SerializeField] private Vector3 offset;


        private void Update()
        {
            followMouseImage.transform.position =
                new Vector3(Input.mousePosition.x + offset.x, Input.mousePosition.y + offset.y, 0 + offset.z);
            if (Input.GetMouseButtonDown(0)) followMouseImage.sprite = clicked;
            if (Input.GetMouseButtonUp(0)) followMouseImage.sprite = unclicked;
        }
    }
}