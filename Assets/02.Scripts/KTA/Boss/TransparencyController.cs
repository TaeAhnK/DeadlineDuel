using Unity.Netcode;
using UnityEngine;

public class TransparencyController : NetworkBehaviour
{
    [field: SerializeField] private Material OpaqueMaterial;
    [field: SerializeField] private Material TransparentMaterial;
    
    public void SetToTransparent()
    {
        SetMaterial(TransparentMaterial);
    }

    public void SetToOpaque()
    {
        SetMaterial(OpaqueMaterial);
    }
    
    private void SetMaterial(Material mat)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.material = mat;
        }
    }
}
