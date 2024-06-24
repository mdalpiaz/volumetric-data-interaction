#nullable enable

using System;
using Networking.Tablet;
using UnityEngine;

namespace Selection
{
    /// <summary>
    /// Halo hack: https://answers.unity.com/questions/10534/changing-color-of-halo.html
    /// </summary>
    public class Selectable : MonoBehaviour
    {
        private bool isHighlighted;
        private bool isSelected;

        public event Action<bool>? HighlightChanged;
        public event Action<bool>? SelectChanged;

        private bool IsHighlighted
        {
            get => isHighlighted;
            set
            {
                if (isHighlighted == value)
                {
                    return;
                }
                
                isHighlighted = value;
                HighlightChanged?.Invoke(isHighlighted);
            }
        }
        
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (!value && IsHighlighted)
                {
                    IsHighlighted = false;
                }
                
                if (isSelected == value)
                {
                    return;
                }

                isSelected = value;
                SelectChanged?.Invoke(isSelected);
            }
        }

        /// <summary>
        /// Selectables are only highlighted if there is not already a highlighted object marked as selected in host script.
        /// This should avoid selection overlap which could occur with overlapping objects.
        /// The first to be selected is the only to be selected.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (TabletServer.Instance.Highlighted != null || !other.CompareTag(Tags.Ray))
            {
                return;
            }

            TabletServer.Instance.Highlighted = this;
            IsHighlighted = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsHighlighted || !other.CompareTag(Tags.Ray))
            {
                return;
            }

            TabletServer.Instance.Highlighted = null;
            IsHighlighted = false;
        }

        public void RerunHighlightEvent()
        {
            HighlightChanged?.Invoke(isHighlighted);
        }
    }
}