using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Button))]
public class SubmitButtonController : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text labelText;             // Buton içindeki TMP_Text (örn: "18 pts")

    [Header("Opsiyonel")]
    public bool useCanvasGroup = true;     // Butonla birlikte CanvasGroup'u da senkronla
    public float disabledAlpha = 0.6f;

    private Button _btn;
    private CanvasGroup _cg;               // varsa görsel/raycast senkronu
    private bool _hooked;

    void Awake()
    {
        _btn = GetComponent<Button>();
        if (useCanvasGroup)
            _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        ApplyState(false, "");             // başlangıç: pasif ve boş yazı
        _btn.onClick.AddListener(OnSubmitClicked);
    }

    void OnEnable()
    {
        StartCoroutine(HookWhenReady());
    }

    void OnDisable()
    {
        var am = AnswerManager.Instance;
        if (am != null) am.OnAnswerChanged -= HandleAnswerChanged;
        _btn.onClick.RemoveListener(OnSubmitClicked);
        _hooked = false;
    }

    IEnumerator HookWhenReady()
    {
        // AnswerManager hazır olana kadar (kısa) bekle
        float t = 0f;
        while (AnswerManager.Instance == null && t < 1f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var am = AnswerManager.Instance;
        if (am == null) yield break;

        am.OnAnswerChanged -= HandleAnswerChanged;
        am.OnAnswerChanged += HandleAnswerChanged;

        // İlk durum için zorla tetiklet
        am.ForceNotify();
        _hooked = true;
    }

    // 🔴 Yalnızca currentAnswer değiştiğinde çağrılır
    void HandleAnswerChanged(string word, bool isValid)
    {
        bool canSubmit = false;
        int ptsToShow = 0;

        if (isValid && AnswerManager.Instance != null)
        {
            // Aynı level’da bu kelime daha önce submit edildiyse buton kapalı
            bool already = AnswerManager.Instance.IsAlreadySubmittedThisLevel(word);
            if (!already)
            {
                canSubmit = true;
                ptsToShow = ScoreManager.Instance != null ? ScoreManager.Instance.ComputeWordScore(word) : 0;
            }
        }

        if (canSubmit)
            ApplyState(true, $"{ptsToShow} pts");
        else
            ApplyState(false, ""); // kilitliyken skor yazma
    }

    void ApplyState(bool interactable, string label)
    {
        // Buton tıklanabilirliği
        _btn.interactable = interactable;

        // Yazı
        if (labelText) labelText.text = label;

        // (Opsiyonel) CanvasGroup ile görsel/raycast senkronu
        if (_cg)
        {
            _cg.interactable = interactable;
            _cg.blocksRaycasts = interactable;
            _cg.alpha = interactable ? 1f : disabledAlpha;
        }
    }

    void OnSubmitClicked()
    {
        // güvenlik: sadece geçerliyse ve duplicate değilse submit
        var am = AnswerManager.Instance;
        if (am != null && am.IsCurrentValid && !am.IsAlreadySubmittedThisLevel(am.CurrentAnswer))
            am.SubmitCurrentWord();
    }
}
