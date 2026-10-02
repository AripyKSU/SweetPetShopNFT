#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PetShopUiRefresh
{
    const string Art = "Assets/Art/PetShop/UI/";
    static Sprite panel, button, rounded;
    static readonly Color Ink = new Color32(57, 66, 49, 255);
    static readonly Color Sage = new Color32(70, 100, 76, 255);
    static readonly Color Cream = new Color32(255, 250, 237, 255);
    static Transform canvas;
    static TMP_FontAsset uiFont;

    [MenuItem("Tools/Pet Shop/Apply Polished UI")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before applying UI.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scene/1.unity") throw new InvalidOperationException("Open Assets/Scene/1.unity first.");
        Import("panel_oak.png", new Vector4(190,190,190,190));
        Import("button_sage.png", new Vector4(400,160,400,160));
        panel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "panel_oak.png");
        button = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "button_sage.png");
        if (panel == null || button == null) throw new InvalidOperationException("UI art is missing.");
        rounded = RoundedSprite();
        canvas = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).First().transform;
        const string fontPath="Assets/Font/SDF/PetShopUI-Dynamic.asset";
        uiFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(uiFont==null)
        {
            var sourceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/NotoSansKR-Medium.ttf");
            uiFont=TMP_FontAsset.CreateFontAsset(sourceFont);
            uiFont.name="PetShopUI-Dynamic"; uiFont.atlasPopulationMode=AtlasPopulationMode.Dynamic; uiFont.isMultiAtlasTexturesEnabled=true;
            AssetDatabase.CreateAsset(uiFont,fontPath);
            AssetDatabase.AddObjectToAsset(uiFont.material,uiFont);
            foreach(var texture in uiFont.atlasTextures) AssetDatabase.AddObjectToAsset(texture,uiFont);
        }
        var theme=canvas.GetComponent<PetShopTheme>()??canvas.gameObject.AddComponent<PetShopTheme>();
        uiFont.TryAddCharacters("…0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz /,+-()");
        var latinFallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if(latinFallback!=null && !uiFont.fallbackFontAssetTable.Contains(latinFallback)) uiFont.fallbackFontAssetTable.Add(latinFallback);
        EditorUtility.SetDirty(uiFont);
        theme.panelSprite=rounded; theme.buttonSprite=rounded; theme.font=uiFont;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = 1;
        var main = Find("MainPetArea");
        Anchors(main, Vector2.zero, new Vector2(.72f,1), new Vector2(24,24), new Vector2(-12,-24));
        var right = Find("RightServicePanel");
        Anchors(right, new Vector2(.72f,0), Vector2.one, new Vector2(0,24), new Vector2(-24,-24));
        Flat(right, new Color32(245, 240, 222, 255));

        var market = UnityEngine.Object.FindFirstObjectByType<PortfolioMarketDemo>();
        var auth = UnityEngine.Object.FindFirstObjectByType<UserNamePw>();
        var so = new SerializedObject(market);
        var ao = new SerializedObject(auth);
        var toolbar = UI("PetShopToolbar", main);
        Box(toolbar, 0, 0, 1, 104);
        Flat(toolbar,Cream);
        var brand=Label("ShopBrand",toolbar,"포근한 펫샵",30); Box(brand.transform,.025f,12,.23f,40);
        var coinLabel=Label("CoinCaption",toolbar,"MY COIN",13); Box(coinLabel.transform,.026f,64,.095f,24); coinLabel.color=Sage;
        var coin = Ref<TextMeshProUGUI>(so,"coinText");
        Move(coin.transform,toolbar, .10f,.23f,59,34); coin.text = "0"; TextStyle(coin,26); coin.fontStyle=FontStyles.Bold;
        ButtonAt(Ref<Button>(so,"refreshBtn"),toolbar,.25f,.42f,14,44,"새로고침");
        ButtonAt(Ref<Button>(so,"giveEquipmentBtn"),toolbar,.44f,.61f,14,44,"펫 만나기");
        ButtonAt(Ref<Button>(so,"addCoinBtn"),toolbar,.63f,.79f,14,44,"코인 +100");
        ButtonAt(Ref<Button>(so,"claimBtn"),toolbar,.81f,.98f,14,44,"판매대금 받기");
        var status=Ref<TextMeshProUGUI>(so,"debugLine");
        Move(status.transform,toolbar,.25f,.975f,66,28); status.gameObject.SetActive(true); status.text="펫을 선택하거나 위아래로 옮겨 거래하세요."; TextStyle(status,18);
        Ref<Button>(so,"marketRefreshBtn").gameObject.SetActive(false);

        Area("AuctionFenceArea","AuctionHeader","MarketPetViewport",main,122,416,"입양을 기다리는 펫");
        Area("InventoryArea","InventoryHeader","InventoryPetViewport",main,592,416,"나의 펫 보관소");
        var guide=Find("TransferGuide"); guide.gameObject.SetActive(true); Box(guide,0,545,1,36);
        var guideText=guide.GetComponent<TextMeshProUGUI>();
        guideText.text="위로 판매 / 아래로 입양, 취소     |     펫이 많으면 좌우로 드래그"; TextStyle(guideText,21); guideText.alignment=TextAlignmentOptions.Center;

        var title=Label("ServiceTitle",right,"함께하는 작은 행복",26); Box(title.transform,.065f,16,.94f,38);
        var subtitle=Label("ServiceSubtitle",right,"PET COMPANIONS  /  ADOPTION LOUNGE",12); Box(subtitle.transform,.068f,52,.94f,20); subtitle.color=Sage;
        var login=Ref<GameObject>(ao,"loginPanel").transform;
        login.SetParent(right,false); Box(login,.055f,74,.945f,218); Flat(login,Cream);
        Box(Label("LoginTitle",login,"01  나의 펫샵 시작하기",22).transform,.06f,14,.94f,30);
        InputAt(Ref<TMP_InputField>(ao,"inputID"),login,.06f,.94f,54,42,"아이디");
        InputAt(Ref<TMP_InputField>(ao,"inputPW"),login,.06f,.94f,104,42,"비밀번호");
        ButtonAt(Ref<Button>(ao,"loginBtn"),login,.06f,.48f,158,44,"로그인");
        ButtonAt(Ref<Button>(ao,"siginUpBtn"),login,.52f,.94f,158,44,"회원가입");

        var wallet=Find("WalletPanel"); Box(wallet,.055f,306,.945f,346); Flat(wallet,Cream);
        Box(Label("WalletTitle",wallet,"02  코인과 특별한 만남",22).transform,.06f,14,.94f,30);
        NamedButton("WalletButton",wallet,.06f,.94f,52,44,"지갑 연결");
        NamedText("WalletAddressText",wallet,102,24,"지갑을 연결해 주세요",17);
        NamedText("Sepolia ETH Text",wallet,130,24,"Sepolia ETH",17);
        NamedButton("Buy10000GoldButton",wallet,.06f,.60f,166,46,"10,000 코인 구매");
        NamedButton("PaymentCheckButton",wallet,.64f,.94f,166,46,"결제 확인");
        NamedButton("BuyLegendarySwordButton",wallet,.06f,.60f,222,46,"특별 펫 입양");
        NamedButton("SwordPaymentCheckButton",wallet,.64f,.94f,222,46,"입양 확인");
        NamedText("PaymentStatusText",wallet,278,52,"Sepolia 테스트 네트워크에서 이용해요.",17);

        var nft=Find("MythicNftPanel"); Box(nft,.055f,666,.945f,340); Flat(nft,new Color32(234,239,224,255));
        Box(Label("NftTitle",nft,"03  별빛 페넥여우 · NFT",22).transform,.06f,14,.94f,30);
        NamedButton("NftConnectButton",nft,.06f,.94f,52,44,"NFT 지갑 연결");
        InputAt(Find("NftCouponInput").GetComponent<TMP_InputField>(),nft,.06f,.94f,108,46,"입양 쿠폰 붙여넣기");
        NamedButton("NftRedeemButton",nft,.06f,.48f,166,46,"NFT 입양");
        NamedButton("NftCheckButton",nft,.52f,.94f,166,46,"발급 확인");
        NamedText("NftStatusText",nft,224,98,"나만의 특별한 친구, 별빛 페넥여우.\n입양 쿠폰으로 NFT를 발급받으세요.",18);
        // This welcome card is revealed by the existing login panel's SetActive(false).
        var welcome=UI("MemberWelcome",right); Box(welcome,.055f,74,.945f,218); Flat(welcome,Cream);
        welcome.SetSiblingIndex(0); login.SetAsLastSibling();
        Box(Label("WelcomeTitle",welcome,"오늘도 반가워요",24).transform,.07f,24,.64f,36);
        var welcomeBody=Label("WelcomeBody",welcome,"작은 친구들과 함께\n포근한 하루를 보내세요.",18);
        Box(welcomeBody.transform,.07f,72,.64f,66); welcomeBody.fontStyle=FontStyles.Normal;
        Box(Label("WelcomeFoot",welcome,"펫을 선택해 입양과 판매를 시작하세요",15).transform,.07f,169,.94f,28);
        var portrait=UI("WelcomePet",welcome); Box(portrait,.64f,28,.95f,128);
        var visuals=AssetDatabase.LoadAssetAtPath<ItemVisualData>("Assets/Data/Market/GlobalItemVisuals.asset");
        var portraitImage=portrait.GetComponent<Image>()??portrait.gameObject.AddComponent<Image>();
        portraitImage.sprite=visuals.GetMapping("SWORD").icon; portraitImage.preserveAspect=true; portraitImage.raycastTarget=false;
        Secondary(Ref<Button>(ao,"siginUpBtn"));
        Secondary(Ref<Button>(so,"refreshBtn")); Secondary(Ref<Button>(so,"addCoinBtn"));
        foreach(var name in new[]{"PaymentCheckButton","SwordPaymentCheckButton","NftCheckButton"}) Secondary(Find(name).GetComponent<Button>());
        foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if(text.text.Trim()=="Coin") text.gameObject.SetActive(false);
            text.font=uiFont;
            if (text.GetComponentInParent<TMP_InputField>() != null) continue;
            text.enableAutoSizing=true; text.fontSizeMax=Mathf.Min(32, text.fontSize); text.fontSizeMin=Mathf.Min(16,text.fontSizeMax);
            text.overflowMode=TextOverflowModes.Truncate;
            text.raycastTarget=false;
        }
        foreach(var path in new[]{"Assets/Prefabs/PetShop/PetInventoryDisplay.prefab","Assets/Prefabs/PetShop/PetMarketDisplay.prefab"})
        {
            var prefab=PrefabUtility.LoadPrefabContents(path);
            var price=prefab.transform.Find("Price");
            if(price!=null)
            {
                var tag=UI("PriceTag",prefab.transform); tag.SetSiblingIndex(0);
                tag.gameObject.SetActive(path.EndsWith("PetMarketDisplay.prefab"));
                Skin(tag,panel,Color.white,32);
                foreach(var target in new[]{tag,price})
                {
                    var r=(RectTransform)target; r.anchorMin=r.anchorMax=new Vector2(.5f,.08f); r.pivot=new Vector2(.5f,.5f);
                    r.anchoredPosition=Vector2.zero; r.sizeDelta=new Vector2(220,34);
                }
            }
            foreach(var badgeName in new[]{"NftBadge","OwnedBadge"})
            {
                var badge=prefab.transform.Find(badgeName) as RectTransform;
                if(badge!=null) { badge.anchorMin=badge.anchorMax=new Vector2(.82f,.86f); badge.anchoredPosition=Vector2.zero; }
            }
            foreach(var b in prefab.GetComponentsInChildren<Button>(true)) StyleButton(b,null);
            foreach(var t in prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                bool isButton=t.GetComponentInParent<Button>()!=null;
                TextStyle(t,Mathf.Min(20,t.fontSize)); t.alignment=TextAlignmentOptions.Center;
                if(isButton || t.transform.parent.name.EndsWith("Badge"))t.color=new Color32(255,251,235,255);
            }
            PrefabUtility.SaveAsPrefabAsset(prefab,path); PrefabUtility.UnloadPrefabContents(prefab);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("[PetShopUI] Polished UI saved.");
    }

    static void Area(string name,string header,string viewport,Transform parent,float top,float height,string title)
    {
        var area=Find(name); Box(area,0,top,1,height);
        // Keep the original spatial artwork. Box skins belong to service UI only.
        var environment=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PetShop/Environment/"+
            (name=="AuctionFenceArea"?"fence_auction.png":"rug_inventory.png"));
        Skin(area,environment,Color.white,1); area.GetComponent<Image>().type=Image.Type.Simple;
        var panelImage=Find(viewport).GetComponent<Image>();
        if(panelImage!=null) { panelImage.color=Color.clear; panelImage.raycastTarget=false; }
        var h=Find(header); Box(h,.045f,20,.94f,40); var t=h.GetComponent<TextMeshProUGUI>(); t.text=title; TextStyle(t,27);
        var plaque=UI("SectionPlaque",area); Box(plaque,.025f,12,.40f,54); Flat(plaque,Cream); plaque.SetSiblingIndex(0);
        h.SetAsLastSibling();
        var hint=Label("SectionHint",area,name=="AuctionFenceArea"?"새로운 가족을 만나는 곳":"함께 모은 소중한 친구들",16);
        Box(hint.transform,.48f,23,.94f,32); hint.alignment=TextAlignmentOptions.MidlineRight;
        var outer=Find(viewport); Box(outer,.025f,72,.975f,height-96);
        var scroll=outer.GetComponentInChildren<ScrollRect>(true);
        if(scroll==null) throw new InvalidOperationException("Missing shelf ScrollRect");
        if(scroll.transform!=outer) Anchors(scroll.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        scroll.enabled=true; scroll.horizontal=true; scroll.vertical=false; scroll.movementType=ScrollRect.MovementType.Clamped;
        if(scroll.GetComponent<Image>() is Image outerImage) { outerImage.color=Color.clear; outerImage.raycastTarget=true; }
        if(scroll.GetComponent<Mask>() is Mask outerMask) outerMask.enabled=false;
        Anchors(scroll.viewport,Vector2.zero,Vector2.one,new Vector2(8,26),new Vector2(-8,0));
        var vi=scroll.viewport.GetComponent<Image>(); vi.color=Color.white; vi.raycastTarget=true;
        var mask=scroll.viewport.GetComponent<Mask>(); if(mask!=null) { mask.enabled=true; mask.showMaskGraphic=false; }
        var fitter=scroll.content.GetComponent<ContentSizeFitter>(); if(fitter!=null) fitter.enabled=false;
        if(scroll.content.GetComponent<PetShelfLayout>()==null) scroll.content.gameObject.AddComponent<PetShelfLayout>();
        if(scroll.verticalScrollbar!=null) scroll.verticalScrollbar.gameObject.SetActive(false);
        if(scroll.horizontalScrollbar!=null)
        {
            var bar=scroll.horizontalScrollbar; Anchors(bar.transform,new Vector2(0,0),new Vector2(1,0),new Vector2(24,2),new Vector2(-24,16));
            bar.GetComponent<Image>().color=new Color32(211,196,169,255);
            bar.targetGraphic.color=new Color32(109,137,100,255);
            scroll.horizontalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            bar.gameObject.SetActive(false);
        }
    }
    static void Flat(Transform target,Color color)
    {
        Skin(target,rounded,color,1);
    }
    static void Secondary(Button target)
    {
        target.GetComponent<Image>().color=new Color32(222,231,211,255);
        var text=target.GetComponentInChildren<TextMeshProUGUI>(true); if(text!=null) text.color=Ink;
    }
    static Sprite RoundedSprite()
    {
        const string path=Art+"ui_round.png";
        if(!System.IO.File.Exists(path))
        {
            const int size=64; const float radius=14;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x+.5f-size/2f)-(size/2f-radius),0);
                float dy=Mathf.Max(Mathf.Abs(y+.5f-size/2f)-(size/2f-radius),0);
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f));
            }
            texture.SetPixels(pixels); texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }
        Import("ui_round.png",new Vector4(16,16,16,16));
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Import(string file,Vector4 border)
    {
        AssetDatabase.ImportAsset(Art+file);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+file);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spriteBorder=border; importer.spritePixelsPerUnit=100; importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
    }
    static T Ref<T>(SerializedObject so,string key) where T:UnityEngine.Object => (T)so.FindProperty(key).objectReferenceValue;
    static Transform Find(string name) => canvas.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    static Transform UI(string name,Transform parent)
    {
        var t=parent.Find(name); if(t!=null)return t;
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); return go.transform;
    }
    static TextMeshProUGUI Label(string name,Transform parent,string value,float size)
    {
        var t=UI(name,parent); var text=t.GetComponent<TextMeshProUGUI>()??t.gameObject.AddComponent<TextMeshProUGUI>();
        text.font=Find("AuctionHeader").GetComponent<TextMeshProUGUI>().font;
        text.text=value; TextStyle(text,size); text.fontStyle=FontStyles.Bold; return text;
    }
    static void TextStyle(TextMeshProUGUI text,float size)
    {
        text.enabled=true;
        text.margin=Vector4.zero;
        if(uiFont!=null)text.font=uiFont;
        text.fontSize=size; text.enableAutoSizing=true; text.fontSizeMax=size; text.fontSizeMin=Mathf.Min(16,size);
        text.overflowMode=TextOverflowModes.Truncate; text.color=Ink; text.raycastTarget=false;
        text.alignment=TextAlignmentOptions.MidlineLeft;
    }
    static void Skin(Transform t,Sprite sprite,Color color,float multiplier)
    {
        var image=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>(); image.sprite=sprite; image.type=Image.Type.Sliced;
        image.color=color; image.pixelsPerUnitMultiplier=multiplier; image.raycastTarget=false; image.preserveAspect=false;
    }
    static void StyleButton(Button b,string value)
    {
        Flat(b.transform,Sage); b.GetComponent<Image>().raycastTarget=true; b.targetGraphic=b.GetComponent<Image>();
        var colors=b.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.18f,1.18f,1.10f); colors.pressedColor=new Color(.78f,.85f,.74f); colors.disabledColor=new Color(.72f,.72f,.72f,.65f); b.colors=colors;
        var text=b.GetComponentInChildren<TextMeshProUGUI>(true);
        if(text!=null) { if(value!=null)text.text=value; TextStyle(text,20); text.color=new Color32(255,251,235,255); text.fontStyle=FontStyles.Bold; text.alignment=TextAlignmentOptions.Center; Anchors(text.transform,Vector2.zero,Vector2.one,new Vector2(12,4),new Vector2(-12,-4)); }
    }
    static void ButtonAt(Button b,Transform p,float left,float right,float top,float height,string label) { Move(b.transform,p,left,right,top,height); StyleButton(b,label); }
    static void NamedButton(string name,Transform p,float left,float right,float top,float height,string label) => ButtonAt(Find(name).GetComponent<Button>(),p,left,right,top,height,label);
    static void NamedText(string name,Transform p,float top,float height,string value,float size) { var t=Find(name); Move(t,p,.06f,.94f,top,height); var text=t.GetComponent<TextMeshProUGUI>(); t.gameObject.SetActive(true); TextStyle(text,size); text.text=value; }
    static void InputAt(TMP_InputField input,Transform p,float left,float right,float top,float height,string placeholder)
    {
        Move(input.transform,p,left,right,top,height); Flat(input.transform,new Color32(234,230,215,255)); input.GetComponent<Image>().raycastTarget=true;
        input.lineType=TMP_InputField.LineType.SingleLine; input.textComponent.fontSize=20; input.textComponent.color=new Color32(77,60,42,255);
        Anchors(input.textViewport,Vector2.zero,Vector2.one,new Vector2(18,7),new Vector2(-18,-7));
        input.selectionColor=new Color(.55f,.68f,.49f,.4f); input.caretColor=Sage; input.customCaretColor=true;
        if(input.placeholder is TextMeshProUGUI text) { text.text=placeholder; TextStyle(text,18); text.fontStyle=FontStyles.Normal; text.color=new Color32(112,117,101,255); }
    }
    static void Move(Transform t,Transform p,float left,float right,float top,float height) { t.SetParent(p,false); Box(t,left,top,right,height); }
    static void Box(Transform t,float left,float top,float right,float height) => Anchors(t,new Vector2(left,1),new Vector2(right,1),new Vector2(0,-top-height),new Vector2(0,-top));
    static void Anchors(Transform t,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
    {
        var r=(RectTransform)t; r.anchorMin=min; r.anchorMax=max; r.offsetMin=low; r.offsetMax=high; r.localScale=Vector3.one;
    }
}
#endif
