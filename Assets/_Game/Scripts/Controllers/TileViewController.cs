using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;

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

    public Action<TileViewController> OnClicked;

    RectTransform _rect;
    Sequence _flipSeq;
    [SerializeField] float flipTime = 0.15f;

    public bool? IsCurrentlyOpen;

    void Awake()
    {
        button = button ? button : GetComponent<Button>();
        letterText = letterText ? letterText : GetComponentInChildren<TextMeshProUGUI>(true);
        _rect = _rect ? _rect : GetComponent<RectTransform>();

        button.onClick.AddListener(() =>
        {
            if (button.interactable) OnClicked?.Invoke(this);
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
        if (scoreText && ScoreManager.Instance!=null)
        {
            scoreText.text = ScoreManager.Instance.ComputeWordScore(ch.ToString()).ToString();
            scoreText.enabled = true;
            scoreText.gameObject.SetActive(true);
        }
    }

    public void SetOpen(bool isOpen)
    {
        if (frontFace) frontFace.SetActive(isOpen);
        if (backFace) backFace.SetActive(!isOpen);
        if (letterText) letterText.gameObject.SetActive(isOpen);
        if (button) button.interactable = isOpen;

        var cg = canvasGroup ? canvasGroup : GetComponent<CanvasGroup>();
        if (cg) cg.blocksRaycasts = isOpen;
    }

    public void AnimateOpen()
    {
        if (_flipSeq?.IsActive() == true) _flipSeq.Kill();
        _flipSeq = DOTween.Sequence()
            .Append(_rect.DOScaleY(0f, flipTime).SetEase(Ease.InQuad))
            .AppendCallback(() => SetOpen(true))
            .Append(_rect.DOScaleY(1f, flipTime).SetEase(Ease.OutQuad));
    }

    public void AnimateClose()
    {
        if (_flipSeq?.IsActive() == true) _flipSeq.Kill();
        _flipSeq = DOTween.Sequence()
            .Append(_rect.DOScaleY(0f, flipTime).SetEase(Ease.InQuad))
            .AppendCallback(() => SetOpen(false))
            .Append(_rect.DOScaleY(1f, flipTime).SetEase(Ease.OutQuad));
    }

    public void SetRaycastEnabled(bool enabled)
    {
        if (canvasGroup) canvasGroup.blocksRaycasts = enabled;

        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics) g.raycastTarget = enabled;
    }
}
