using UnityEngine;

namespace ContentContent.Save.Unity
{
    /// <summary>
    /// Code-free <see cref="SaveableBehaviour"/> persisting local transform position, rotation, scale,
    /// and GameObject active state across save/load cycles.
    /// </summary>
    [AddComponentMenu("ContentContent/Save/Saveable Transform")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/save/quickstart")]
    public class SaveableTransform : SaveableBehaviour
    {
        [Header("Captured Fields")]
        [Tooltip("Persists Transform.localPosition.")]
        [SerializeField] private bool _savePosition = true;

        [Tooltip("Persists Transform.localRotation.")]
        [SerializeField] private bool _saveRotation = true;

        [Tooltip("Persists Transform.localScale.")]
        [SerializeField] private bool _saveScale;

        [Tooltip("Persists GameObject.activeSelf (e.g., opened chests or collected items).")]
        [SerializeField] private bool _saveActiveState = true;

        public override void CaptureState(SaveSection section)
        {
            if (_savePosition)
            {
                Vector3 pos = transform.localPosition;
                section.SetFloat("px", pos.x);
                section.SetFloat("py", pos.y);
                section.SetFloat("pz", pos.z);
            }

            if (_saveRotation)
            {
                Quaternion rot = transform.localRotation;
                section.SetFloat("rx", rot.x);
                section.SetFloat("ry", rot.y);
                section.SetFloat("rz", rot.z);
                section.SetFloat("rw", rot.w);
            }

            if (_saveScale)
            {
                Vector3 scale = transform.localScale;
                section.SetFloat("sx", scale.x);
                section.SetFloat("sy", scale.y);
                section.SetFloat("sz", scale.z);
            }

            if (_saveActiveState)
            {
                section.SetBool("active", gameObject.activeSelf);
            }
        }

        public override void RestoreState(SaveSection section)
        {
            if (_savePosition && section.Has("px"))
            {
                transform.localPosition = new Vector3(
                    section.GetFloat("px"),
                    section.GetFloat("py"),
                    section.GetFloat("pz"));
            }

            if (_saveRotation && section.Has("rw"))
            {
                transform.localRotation = new Quaternion(
                    section.GetFloat("rx"),
                    section.GetFloat("ry"),
                    section.GetFloat("rz"),
                    section.GetFloat("rw"));
            }

            if (_saveScale && section.Has("sx"))
            {
                transform.localScale = new Vector3(
                    section.GetFloat("sx", 1f),
                    section.GetFloat("sy", 1f),
                    section.GetFloat("sz", 1f));
            }

            if (_saveActiveState && section.Has("active"))
            {
                gameObject.SetActive(section.GetBool("active", true));
            }
        }
    }
}
