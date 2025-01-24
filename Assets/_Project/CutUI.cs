using System;
using TMPro;
using UnityEngine;

public class CutUI : MonoBehaviour
{
    [SerializeField] private SoftbodyEqualCutterBehaviour cutterBehaviour;
    [SerializeField] private TMP_InputField sliceCount;
    [SerializeField] private TMP_InputField cutAngle;
    
    public void ButtonClearClicked()
    {
        cutterBehaviour.Refresh();
    }
    public void ButtonCutClicked()
    {
        cutterBehaviour.Cut(int.Parse(sliceCount.text), AngleToAxis(float.Parse(cutAngle.text)));
    }
    private Vector3 AngleToAxis(float cutAngle)
    {
        // Преобразуем угол из градусов в радианы
        float radians = cutAngle * Mathf.Deg2Rad;

        // Используем cos и sin для получения оси
        float x = Mathf.Cos(radians);
        float y = Mathf.Sin(radians);

        // Возвращаем вектор в плоскости XY
        return new Vector3(x, y, 0).normalized;
    }

}
