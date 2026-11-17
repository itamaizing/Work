using UnityEngine;
using UnityEngine.UI;

public sealed class HUDPsiTicks : MaskableGraphic
{
    private const int TickStep = 10;
    private float _maximum;
    private float _threshold;

    public void SetScale(float maximum, float threshold)
    {
        if (_maximum == maximum && _threshold == threshold) return;

        _maximum = maximum;
        _threshold = threshold;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f) return;

        float fineEnd = Mathf.Min(_maximum, _threshold);
        for (int value = TickStep; value < fineEnd; value += TickStep)
            AddTick(mesh, rect, value, 0.45f);

        if (_threshold > 0f && _threshold < _maximum)
            AddTick(mesh, rect, _threshold, 1f);
    }

    private void AddTick(VertexHelper mesh, Rect rect, float value, float heightFraction)
    {
        float fraction = PsionicBarScale.GetFill(value, _maximum, _threshold, PsionicBarScale.FineWidth);
        float center = rect.xMin + rect.width * fraction;
        float halfWidth = Mathf.Min(0.5f, rect.width * 0.01f);
        float bottom = rect.yMax - rect.height * heightFraction;
        int first = mesh.currentVertCount;
        mesh.AddVert(new Vector2(center - halfWidth, bottom), color, Vector2.zero);
        mesh.AddVert(new Vector2(center - halfWidth, rect.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector2(center + halfWidth, rect.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector2(center + halfWidth, bottom), color, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
