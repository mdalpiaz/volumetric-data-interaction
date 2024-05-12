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
        private bool _isHighlighted;
        private bool _isSelected;

        public event Action<bool> HighlightChanged;
        public event Action<bool> SelectChanged;

        private bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted == value)
                {
                    return;
                }
                
                _isHighlighted = value;
                HighlightChanged?.Invoke(_isHighlighted);
            }
        }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!value && IsHighlighted)
                {
                    IsHighlighted = false;
                }
                
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                SelectChanged?.Invoke(_isSelected);
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
    }
}