using System.Collections.Generic;
using System.Linq;
using BepInEx;
using GorillaNetworking;
using UnityEngine;

namespace GShopPCUI;

[BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
public class SimpleGorillaShop : BaseUnityPlugin
{
    private const float                                  itemHeight = 70f;
    private       GUIStyle                               boxStyle;
    private       GUIStyle                               buttonStyle;
    private       bool                                   cosmeticsReady;
    private       List<CosmeticsController.CosmeticItem> filtered;
    private       List<CosmeticsController.CosmeticItem> items;
    private       bool                                   open;
    private       Vector2                                scroll;
    private       string                                 search = "";
    private       GUIStyle                               textFieldStyle;
    private       Rect                                   windowRect;
    private       GUIStyle                               windowStyle;

    private void Start() =>
            CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs2 += CosmeticsLoaded;

    private void OnGUI()
    {
        if (!cosmeticsReady)
            return;

        if (windowStyle == null)
        {
            windowStyle                      = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background    = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f));
            windowStyle.focused.background   = windowStyle.normal.background;
            windowStyle.active.background    = windowStyle.normal.background;
            windowStyle.onNormal.background  = windowStyle.normal.background;
            windowStyle.onFocused.background = windowStyle.normal.background;
            windowStyle.onActive.background  = windowStyle.normal.background;

            boxStyle                      = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background    = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f));
            boxStyle.focused.background   = boxStyle.normal.background;
            boxStyle.active.background    = boxStyle.normal.background;
            boxStyle.onNormal.background  = boxStyle.normal.background;
            boxStyle.onFocused.background = boxStyle.normal.background;
            boxStyle.onActive.background  = boxStyle.normal.background;

            buttonStyle                   = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = MakeTex(1, 1, new Color(0.25f, 0.25f, 0.25f));
            buttonStyle.hover.background  = MakeTex(1, 1, new Color(0.35f, 0.35f, 0.35f));
            buttonStyle.active.background = MakeTex(1, 1, new Color(0.2f,  0.2f,  0.2f));

            textFieldStyle                    = new GUIStyle(GUI.skin.textField);
            textFieldStyle.normal.background  = MakeTex(1, 1, new Color(0.25f, 0.25f, 0.25f));
            textFieldStyle.focused.background = MakeTex(1, 1, new Color(0.3f,  0.3f,  0.3f));
            textFieldStyle.active.background  = textFieldStyle.focused.background;
        }

        windowRect.height = open ? 520f : 110f;
        GUI.Window(42069, windowRect, DrawWindow, "Gorilla Shop PC UI - By ZlothY", windowStyle);
    }

    private void CosmeticsLoaded()
    {
        windowRect = new Rect(Screen.width / 2f - 350, 20, 700, 520);
        items = CosmeticsController.instance.allCosmetics
                                   .Where(x => x.canTryOn)
                                   .ToList();

        filtered       = new List<CosmeticsController.CosmeticItem>(items);
        cosmeticsReady = true;
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Shiny Rocks: " + CosmeticsController.instance.currencyBalance);
        GUILayout.FlexibleSpace();
        GUILayout.Label("FPS: " + GetFPS());
        GUILayout.EndHorizontal();
        
        GUILayout.BeginVertical();
        buttonStyle.richText = true;
        GUILayout.Label("<color=red>This WILL cause your game to lag having all of the cosmetics showing!</color>");
        GUILayout.Space(5);

        if (!open)
        {
            if (GUILayout.Button("Open", buttonStyle, GUILayout.Height(30)))
                open = true;

            GUILayout.EndVertical();

            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Purchase All Free Items", buttonStyle, GUILayout.Height(30)))
            PurchaseAllFree();

        if (GUILayout.Button("Close", buttonStyle, GUILayout.Height(30)))
        {
            open   = false;
            scroll = Vector2.zero;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        string newSearch = GUILayout.TextField(search, textFieldStyle);
        if (newSearch != search)
        {
            search = newSearch;
            filtered = items.Where(x =>
                                           string.IsNullOrEmpty(search) ||
                                           (x.overrideDisplayName ?? x.displayName).ToLower().Contains(search.ToLower())
            ).ToList();

            scroll = Vector2.zero;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(windowRect.height - 120f));
        int currency = CosmeticsController.instance.currencyBalance;

        foreach (CosmeticsController.CosmeticItem item in filtered.Where(item => !CosmeticsController.instance
                                                                                .unlockedCosmetics.Contains(item)))
        {
            GUILayout.BeginHorizontal(boxStyle, GUILayout.Height(itemHeight));

            if (item.itemPicture != null)
                DrawSprite(item.itemPicture, 64, 64);

            GUILayout.BeginVertical();
            GUILayout.Label(item.overrideDisplayName ?? item.displayName);
            GUILayout.Label("Price: " + item.cost);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            Color prev = GUI.backgroundColor;
            if (item.cost == 0)
                GUI.backgroundColor = Color.blue;
            else if (item.cost <= currency)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = Color.red;

            GUILayout.BeginVertical();
            if (GUILayout.Button("Purchase", buttonStyle, GUILayout.Width(110)))
            {
                CosmeticsController.instance.itemToBuy = item;
                CosmeticsController.instance.PurchaseItem();
            }

            GUI.backgroundColor = prev;

            bool   isInCart   = CosmeticsController.instance.currentCart.Contains(item);
            string tryOnLabel = isInCart ? "Remove" : "Try On";

            if (GUILayout.Button(tryOnLabel, buttonStyle, GUILayout.Width(110)))
            {
                if (isInCart)
                    CosmeticsController.instance.currentCart.Remove(item);
                else
                    CosmeticsController.instance.currentCart.Add(item);

                CosmeticsController.instance.UpdateShoppingCart();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void PurchaseAllFree()
    {
        foreach (CosmeticsController.CosmeticItem item in items.Where(item => item.cost == 0 &&
                                                                              !CosmeticsController.instance
                                                                                     .unlockedCosmetics.Contains(item)))
        {
            if (!item.canTryOn)
                continue;

            CosmeticsController.instance.itemToBuy = item;
            CosmeticsController.instance.PurchaseItem();
        }
    }

    private static void DrawSprite(Sprite sprite, float w, float h)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Rect r  = GUILayoutUtility.GetRect(w, h);
        Rect tr = sprite.textureRect;
        Rect uv = new(
                tr.x      / sprite.texture.width,
                tr.y      / sprite.texture.height,
                tr.width  / sprite.texture.width,
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

    private string GetFPS() => Mathf.RoundToInt(1.0f / Time.deltaTime).ToString();
}