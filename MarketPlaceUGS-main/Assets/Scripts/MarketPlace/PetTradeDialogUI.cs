using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PetTradeDialogUI : MonoBehaviour
{
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private TMP_InputField priceInput;
    private Button confirmButton;
    private Action<int> confirmed;

    public static PetTradeDialogUI GetOrCreate(Component owner)
    {
        Canvas canvas = owner.GetComponentInParent<Canvas>();
        Transform layer = canvas != null ? canvas.transform.Find("DialogLayer") : null;
        Transform parent = layer != null ? layer : canvas != null ? canvas.transform : owner.transform;
        PetTradeDialogUI existing = parent.GetComponentInChildren<PetTradeDialogUI>(true);
        if (existing != null) return existing;

        GameObject root = CreateObject("PetTradeDialog", parent, typeof(Image), typeof(PetTradeDialogUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);
        Image shade = root.GetComponent<Image>();
        shade.color = new Color(0f, 0f, 0f, .55f);
        shade.raycastTarget = true;

        GameObject panel = CreateObject("Panel", root.transform, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
        panelRect.pivot = new Vector2(.5f, .5f);
        panelRect.sizeDelta = new Vector2(560f, 0f);
        panel.GetComponent<Image>().color = new Color32(250, 241, 218, 255);
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 30, 30);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        PetTradeDialogUI dialog = root.GetComponent<PetTradeDialogUI>();
        dialog.titleText = CreateText("Title", panel.transform, 32, FontStyles.Bold, 52);
        dialog.bodyText = CreateText("Body", panel.transform, 23, FontStyles.Normal, 84);
        dialog.priceInput = CreateInput(panel.transform);

        GameObject buttons = CreateObject("Buttons", panel.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttons.GetComponent<LayoutElement>().preferredHeight = 58;
        HorizontalLayoutGroup buttonsLayout = buttons.GetComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 18;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandWidth = true;
        dialog.confirmButton = CreateButton("확인", buttons.transform, new Color32(101, 139, 91, 255));
        Button cancel = CreateButton("취소", buttons.transform, new Color32(135, 116, 96, 255));
        dialog.confirmButton.onClick.AddListener(dialog.Confirm);
        cancel.onClick.AddListener(dialog.Hide);
        canvas.GetComponent<PetShopTheme>()?.StyleDialog(root);
        root.SetActive(false);
        return dialog;
    }

    public void ShowConfirm(string title, string body, Action onConfirmed)
    {
        Show(title, body, false, 0, _ => onConfirmed?.Invoke());
    }

    public void ShowPrice(string title, string body, int initialPrice, Action<int> onConfirmed)
    {
        Show(title, body, true, initialPrice, onConfirmed);
    }

    private void Show(string title, string body, bool showPrice, int initialPrice, Action<int> callback)
    {
        titleText.text = title;
        bodyText.text = body;
        priceInput.gameObject.SetActive(showPrice);
        priceInput.text = initialPrice.ToString();
        confirmed = callback;
        confirmButton.interactable = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (showPrice) { priceInput.Select(); priceInput.ActivateInputField(); }
    }

    public void Hide()
    {
        confirmed = null;
        gameObject.SetActive(false);
    }

    private void Confirm()
    {
        int value = 0;
        if (priceInput.gameObject.activeSelf && (!int.TryParse(priceInput.text, out value) || value < 1 || value > 1000000))
        {
            bodyText.text = "가격은 1~1,000,000 사이의 정수로 입력해 주세요.";
            return;
        }
        Action<int> callback = confirmed;
        Hide();
        callback?.Invoke(value);
    }

    private static GameObject CreateObject(string name, Transform parent, params Type[] components)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        foreach (Type component in components) go.AddComponent(component);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, FontStyles style, float height)
    {
        GameObject go = CreateObject(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size; text.fontStyle = style; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(76, 54, 38, 255); text.raycastTarget = false;
        go.GetComponent<LayoutElement>().preferredHeight = height;
        return text;
    }

    private static TMP_InputField CreateInput(Transform parent)
    {
        GameObject go = CreateObject("PriceInput", parent, typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        go.GetComponent<Image>().color = Color.white;
        go.GetComponent<LayoutElement>().preferredHeight = 58;
        TextMeshProUGUI text = CreateText("Text", go.transform, 25, FontStyles.Normal, 58);
        Stretch(text.rectTransform); text.margin = new Vector4(16, 8, 16, 8); text.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_InputField input = go.GetComponent<TMP_InputField>();
        input.textComponent = text; input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 7;
        return input;
    }

    private static Button CreateButton(string label, Transform parent, Color color)
    {
        GameObject go = CreateObject(label, parent, typeof(Image), typeof(Button));
        go.GetComponent<Image>().color = color;
        TextMeshProUGUI text = CreateText("Label", go.transform, 24, FontStyles.Bold, 58);
        text.text = label;
        Stretch(text.rectTransform); text.color = Color.white;
        return go.GetComponent<Button>();
    }
}
