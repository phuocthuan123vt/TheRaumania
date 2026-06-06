using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BedSleepTrigger : MonoBehaviour
{
    [Header("Sleep Dialog")]
    public GameObject pnlSleepDialog;
    public Button btnYes;
    public Button btnNo;

    private PlayerMovement _playerMovement;
    private Rigidbody2D _playerRigidbody;
    private bool _isSleepDialogOpen;

    private void Awake()
    {
        AutoMapUI();
        HideDialog();
    }

    private void Start()
    {
        AutoMapUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        AutoMapUI();
        StartCoroutine(RemapNextFrame());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AutoMapUI();
        StartCoroutine(RemapNextFrame());
    }

    private System.Collections.IEnumerator RemapNextFrame()
    {
        yield return null;
        AutoMapUI();
    }

    private void AutoMapUI()
    {
        Transform root = transform.root;

        if (pnlSleepDialog == null)
        {
            pnlSleepDialog = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_SleepDialog", "pnlSleepDialog", "pnl_BedDialog", "pnl_DialogSleep");
        }

        if (pnlSleepDialog == null)
        {
            pnlSleepDialog = RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_SleepDialog")
                ?? RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnlSleepDialog")
                ?? RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_BedDialog")
                ?? RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_DialogSleep")
                ?? RuntimeReferenceFinder.FindGameObjectEverywhere("pnl_SleepDialog")
                ?? RuntimeReferenceFinder.FindGameObjectEverywhere("pnlSleepDialog")
                ?? RuntimeReferenceFinder.FindGameObjectEverywhere("pnl_BedDialog")
                ?? RuntimeReferenceFinder.FindGameObjectEverywhere("pnl_DialogSleep");
        }

        if (pnlSleepDialog != null)
        {
            if (btnYes == null)
            {
                btnYes = RuntimeReferenceFinder.FindDeepComponent<Button>(pnlSleepDialog.transform, "btnYes", "btn_Yes", "btn_SleepYes", "btn_OK", "Yes", "btn_Yes_1");
            }

            if (btnNo == null)
            {
                btnNo = RuntimeReferenceFinder.FindDeepComponent<Button>(pnlSleepDialog.transform, "btnNo", "btn_No", "btn_SleepNo", "btn_Cancel", "No", "btn_No_1");
            }
        }

        if (btnYes != null)
        {
            btnYes.onClick.RemoveListener(OnClickYes);
            btnYes.onClick.AddListener(OnClickYes);
        }

        if (btnNo != null)
        {
            btnNo.onClick.RemoveListener(OnClickNo);
            btnNo.onClick.AddListener(OnClickNo);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _playerMovement = other.GetComponent<PlayerMovement>();
        _playerRigidbody = other.GetComponent<Rigidbody2D>();
        ShowDialog();
        SetSleepModalActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_isSleepDialogOpen)
        {
            return;
        }

        HideDialog();
        SetSleepModalActive(false);
    }

    public void OnClickYes()
    {
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.SkipToNextDayMorning();
        }

        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.SaveActiveGameToSlot(0, "AutoSave_Sleep");
        }

        HideDialog();
        SetSleepModalActive(false);
    }

    public void OnClickNo()
    {
        HideDialog();
        SetSleepModalActive(false);
    }

    private void ShowDialog()
    {
        if (pnlSleepDialog != null) pnlSleepDialog.SetActive(true);
    }

    private void HideDialog()
    {
        if (pnlSleepDialog != null) pnlSleepDialog.SetActive(false);
    }

    private void SetSleepModalActive(bool active)
    {
        _isSleepDialogOpen = active;

        if (_playerMovement == null)
        {
            _playerMovement = FindObjectOfType<PlayerMovement>(true);
        }

        if (_playerMovement != null)
        {
            _playerMovement.enabled = !active;
        }

        if (_playerRigidbody == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerRigidbody = player.GetComponent<Rigidbody2D>();
            }
        }

        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = Vector2.zero;
            _playerRigidbody.simulated = true;
        }
    }
}