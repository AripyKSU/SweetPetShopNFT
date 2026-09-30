#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PetShopUiValidation
{
    const string Request = "Temp/petshop-ui-request.txt";
    static PetShopUiValidation() { EditorApplication.update += ProcessRequest; }
    static void ProcessRequest()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        string command;
        try { command=File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch(IOException) { return; }
        try
        {
            if(command=="apply") PetShopUiRefresh.Apply();
            if(command=="validate") Validate();
            File.WriteAllText("Temp/petshop-ui-result.txt", "SUCCESS " + command);
        }
        catch(Exception e) { File.WriteAllText("Temp/petshop-ui-result.txt",e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Pet Shop/Validate and Capture UI")]
    public static void Validate()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var source=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.isRootCanvas);
        var clone=UnityEngine.Object.Instantiate(source.gameObject); clone.name="PetShopValidationCanvas";
        var canvas=clone.GetComponent<Canvas>();
        var cameraObject=new GameObject("PetShopValidationCamera",typeof(Camera));
        var camera=cameraObject.GetComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(245,232,209,255);
        camera.orthographic=true; camera.nearClipPlane=.1f; camera.farClipPlane=100; camera.cullingMask=1<<5;
        camera.transform.position=new Vector3(0,0,-10);
        canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
        bool originalActive=source.gameObject.activeSelf; source.gameObject.SetActive(false);
        var report=new List<string>();
        var created=new List<GameObject>();
        try
        {
            Directory.CreateDirectory("Temp/PetShopUI");
            var mapping=AssetDatabase.LoadAssetAtPath<ItemVisualData>("Assets/Data/Market/GlobalItemVisuals.asset");
            var scrolls=clone.GetComponentsInChildren<ScrollRect>(true).Where(s=>s.content!=null && s.content.GetComponent<GridLayoutGroup>()!=null).ToArray();
            if(scrolls.Length!=2) throw new Exception("Expected two pet shelves; got " + scrolls.Length);
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
            {
                var rt=new RenderTexture(size.x,size.y,24); rt.Create(); camera.targetTexture=rt;
                canvas.GetComponent<CanvasScaler>().SendMessage("Handle",SendMessageOptions.DontRequireReceiver);
                Canvas.ForceUpdateCanvases();
                foreach(int count in new[]{0,1,4,5,30})
                {
                    foreach(var go in created) UnityEngine.Object.DestroyImmediate(go); created.Clear();
                    foreach(var scroll in scrolls)
                    {
                        bool inventory=scroll.transform.parent.name.Contains("Inventory") || scroll.name.Contains("Inventory");
                        string path="Assets/Prefabs/PetShop/"+(inventory?"PetInventoryDisplay":"PetMarketDisplay")+".prefab";
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        for(int i=0;i<count;i++)
                        {
                            var go=UnityEngine.Object.Instantiate(prefab,scroll.content); created.Add(go);
                            string id=new[]{"SWORD","REDPOTION","BLUEPOTION","LEGENDARY_SWORD","MYTHIC_SWORD_NFT"}[i%5];
                            var visual=mapping.GetMapping(id);
                            go.GetComponent<PetDisplayUI>().Bind(id,visual.icon,false,false,_=>{});
                            var price=go.transform.Find("Price"); if(price!=null) price.GetComponent<TextMeshProUGUI>().text="1,000,000 COIN";
                        }
                        scroll.content.GetComponent<PetShelfLayout>().Refresh();
                        Canvas.ForceUpdateCanvases();
                        bool expected=count*280+Mathf.Max(0,count-1)*24+32>scroll.viewport.rect.width+.5f;
                        if(scroll.horizontal!=expected) throw new Exception("Overflow mismatch");
                        if(scroll.content.rect.width+1<scroll.viewport.rect.width) throw new Exception("Shelf narrower than viewport");
                        scroll.horizontalNormalizedPosition=1; Canvas.ForceUpdateCanvases();
                        if(count>0 && scroll.content.rect.width>scroll.viewport.rect.width && Mathf.Abs(scroll.content.anchoredPosition.x+scroll.content.rect.width-scroll.viewport.rect.width)>2) throw new Exception("Last pet unreachable");
                        scroll.horizontalNormalizedPosition=0;
                        if (count == 5)
                        {
                            var bar = scroll.horizontalScrollbar;
                            if (bar == null || !bar.gameObject.activeInHierarchy)
                                throw new Exception("Horizontal scrollbar missing: " + scroll.transform.parent.name);
                            // Reproduce a ScrollRect re-enable while horizontal scrolling is off.
                            scroll.horizontal = false;
                            scroll.enabled = false;
                            scroll.enabled = true;
                            var shelf = scroll.content.GetComponent<PetShelfLayout>();
                            shelf.enabled = false;
                            shelf.enabled = true;
                            shelf.Refresh();
                            Canvas.ForceUpdateCanvases();
                            if (!scroll.horizontal)
                                throw new Exception("Horizontal scrolling did not resume: " + scroll.transform.parent.name);
                            if (bar.size >= .999f)
                                throw new Exception("Horizontal scrollbar handle did not resize: " + scroll.transform.parent.name);
                            bar.value = 1;
                            Canvas.ForceUpdateCanvases();
                            if (scroll.horizontalNormalizedPosition < .99f)
                                throw new Exception("Scrollbar does not move shelf: " + scroll.transform.parent.name);
                            bar.value = 0;
                            var pointer = new PointerEventData(EventSystem.current);
                            pointer.position = RectTransformUtility.WorldToScreenPoint(camera,
                                bar.handleRect.TransformPoint(bar.handleRect.rect.center));
                            var hits = new List<RaycastResult>();
                            EventSystem.current.RaycastAll(pointer, hits);
                            if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(bar.transform))
                                throw new Exception("Scrollbar pointer blocked: " + scroll.transform.parent.name +
                                    " by " + (hits.Count == 0 ? "nothing" : hits[0].gameObject.name));
                            pointer.button = PointerEventData.InputButton.Left;
                            pointer.pressPosition = pointer.position;
                            pointer.pointerPressRaycast = hits[0];
                            bar.OnBeginDrag(pointer);
                            pointer.position += new Vector2(300, 0);
                            bar.OnDrag(pointer);
                            Canvas.ForceUpdateCanvases();
                            if (scroll.horizontalNormalizedPosition < .01f)
                                throw new Exception("Scrollbar pointer drag did not move shelf: " + scroll.transform.parent.name);
                            bar.value = 0;
                        }
                        if(count==30)
                        {
                            var pet=scroll.content.GetComponentInChildren<PetDisplayUI>(); int drops=0;
                            pet.Bind("SWORD",mapping.GetMapping("SWORD").icon,false,false,_=>drops++);
                            var e=new PointerEventData(EventSystem.current) { button=PointerEventData.InputButton.Left, pressPosition=new Vector2(600,400), position=new Vector2(680,402) };
                            pet.OnInitializePotentialDrag(e); pet.OnBeginDrag(e); e.position=new Vector2(300,402); pet.OnDrag(e); pet.OnEndDrag(e);
                            if(drops!=0 || PetDisplayUI.IsAnyPetDragging) throw new Exception("Horizontal drag triggered a trade or leaked drag state");
                            e.pressPosition=new Vector2(600,400); e.position=new Vector2(603,480);
                            pet.OnInitializePotentialDrag(e); pet.OnBeginDrag(e); pet.OnDrag(e); pet.OnEndDrag(e);
                            if(drops!=1 || PetDisplayUI.IsAnyPetDragging) throw new Exception("Vertical trade drag failed");
                            report.Add("PASS horizontal scroll / vertical trade / drag cleanup");
                        }
                        report.Add(size+" "+scroll.name+" count="+count+" horizontal="+scroll.horizontal+" viewport="+scroll.viewport.rect.size+" content="+scroll.content.rect.size);
                    }
                    if(count==5)
                    {
                        Canvas.ForceUpdateCanvases(); camera.Render();
                        var previous=RenderTexture.active; RenderTexture.active=rt;
                        var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                        File.WriteAllBytes("Temp/PetShopUI/ui-"+size.x+"x"+size.y+".png",image.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(image); RenderTexture.active=previous;
                    }
                }
                camera.targetTexture=null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
            File.WriteAllLines("Temp/PetShopUI/validation.txt",report);
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); UnityEngine.Object.DestroyImmediate(cameraObject); source.gameObject.SetActive(originalActive); }
    }
}
#endif
