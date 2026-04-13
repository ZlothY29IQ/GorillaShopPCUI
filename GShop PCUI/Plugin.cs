using BepInEx;
using GorillaNetworking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GShopPCUI;

[BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
public class Plugin : BaseUnityPlugin
{
    private const float itemHeight = 70f;

    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle windowStyle;

    private bool cosmeticsReady;

    private bool guiVisible = false;
    private bool cosmeticsVisible = false;

    private List<CosmeticsController.CosmeticItem> items, filtered;

    private Vector2 scroll;
    private string search = "";

    private Rect windowRect;

    private void Start() => CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs += CosmeticsLoaded;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.capsLockKey.wasPressedThisFrame)
        {
            guiVisible = !guiVisible;

            if (!guiVisible)
            {
                cosmeticsVisible = true;
                scroll = Vector2.zero;
            }
        }
    }

    private void OnGUI()
    {
        if (!cosmeticsReady || !guiVisible)
            return;

        InitStyles();

        windowRect.height = cosmeticsVisible ? 520f : 110f;
        GUI.Window(42069, windowRect, DrawWindow, "Gorilla Shop PC UI - By ZlothY", windowStyle);
    }

    private void InitStyles()
    {
        if (windowStyle != null)
            return;

        windowStyle = new GUIStyle(GUI.skin.window);
        windowStyle.normal.background = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f));
        windowStyle.focused.background = windowStyle.normal.background;
        windowStyle.active.background = windowStyle.normal.background;
        windowStyle.onNormal.background = windowStyle.normal.background;
        windowStyle.onFocused.background = windowStyle.normal.background;
        windowStyle.onActive.background = windowStyle.normal.background;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f));

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = MakeTex(1, 1, new Color(0.25f, 0.25f, 0.25f));
        buttonStyle.hover.background = MakeTex(1, 1, new Color(0.35f, 0.35f, 0.35f));
        buttonStyle.active.background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f));

        textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.normal.background = MakeTex(1, 1, new Color(0.25f, 0.25f, 0.25f));
        textFieldStyle.focused.background = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f));
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Shiny Rocks: " + CosmeticsController.instance.currencyBalance);
        GUILayout.FlexibleSpace();
        GUILayout.Label("FPS: " + Mathf.RoundToInt(1f / Time.deltaTime));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Purchase All Free Items", buttonStyle, GUILayout.Height(30)))
            PurchaseAllFree();

        if (GUILayout.Button(cosmeticsVisible ? "Close" : "Open", buttonStyle, GUILayout.Height(30)))
        {
            cosmeticsVisible = !cosmeticsVisible;
            scroll = Vector2.zero;
        }

        GUILayout.EndHorizontal();

        if (!cosmeticsVisible)
            return;

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));

        string newSearch = GUILayout.TextField(search, textFieldStyle);
        if (newSearch != search)
        {
            search = newSearch;
            string lower = search.ToLower();

            filtered = items.Where(x =>
                string.IsNullOrEmpty(lower) ||
                (x.overrideDisplayName ?? x.displayName).ToLower().Contains(lower)
            ).ToList();

            scroll = Vector2.zero;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        float viewHeight = windowRect.height - 120f;
        int totalCount = filtered.Count;

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(viewHeight));

        int firstIndex = Mathf.FloorToInt(scroll.y / itemHeight);
        int visibleCount = Mathf.CeilToInt(viewHeight / itemHeight) + 2;
        int lastIndex = Mathf.Min(firstIndex + visibleCount, totalCount);

        GUILayout.Space(firstIndex * itemHeight);

        for (int i = firstIndex; i < lastIndex; i++)
        {
            var item = filtered[i];

            if (CosmeticsController.instance.unlockedCosmetics.Contains(item))
                continue;

            DrawItem(item);
        }

        GUILayout.Space((totalCount - lastIndex) * itemHeight);

        GUILayout.EndScrollView();
    }

    private void DrawItem(CosmeticsController.CosmeticItem item)
    {
        GUILayout.BeginHorizontal(boxStyle, GUILayout.Height(itemHeight));

        if (item.itemPicture != null)
            DrawSprite(item.itemPicture, 64, 64);

        GUILayout.BeginVertical();
        GUILayout.Label(item.overrideDisplayName ?? item.displayName);
        GUILayout.Label("Price: " + item.cost);
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        int currency = CosmeticsController.instance.currencyBalance;
        Color prev = GUI.backgroundColor;

        GUI.backgroundColor =
            item.cost == 0 ? Color.blue :
            item.cost <= currency ? Color.green :
            Color.red;

        GUILayout.BeginVertical();

        if (GUILayout.Button("Purchase", buttonStyle, GUILayout.Width(110)))
        {
            CosmeticsController.instance.itemToBuy = item;
            CosmeticsController.instance.PurchaseItem();
        }

        GUI.backgroundColor = prev;

        bool inCart = CosmeticsController.instance.currentCart.Contains(item);
        if (GUILayout.Button(inCart ? "Remove" : "Try On", buttonStyle, GUILayout.Width(110)))
        {
            if (inCart)
                CosmeticsController.instance.currentCart.Remove(item);
            else
                CosmeticsController.instance.currentCart.Add(item);

            CosmeticsController.instance.UpdateShoppingCart();
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void PurchaseAllFree()
    {
        foreach (var item in items.Where(x => x.cost == 0 &&
                                             !CosmeticsController.instance.unlockedCosmetics.Contains(x)))
        {
            CosmeticsController.instance.itemToBuy = item;
            CosmeticsController.instance.PurchaseItem();
        }
    }

    private void CosmeticsLoaded()
    {
        windowRect = new Rect(Screen.width / 2f - 350, 20, 700, 520);

        items = CosmeticsController.instance.allCosmetics
            .Where(x => x.canTryOn)
            .ToList();

        filtered = new List<CosmeticsController.CosmeticItem>(items);
        cosmeticsReady = true;
    }

    private static void DrawSprite(Sprite sprite, float w, float h)
    {
        Rect r = GUILayoutUtility.GetRect(w, h);
        Rect tr = sprite.textureRect;
        Rect uv = new(
            tr.x / sprite.texture.width,
            tr.y / sprite.texture.height,
            tr.width / sprite.texture.width,
            tr.height / sprite.texture.height
        );

        GUI.DrawTextureWithTexCoords(r, sprite.texture, uv);
    }

    private static Texture2D MakeTex(int w, int h, Color c)
    {
        Texture2D t = new(w, h);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
