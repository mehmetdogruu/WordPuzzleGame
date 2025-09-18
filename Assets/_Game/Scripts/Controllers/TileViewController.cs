using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Button))]
public class TileViewController : MonoBehaviour
{
    public TextMeshProUGUI letterText;
    public GameObject frontFace;
    public GameObject backFace;
    public Button button;

    [HideInInspector] public int tileId;
    [HideInInspector] public int tileIndex;
    [HideInInspector] public char letter;

    public CanvasGroup canvasGroup;


    public Action<TileViewController> OnClicked; // LevelManager set edecek

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!letterText) letterText = GetComponentInChildren<TextMeshProUGUI>(true);
        button.onClick.AddListener(() => { if (button.interactable) OnClicked?.Invoke(this); });
    }

    public void Setup(int index, int id, char ch)
    {
        tileIndex = index; tileId = id; letter = ch;
        if (letterText)
        {
            letterText.text = char.ToUpperInvariant(ch).ToString();
            letterText.enabled = true;
            letterText.gameObject.SetActive(true);
        }
    }

    public void SetOpen(bool isOpen)
    {
        if (frontFace) frontFace.SetActive(isOpen);
        if (backFace) backFace.SetActive(!isOpen);
        if (letterText) letterText.gameObject.SetActive(isOpen);
        if (button) button.interactable = isOpen;
        var cg = GetComponent<CanvasGroup>();
        if (cg) cg.blocksRaycasts = isOpen;
    }
    public void SetRaycastEnabled(bool enabled)
    {
        if (canvasGroup) canvasGroup.blocksRaycasts = enabled;

        // CanvasGroup yoksa veya garanti olsun diye:
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics) g.raycastTarget = enabled;
    }
}
