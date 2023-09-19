using System.Collections.Generic;
using UnityEngine;

namespace Snapshots
{
    public class TabletOverlay : MonoBehaviour
    {
        private const int AdditionCount = 5;

        [SerializeField]
        private Transform main;

        public Transform Main => main;

        public List<Transform> Additions { get; } = new(AdditionCount);

        private void Awake()
        {
            // the first one is main
            // get all additions and add them to the list
            for (var i = 0; i < AdditionCount; i++)
            {
                Additions.Add(transform.GetChild(i + 1));
            }
        }
    }
}