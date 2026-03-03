using Mirror;
using UnityEngine;

public class TransformationComponent : NetworkBehaviour
{
    [SerializeField] private GameObject _baseModel;
    [SerializeField] private GameObject _transformationModel;
    [SerializeField] private MeshFilter _transformationRenderer;

    public void MakeTransformation(Mesh mesh)
    {
        _baseModel.SetActive(false);
        _transformationModel.SetActive(true);
        _transformationRenderer.mesh = mesh;
    }

    public void ReturnToInitial()
    {
        if(_baseModel == null) return; 
        
        _baseModel.SetActive(true);
        _transformationModel.SetActive(false);
    }
}
