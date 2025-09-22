using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Button))]
public class UndoButtonController : MonoBehaviour
{


    [Header("Opsiyonel")]
    public bool useCanvasGroup = true;
    public float disabledAlpha = 0.6f;

    private Button _btn;
    private CanvasGroup _cg;
    private bool _hooked;

    void Awake()
    {
        _btn = GetComponent<Button>();
        if (useCanvasGroup)
            _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        ApplyState(false); 
        _btn.onClick.AddListener(OnUndoClicked);
    }

    void OnEnable()
    {
        StartCoroutine(HookWhenReady());
    }

    void OnDisable()
    {
        var am = AnswerManager.Instance;
        if (am != null) am.OnAnswerChanged -= HandleAnswerChanged;
        _btn.onClick.RemoveListener(OnUndoClicked);
        _hooked = false;
    }

    IEnumerator HookWhenReady()
    {
        float t = 0f;
        while ((AnswerManager.Instance == null || LetterHolderManager.Instance == null) && t < 1f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var am = AnswerManager.Instance;
        if (am == null) yield break;


        am.OnAnswerChanged -= HandleAnswerChanged;
        am.OnAnswerChanged += HandleAnswerChanged;

        HandleAnswerChanged(am.CurrentAnswer, am.IsCurrentValid);
        _hooked = true;
    }

    void HandleAnswerChanged(string _, bool __)
    {
        bool canUndo = LetterHolderManager.Instance != null && LetterHolderManager.Instance.HasAnyOccupied();
        ApplyState(canUndo);
    }

    void ApplyState(bool interactable)
    {
        _btn.interactable = interactable;

        if (_cg)
        {
            _cg.interactable = interactable;
            _cg.blocksRaycasts = interactable;
            _cg.alpha = interactable ? 1f : disabledAlpha;
        }
    }

    void OnUndoClicked()
    {
        LetterHolderManager.Instance?.UndoLastMove();
    }
}
