using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class GameButton : MonoBehaviour
{


    [Header("References")]
    [SerializeField]
    private GameObject press;
    [SerializeField]
    private GameButtonCollider buttonCollider;

    
    
    
    [Header("Settings and public events")]
    public UnityEvent onPress;
    public UnityEvent onRelease;
    public bool showDebugLogs = false;
    public bool useSound = true;
    [Tooltip("If true, the button is continuely interactable, if false it's pressable only when 'WaitForButtonPress' is called.")]
    public bool alwaysPressable = false;

    private Vector3 _origPosition;
    private AudioSource clickSound;

    private bool _isWaitingForPress = false;
    private UniTaskCompletionSource<bool> _waitTcs;
    private bool _pressAccepted = false;




    private void Start()
    {
        _origPosition = press.transform.localPosition;
        if (useSound)
        {
            clickSound = gameObject.GetComponent<AudioSource>();
            if (clickSound == null)
            {
                Debug.LogWarning($"[GameButton] No AudioSource found on {gameObject.name} but useSound is true. Disabling sound.");
                useSound = false;
            }
        }

        if (buttonCollider == null)
        {
            Debug.LogError($"[GameButton] {gameObject.name}: buttonCollider reference is not set in the inspector.");
        }

    }



    public virtual void whenPressed()
    {
        if (!alwaysPressable && !_isWaitingForPress)
            return;

        _pressAccepted = true;

        if (_isWaitingForPress)
        {
            _waitTcs?.TrySetResult(true);
            _isWaitingForPress = false;
        }

        onPress.Invoke();

        press.transform.localPosition = new Vector3(0, 0.003f, 0);
        clickSound?.Play();

        if (showDebugLogs)
        {
            Debug.Log($"[GameButton] {gameObject.name} button was pressed");
        }

    }


    public virtual void whenReleased()
    {
        if (!_pressAccepted)
            return;

        _pressAccepted = false;

        press.transform.localPosition = _origPosition;
        onRelease.Invoke();

        if (showDebugLogs)
        {
            Debug.Log($"[GameButton] {gameObject.name} button was released");
        }
    }

    public async UniTask WaitForButtonPress()
    {
        _isWaitingForPress = true;
        _waitTcs = new UniTaskCompletionSource<bool>();
        await _waitTcs.Task;

    }

    private void OnEnable()
    {
        buttonCollider.onPress += whenPressed;
        buttonCollider.onRelease += whenReleased;
    }

    private void OnDisable()
    {
        buttonCollider.onPress -= whenPressed;
        buttonCollider.onRelease -= whenReleased;
    }


}
