using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class EnvironmentCalibrator : ResXRSingleton<EnvironmentCalibrator>
{
    [SerializeField] private GameObject centerModel;
    [SerializeField] private Transform virtualReferencePointPosition;
    [SerializeField] private Transform virtualReferencePointRotation;
    [SerializeField] private GameObject calibrationMark;
    [SerializeField] private GameObject _btnConfirm;
    [SerializeField] private GameObject _btnRedo;

    [SerializeField] private FollowTransform
        _playerMarkedPosition; // a sphere following the exact position players mark when they pinch. Changes every frame according to players' hand.

    private float PINCH_HOLD_DURATION = 1f;

    // gets 2 points where player touches during the calibration action.
    private Transform realWorldReferencePointPosition;
    private Transform realWorldReferencePointRotation;
    private Transform _player;
    private bool _shouldRedo, _shouldConfirm;

    public async UniTask CalibrateRoom()
    {
        Init();
        EnterCalibrationMode();

        // wait until getting 1st point
        await ResXRPlayer.Instance.PinchingInputManager.WaitForHoldAndRelease(HandType.Right, PINCH_HOLD_DURATION);
        realWorldReferencePointPosition =
            Instantiate(calibrationMark, _playerMarkedPosition.Position, Quaternion.identity, _player).transform;

        // wait until getting 2st point
        await ResXRPlayer.Instance.PinchingInputManager.WaitForHoldAndRelease(HandType.Right, PINCH_HOLD_DURATION);
        realWorldReferencePointRotation =
            Instantiate(calibrationMark, _playerMarkedPosition.Position, Quaternion.identity, _player).transform;

        // once we have 2 real world reference points - we can calibrate.
        AlignVirtualToPhysicalRoom();

        // disable passthrough & show model
        ExitCalibrationMode();

        DisplayConfirmationButtons();

        while (!_shouldRedo && !_shouldConfirm)
        {
            await UniTask.Yield();
        }

        if (_shouldRedo)
        {
            // we will call Calibrate Room again to redo the calibration. 
            _shouldRedo = false;
            await CalibrateRoom();
        }
        else
        {
            // if we got here it means _shouldConfirm = true and we can end the task.
            _shouldConfirm = false;
            EndCalibration();
        }
    }

    private void Init()
    {
        _btnConfirm.gameObject.SetActive(false);
        _btnRedo.gameObject.SetActive(false);

        // TODO: Apply offsets
        _playerMarkedPosition.Init(ResXRPlayer.Instance.GetHandFingerCollider(HandType.Left, FingerType.Index));

        _shouldConfirm = false;
        _shouldRedo = false;

        _player = ResXRPlayer.Instance.transform;
        //    WaitForRightPinchHold = ResXRPlayer.Instance.WaitForPinchHold(HandType.Right, PINCH_HOLD_DURATION, true);
    }

    private void EnterCalibrationMode()
    {
        ResXRPlayer.Instance.SetPassthrough(true);

        // display reference point in hand (so user have accurate place for point-markdown)
        _playerMarkedPosition.gameObject.SetActive(true);
    }

    private void DisplayConfirmationButtons()
    {
        // show buttons
        _btnConfirm.gameObject.SetActive(true);
        _btnRedo.gameObject.SetActive(true);
    }

    private void ExitCalibrationMode()
    {
        ResXRPlayer.Instance.SetPassthrough(false);

        realWorldReferencePointPosition.gameObject.SetActive(false);
        realWorldReferencePointRotation.gameObject.SetActive(false);
    }

    private void EndCalibration()
    {
        // hide marker
        _playerMarkedPosition.gameObject.SetActive(false);

        // hide buttons
        _btnConfirm.gameObject.SetActive(false);
        _btnRedo.gameObject.SetActive(false);
    }

    // called when the redo calibration hold button is touched
    public void OnRedoCalibration()
    {
        _shouldRedo = true;
    }

    // called when the confirm calibration hold button is touched
    public void OnConfirmCalibration()
    {
        _shouldConfirm = true;
    }

    public void AlignVirtualToPhysicalRoom()
    {
        // ignore differences on height
        realWorldReferencePointRotation.position = new Vector3(realWorldReferencePointRotation.position.x,
            realWorldReferencePointPosition.position.y, realWorldReferencePointRotation.position.z);

        Vector3 realDirection = (realWorldReferencePointRotation.position - realWorldReferencePointPosition.position)
            .normalized;
        Vector3 virtualDirection = (virtualReferencePointRotation.position - virtualReferencePointPosition.position)
            .normalized;

        float angle = Vector3.SignedAngle(realDirection, virtualDirection, _player.up);
        _player.Rotate(_player.up, angle);

        Vector3 positionOffset = virtualReferencePointPosition.position - realWorldReferencePointPosition.position;
        _player.position += positionOffset;
    }
}