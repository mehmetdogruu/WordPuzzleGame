using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Button))]
public class SubmitButtonController : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text labelText;

    [Header("Opsiyonel")]
    public bool useCanvasGroup = true;
    public float disabledAlpha = 0.6f;

    Button _btn;
    CanvasGroup _cg;

    void Awake()
    {
        _btn = GetComponent<Button>();
        if (useCanvasGroup) _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        ApplyState(false, "");
        _btn.onClick.AddListener(OnSubmitClicked);
    }

    void OnEnable() => StartCoroutine(HookWhenReady());
    void OnDisable()
    {
        var am = AnswerManager.Instance;
        if (am != null) am.OnAnswerChanged -= HandleAnswerChanged;
        _btn.onClick.RemoveListener(OnSubmitClicked);
    }

    IEnumerator HookWhenReady()
    {
        float t = 0f;
        while (AnswerManager.Instance == null && t < 1f) { t += Time.unscaledDeltaTime; yield return null; }

        var am = AnswerManager.Instance;
        if (am == null) yield break;

        am.OnAnswerChanged -= HandleAnswerChanged;
        am.OnAnswerChanged += HandleAnswerChanged;
        am.ForceNotify();
    }

    void HandleAnswerChanged(string word, bool isValid)
    {
        if (!isValid || AnswerManager.Instance == null) { ApplyState(false, ""); return; }

        bool already = AnswerManager.Instance.IsAlreadySubmittedThisLevel(word);
        if (already) { ApplyState(false, ""); return; }

        int pts = ScoreManager.Instance ? ScoreManager.Instance.ComputeWordScore(word) : 0;
        ApplyState(true, $"{pts} pts");
    }

    void ApplyState(bool interactable, string label)
    {
        _btn.interactable = interactable;
        if (labelText) labelText.text = label;

        if (_cg)
        {
            _cg.interactable = interactable;
            _cg.blocksRaycasts = interactable;
            _cg.alpha = interactable ? 1f : disabledAlpha;
        }
    }

    void OnSubmitClicked()
    {
        var am = AnswerManager.Instance;
        if (am != null && am.IsCurrentValid && !am.IsAlreadySubmittedThisLevel(am.CurrentAnswer))
            am.SubmitCurrentWord();
    }
}
