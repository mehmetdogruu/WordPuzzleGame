using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;   // ✅ DOTween

[RequireComponent(typeof(Button))]
public class TileViewController : MonoBehaviour
{
    public TextMeshProUGUI letterText;
    public TextMeshProUGUI scoreText;
    public GameObject frontFace;
    public GameObject backFace;
    public Button button;
    public CanvasGroup canvasGroup;

    [HideInInspector] public int tileId;
    [HideInInspector] public int tileIndex;
    [HideInInspector] public char letter;

    public Action<TileViewController> OnClicked; // LevelManager set edecek

    private RectTransform _rect;
    private Sequence _flipSeq;
    [SerializeField] private float flipTime = 0.15f;

    [HideInInspector] public bool? IsCurrentlyOpen;  // en son bilinen state


    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!letterText) letterText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (!_rect) _rect = GetComponent<RectTransform>();

        button.onClick.AddListener(() =>
        {
            if (button.interactable)
                OnClicked?.Invoke(this);
        });
    }

    public void Setup(int index, int id, char ch)
    {
        tileIndex = index;
        tileId = id;
        letter = ch;
        IsCurrentlyOpen = null;

        if (letterText)
        {
            letterText.text = char.ToUpperInvariant(ch).ToString();
            letterText.enabled = true;
            letterText.gameObject.SetActive(true);
        }
        if (scoreText && ScoreManager.InstanceExists)
        {
            int point = ScoreManager.Instance.ComputeWordScore(ch.ToString());
            scoreText.text = point.ToString();
            scoreText.enabled = true;
            scoreText.gameObject.SetActive(true);
        }
    }

    /// <summary>Animasyonsuz aç / kapa (eski kullanım için)</summary>
    public void SetOpen(bool isOpen)
    {
        if (frontFace) frontFace.SetActive(isOpen);
        if (backFace) backFace.SetActive(!isOpen);
        if (letterText) letterText.gameObject.SetActive(isOpen);
        if (button) button.interactable = isOpen;

        var cg = canvasGroup ? canvasGroup : GetComponent<CanvasGroup>();
        if (cg) cg.blocksRaycasts = isOpen;
    }

    /// <summary>Açılış animasyonu: Y scale 1→0→1</summary>
    public void AnimateOpen()
    {
        if (_flipSeq != null && _flipSeq.IsActive()) _flipSeq.Kill();
        _flipSeq = DOTween.Sequence();
        _flipSeq.Append(_rect.DOScaleY(0f, flipTime).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    SetOpen(true); // yüzleri değiştir
                })
                .Append(_rect.DOScaleY(1f, flipTime).SetEase(Ease.OutQuad));
    }

    /// <summary>Kapanış animasyonu: Y scale 1→0→1</summary>
    public void AnimateClose()
    {
        if (_flipSeq != null && _flipSeq.IsActive()) _flipSeq.Kill();
        _flipSeq = DOTween.Sequence();
        _flipSeq.Append(_rect.DOScaleY(0f, flipTime).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    SetOpen(false);
                })
                .Append(_rect.DOScaleY(1f, flipTime).SetEase(Ease.OutQuad));
    }

    public void SetRaycastEnabled(bool enabled)
    {
        if (canvasGroup) canvasGroup.blocksRaycasts = enabled;

        // CanvasGroup yoksa garanti olsun diye:
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics) g.raycastTarget = enabled;
    }
}
