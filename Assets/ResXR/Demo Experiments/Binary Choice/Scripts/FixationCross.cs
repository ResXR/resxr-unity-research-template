using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider))]
public class FixationCross : MonoBehaviour
{
    [InfoBox("A simple fixation cross that can be shown or hidden.\nRequires a sprite renderer and a box collider. Make sure the collider is set to the sprite size before running the experiment, since it will collect eye gaze hit point data.")]
    public bool hideInAwake = true;
    private SpriteRenderer _spriteRenderer;
    private Collider _collider;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider>();
        if (hideInAwake)
        {
            Hide();
        }
    }

    public void Show()
    {
        _spriteRenderer.enabled = true;
        _collider.enabled = true;
    }

    public void Hide()
    {
        _spriteRenderer.enabled = false;
        _collider.enabled = false;
    }

    public async UniTask ShowForSeconds(float seconds)
    {
        Show();
        await UniTask.Delay(TimeSpan.FromSeconds(seconds));
        Hide();
    }

}
