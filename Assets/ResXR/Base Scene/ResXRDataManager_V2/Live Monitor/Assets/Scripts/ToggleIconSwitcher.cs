using UnityEngine;
using UnityEngine.UI;

public class ToggleIconSwitcher : MonoBehaviour
{
    public Image icon;
    public Sprite OffIcon;
    public Sprite OnIcon;

    public void SetIcon(bool isOn)
    {
        icon.sprite = isOn ? OnIcon : OffIcon;
    }
}
