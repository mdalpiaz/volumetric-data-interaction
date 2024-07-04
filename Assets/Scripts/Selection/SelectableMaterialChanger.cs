using UnityEngine;
using UnityEngine.Serialization;

namespace Selection
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SelectableMaterialChanger : MonoBehaviour
    {
        [FormerlySerializedAs("greenMaterial")]
        [SerializeField]
        private Material selectedMaterial;
        
        [SerializeField]
        private Material highlightedMaterial;
        
        private Selectable selectable;
        private MeshRenderer meshRenderer;
        private Material defaultMaterial;
        
        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            meshRenderer = GetComponent<MeshRenderer>();
            defaultMaterial = meshRenderer.material;
        }

        private void OnEnable()
        {
            selectable.HighlightChanged += OnHighlightChanged;
            selectable.SelectChanged += OnSelectChanged;
        }

        private void OnDisable()
        {
            selectable.HighlightChanged -= OnHighlightChanged;
            selectable.SelectChanged -= OnSelectChanged;
        }
        
        private void OnHighlightChanged(bool isHighlighted) => SetMaterial(isHighlighted ? highlightedMaterial : defaultMaterial);

        private void OnSelectChanged(bool isSelected) => SetMaterial(isSelected ? selectedMaterial : defaultMaterial);

        private void SetMaterial(Material mat)
        {
            meshRenderer.material = mat;
            meshRenderer.material.mainTexture = defaultMaterial.mainTexture;
            meshRenderer.material.mainTextureScale = defaultMaterial.mainTextureScale;
        }
    }
}