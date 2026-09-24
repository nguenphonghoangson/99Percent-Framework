using UnityEditor;
using UnityEngine;
using NinetyNine.Features.Home;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using NinetyNine.UI;

public class LinkLuong
{
    [MenuItem("NinetyNine/Setup/Link LayerLab Home")]
    public static void LinkHome()
    {
        string lobbyPath = "Assets/Layer Lab/GUI Pro-CasualGame/Prefabs/Prefabs_DemoScene_Panels/Lobby.prefab";
        string stagePath = "Assets/Layer Lab/GUI Pro-CasualGame/Prefabs/Prefabs_DemoScene_Panels/Stage_Select_Type2.prefab";
        string targetHudPath = "Assets/Game/Features/Home/UI/MainHud.prefab";
        string targetHomePath = "Assets/Game/Features/Home/UI/HomeScreen.prefab";
        string targetMapPath = "Assets/Game/Features/Home/UI/MapScreen.prefab";
        
        var icons = AssetDatabase.LoadAssetAtPath<UIIconSet>("Assets/Game/Content/UI/UIIconSet.asset");

        // 1. Link MainHud (Strip background)
        GameObject hudGo = PrefabUtility.LoadPrefabContents(lobbyPath);
        if (hudGo.TryGetComponent<MainHudView>(out var oldHud)) Object.DestroyImmediate(oldHud);
        if (hudGo.TryGetComponent<HomeScreenView>(out var oldHome1)) Object.DestroyImmediate(oldHome1); // Just in case
        MainHudView hud = hudGo.AddComponent<MainHudView>();
        var soHud = new SerializedObject(hud);
        
        string[] toDelete = { "Background", "Floor", "Character_Shadow", "Character", 
                              "Image_GoldenPass", "GoldenPass_Info", "Chest", "Chest_Info", 
                              "Fx_Rotate_Light01", "Fx_Rotate_Glow", "User_Info", "Button_Settings", "Bottom" };
        foreach (var name in toDelete) {
            var obj = FindChild(hudGo, name);
            if (obj != null) Object.DestroyImmediate(obj);
        }

        var topGrp = FindChild(hudGo, "StatusBar_Group");
        if (topGrp) soHud.FindProperty("topBar").objectReferenceValue = topGrp;

        var statsEnergy = FindChild(hudGo, "Stats_Energy");
        if (statsEnergy) {
            soHud.FindProperty("livesGroup").objectReferenceValue = statsEnergy;
            var txt = FindChild(statsEnergy, "Text_Value");
            if (txt) soHud.FindProperty("livesText").objectReferenceValue = txt.GetComponent<TMP_Text>();
        }
        var statsGold = FindChild(hudGo, "Stats_Gold");
        if (statsGold) {
            var txt = FindChild(statsGold, "Text_Value");
            if (txt) soHud.FindProperty("coinText").objectReferenceValue = txt.GetComponent<TMP_Text>();
            var addBtn = FindChild(statsGold, "Button_Add");
            if (addBtn) soHud.FindProperty("addCoinsButton").objectReferenceValue = addBtn.GetComponent<Button>();
        }
        
        soHud.FindProperty("icons").objectReferenceValue = icons;
        soHud.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(hudGo, targetHudPath);
        PrefabUtility.UnloadPrefabContents(hudGo);
        
        // 2. Link HomeScreen (Lobby with background, Bottom bar, but NO HUD)
        GameObject lobbyGo = PrefabUtility.LoadPrefabContents(lobbyPath);
        if (lobbyGo.TryGetComponent<MainHudView>(out var oldHud2)) Object.DestroyImmediate(oldHud2);
        if (lobbyGo.TryGetComponent<HomeScreenView>(out var oldHome2)) Object.DestroyImmediate(oldHome2);
        HomeScreenView home = lobbyGo.AddComponent<HomeScreenView>();
        var soHome = new SerializedObject(home);
        
        string[] hudToDelete = { "StatusBar_Group" };
        foreach (var name in hudToDelete) {
            var obj = FindChild(lobbyGo, name);
            if (obj != null) Object.DestroyImmediate(obj);
        }
        
        var playBtn = FindChild(lobbyGo, "Button_Stage") ?? FindChild(lobbyGo, "Play_Continue") ?? FindChild(lobbyGo, "Button_Play");
        if (playBtn) {
            soHome.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<Button>();
        }

        var settingsBtn = FindChild(lobbyGo, "Button_Settings");
        if (settingsBtn) soHome.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
        
        var shopBtn = FindChild(lobbyGo, "Button_Shop");
        if (shopBtn) soHome.FindProperty("shopTab").objectReferenceValue = shopBtn.GetComponent<Button>();
        var rankBtn = FindChild(lobbyGo, "Button_Ranking");
        if (rankBtn) soHome.FindProperty("trophyTab").objectReferenceValue = rankBtn.GetComponent<Button>();
        
        var newHudPrefab = AssetDatabase.LoadAssetAtPath<MainHudView>(targetHudPath);
        soHome.FindProperty("hudPrefab").objectReferenceValue = newHudPrefab;
        soHome.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(lobbyGo, targetHomePath);
        PrefabUtility.UnloadPrefabContents(lobbyGo);
        
        // 3. Link MapScreen (Stage_Select_Type2 without HUD)
        GameObject stageGo = PrefabUtility.LoadPrefabContents(stagePath);
        if (stageGo.TryGetComponent<MapScreenView>(out var oldMap)) Object.DestroyImmediate(oldMap);
        if (stageGo.TryGetComponent<HomeScreenView>(out var oldHome3)) Object.DestroyImmediate(oldHome3);
        MapScreenView map = stageGo.AddComponent<MapScreenView>();
        var soMap = new SerializedObject(map);
        
        foreach (var name in hudToDelete) {
            var obj = FindChild(stageGo, name);
            if (obj != null) Object.DestroyImmediate(obj);
        }
        
        var stages = stageGo.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Stage")
            .Select(t => t.GetComponentInChildren<TMP_Text>())
            .Where(t => t != null)
            .OrderByDescending(t => t.transform.position.y)
            .ToArray();
        System.Array.Sort(stages, (a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        
        var levelNodesProp = soMap.FindProperty("levelNodes");
        levelNodesProp.arraySize = stages.Length;
        for (int i = 0; i < stages.Length; i++) {
            levelNodesProp.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        }
        
        var mapPlayBtn = FindChild(stageGo, "Button_Play") ?? FindChild(stageGo, "Play_Continue") ?? FindChild(stageGo, "Play");
        if (mapPlayBtn) {
            soMap.FindProperty("playButton").objectReferenceValue = mapPlayBtn.GetComponent<Button>();
            var label = mapPlayBtn.GetComponentInChildren<TMP_Text>();
            if (label) soMap.FindProperty("playButtonLabel").objectReferenceValue = label;
        }
        soMap.FindProperty("icons").objectReferenceValue = icons;
        soMap.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(stageGo, targetMapPath);
        PrefabUtility.UnloadPrefabContents(stageGo);
        
        // 4. Update Catalog
        var catalog = AssetDatabase.LoadAssetAtPath<UICatalog>("Assets/Game/Content/UI/HomeUICatalog.asset");
        if (catalog != null) {
            var entry = catalog.Find("map");
            if (entry == null) {
                entry = new UICatalogEntry { key = "map" };
                catalog.entries.Add(entry);
            }
            entry.prefab = AssetDatabase.LoadAssetAtPath<UIView>(targetMapPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
        
        Debug.Log("Lobby, HUD, and Map linked perfectly!");
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
