using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SelfDestroyOnAnimationEnd : MonoBehaviour
{
    private Animator _animator;

    void Awake()
    {
        this.TryInitComponent(ref _animator);
    }

    private void Update()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

        if (state.normalizedTime >= 1f && !_animator.IsInTransition(0))
        {
            Destroy(gameObject);
        }
    }
}
