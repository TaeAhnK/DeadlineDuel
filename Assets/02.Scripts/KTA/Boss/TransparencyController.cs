using Unity.Netcode;
using UnityEngine;

public class TransparencyController : NetworkBehaviour
{
    private enum TransparencyState
    {
        Transparent,
        Opaque
    }
    
    [field: SerializeField] private Material[] OpaqueMaterial;
    [field: SerializeField] private Material[] TransparentMaterial;
    
    public void SetToTransparent()
    {
        SetMaterial(TransparencyState.Transparent);
    }

    public void SetToOpaque()
    {
        SetMaterial(TransparencyState.Opaque);
    }
    
    private void SetMaterial(TransparencyState transparencyState)
    {
        Debug.Log(transparencyState);
        Material[] mats;
        switch (transparencyState)
        {
           case TransparencyState.Transparent:
               mats = TransparentMaterial;
               break;
           case TransparencyState.Opaque:
               mats = OpaqueMaterial;
               break;
           default:
               mats = OpaqueMaterial;
               break;
        }
        
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            Debug.Log(transparencyState);
            Material[] originalMats = renderer.materials;
            for (int i = 0; i < originalMats.Length; i++)
            {
                originalMats[i] = mats[i];
            }
            renderer.materials = originalMats;
        }
    }
}
