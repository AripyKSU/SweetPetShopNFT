using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PetShopTheme : MonoBehaviour
{
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public TMP_FontAsset font;

    public void StyleDialog(GameObject root)
    {
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            bool button=image.GetComponent<Button>()!=null;
            if(!button && image.name!="Panel") continue;
            image.sprite=button?buttonSprite:panelSprite;
            image.type=Image.Type.Sliced;
            image.pixelsPerUnitMultiplier=button?7:5;
            image.color=Color.white;
        }
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.font=font;
            text.enableAutoSizing=true;
            text.fontSizeMax=text.fontSize;
            text.fontSizeMin=18;
            text.overflowMode=TextOverflowModes.Truncate;
        }
    }
}
