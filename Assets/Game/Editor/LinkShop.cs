using UnityEditor;
using UnityEngine;
using NinetyNine.Features.Shop.UI;
using TMPro;
using UnityEngine.UI;
using NinetyNine.UI;
using NinetyNine.Features.Profile;
using NinetyNine.Features.Home;

public class LinkShop
{
    [MenuItem("NinetyNine/Setup/Link LayerLab Shop and Profile")]
    public static void Link()
    {
        string shopPath = "Assets/Layer Lab/GUI Pro-CasualGame/Prefabs/Prefabs_DemoScene_Panels/Shop.prefab";
        string profilePath = "Assets/Layer Lab/GUI Pro-CasualGame/Prefabs/Prefabs_DemoScene_Panels/Popup_UserInfo.prefab";
        string targetShopPath = "Assets/Game/Features/Shop/UI/ShopScreen.prefab";
        string targetProfilePath = "Assets/Game/Features/Profile/UI/ProfilePopup.prefab";
        var icons = AssetDatabase.LoadAssetAtPath<UIIconSet>("Assets/Game/Content/UI/UIIconSet.asset");

        // 1. Link ShopScreen
        GameObject shopGo = PrefabUtility.LoadPrefabContents(shopPath);
        if (shopGo.TryGetComponent<ShopScreen>(out var oldShop)) Object.DestroyImmediate(oldShop);
        ShopScreen shop = shopGo.AddComponent<ShopScreen>();
        var soShop = new SerializedObject(shop);
        
        string[] hudToDelete = { "StatusBar_Group", "Bottom", "Button_Home" };
        foreach (var name in hudToDelete) {
            var obj = FindChild(shopGo, name);
            if (obj != null) Object.DestroyImmediate(obj);
        }

        var content = FindChild(shopGo, "Content");
        if (content) {
            soShop.FindProperty("content").objectReferenceValue = content.GetComponent<RectTransform>();
            
            var header = FindChild(content, "Text_Title") ?? FindChild(content, "Group_First");
            if (header) {
                header.transform.SetParent(content.transform, false); // Make direct child
                soShop.FindProperty("sectionHeaderTemplate").objectReferenceValue = header;
            }
            
            var itemGold = FindChild(content, "Item_Gold");
            if (itemGold) {
                itemGold.transform.SetParent(content.transform, false); // Make direct child
                var itemComp = itemGold.AddComponent<ShopProductItem>();
                var soItem = new SerializedObject(itemComp);
                
                var icon = FindChild(itemGold, "Image_Gold") ?? FindChild(itemGold, "Image_Gem");
                if (icon) soItem.FindProperty("icon").objectReferenceValue = icon.GetComponent<Image>();
                
                var amt = FindChild(itemGold, "Text_Value") ?? FindChild(itemGold, "Text_Gold") ?? FindChild(itemGold, "Text_Count") ?? FindChild(itemGold, "Text_Gem");
                if (amt) soItem.FindProperty("amountText").objectReferenceValue = amt.GetComponent<TMP_Text>();
                
                var price = FindChild(itemGold, "Text_Cost");
                if (price) soItem.FindProperty("priceText").objectReferenceValue = price.GetComponent<TMP_Text>();
                
                var buyBtnGo = FindChild(itemGold, "Button_Add");
                var buyBtn = buyBtnGo != null ? buyBtnGo.GetComponent<Button>() : itemGold.GetComponentInChildren<Button>();
                if (buyBtn == null) buyBtn = itemGold.GetComponent<Button>();
                if (buyBtn == null) buyBtn = itemGold.AddComponent<Button>();
                soItem.FindProperty("buyButton").objectReferenceValue = buyBtn;
                
                soItem.ApplyModifiedProperties();
                soShop.FindProperty("itemPrefab").objectReferenceValue = itemComp;
            }
            
            // Create a grid template
            var gridObj = new GameObject("GridTemplate", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            gridObj.transform.SetParent(content.transform, false);
            var hg = gridObj.GetComponent<HorizontalLayoutGroup>();
            hg.spacing = 20;
            hg.childAlignment = TextAnchor.MiddleCenter;
            soShop.FindProperty("sectionGridTemplate").objectReferenceValue = gridObj.GetComponent<RectTransform>();

            // Clear all other children in Content
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in content.transform) {
                if (child.gameObject != header && child.gameObject != itemGold && child.gameObject != gridObj) {
                    children.Add(child.gameObject);
                }
            }
            foreach (var c in children) Object.DestroyImmediate(c);
        }
        
        var listRoot = FindChild(shopGo, "ScrollRect");
        if (listRoot) soShop.FindProperty("listRoot").objectReferenceValue = listRoot;
        
        var messageText = FindChild(shopGo, "Text_PanelName");
        if (messageText) soShop.FindProperty("messageText").objectReferenceValue = messageText.GetComponent<TMP_Text>();

        soShop.FindProperty("icons").objectReferenceValue = icons;

        // Add Close Button to return
        var closeBtn = FindChild(shopGo, "Button_Back") ?? FindChild(shopGo, "Close");
        if (closeBtn != null) {
            var btn = closeBtn.GetComponent<UnityEngine.UI.Button>();
            if (!btn) btn = closeBtn.AddComponent<UnityEngine.UI.Button>();
            soShop.FindProperty("closeButton").objectReferenceValue = btn;
        }
        
        soShop.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(shopGo, targetShopPath);
        PrefabUtility.UnloadPrefabContents(shopGo);
        
        // 2. Link Profile Popup
        GameObject profileGo = PrefabUtility.LoadPrefabContents(profilePath);
        if (profileGo.TryGetComponent<NinetyNine.Features.Profile.UI.ProfilePopup>(out var oldProfile)) Object.DestroyImmediate(oldProfile);
        var profile = profileGo.AddComponent<NinetyNine.Features.Profile.UI.ProfilePopup>();
        var soProfile = new SerializedObject(profile);
        
        var nameText = FindChild(profileGo, "Text_Name");
        if (nameText) {
            var input = nameText.GetComponent<TMP_InputField>();
            if (input == null) {
                input = nameText.AddComponent<TMP_InputField>();
                input.textComponent = nameText.GetComponent<TMP_Text>();
            }
            soProfile.FindProperty("nameInput").objectReferenceValue = input;
        }
        
        var closeProfile = FindChild(profileGo, "Button_Back") ?? FindChild(profileGo, "Close");
        if (closeProfile) soProfile.FindProperty("closeButton").objectReferenceValue = closeProfile.GetComponent<Button>();
        
        var saveProfile = FindChild(profileGo, "Button_Save") ?? FindChild(profileGo, "Button_Ok");
        if (saveProfile) soProfile.FindProperty("saveButton").objectReferenceValue = saveProfile.GetComponent<Button>();
        
        var previewIcon = FindChild(profileGo, "Image_Character") ?? FindChild(profileGo, "Icon_Profile") ?? FindChild(profileGo, "Icon");
        if (previewIcon) soProfile.FindProperty("previewIcon").objectReferenceValue = previewIcon.GetComponent<Image>();
        
        // Find avatar options list
        var contentProf = FindChild(profileGo, "Content");
        if (contentProf) {
            soProfile.FindProperty("grid").objectReferenceValue = contentProf.GetComponent<RectTransform>();
            var itemProf = FindChild(contentProf, "Item_Avatar") ?? FindChild(contentProf, "Icon");
            if (itemProf) {
                var itemComp = itemProf.AddComponent<NinetyNine.Features.Profile.UI.ProfileAvatarOption>();
                var soItem = new SerializedObject(itemComp);
                soItem.ApplyModifiedProperties();
                soProfile.FindProperty("optionTemplate").objectReferenceValue = itemComp;
            }
        }
        
        soProfile.FindProperty("icons").objectReferenceValue = icons;
        soProfile.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(profileGo, targetProfilePath);
        PrefabUtility.UnloadPrefabContents(profileGo);
        
        // 3. Update Catalogs
        var catalog = AssetDatabase.LoadAssetAtPath<UICatalog>("Assets/Game/Content/UI/HomeUICatalog.asset");
        if (catalog != null) {
            var entryShop = catalog.Find("shop");
            if (entryShop == null) {
                entryShop = new UICatalogEntry { key = "shop" };
                catalog.entries.Add(entryShop);
            }
            entryShop.prefab = AssetDatabase.LoadAssetAtPath<UIView>(targetShopPath);
            
            var entryProfile = catalog.Find("profile");
            if (entryProfile == null) {
                entryProfile = new UICatalogEntry { key = "profile" };
                catalog.entries.Add(entryProfile);
            }
            entryProfile.prefab = AssetDatabase.LoadAssetAtPath<UIView>(targetProfilePath);
            
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
        
        Debug.Log("Shop and Profile linked perfectly!");
    }
    
    private static GameObject FindChild(GameObject parent, string name)
    {
        var transforms = parent.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms) {
            if (t.name == name) return t.gameObject;
        }
        return null;
    }
}
