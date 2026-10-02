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
            bool input=image.GetComponent<TMP_InputField>()!=null;
            if(!button && !input && image.name!="Panel") continue;
            image.sprite=button?buttonSprite:panelSprite;
            image.type=Image.Type.Sliced;
            image.pixelsPerUnitMultiplier=1;
            image.color=input ? new Color32(234,230,215,255) : button ? (image.name=="취소" ? new Color32(222,231,211,255) : new Color32(70,100,76,255)) : new Color32(255,250,237,255);
        }
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.font=font;
            var parentButton=text.GetComponentInParent<Button>();
            text.color=parentButton!=null && parentButton.name!="취소" ? new Color32(255,250,237,255) : new Color32(57,66,49,255);
            text.enableAutoSizing=true;
            text.fontSizeMax=text.fontSize;
            text.fontSizeMin=18;
            text.overflowMode=TextOverflowModes.Truncate;
        }
    }
}
